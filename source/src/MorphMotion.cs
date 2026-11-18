using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace RingTimer {
  sealed partial class MenuWindow {
    // The HWND keeps its final size during opening/closing. Only the visible and
    // interactive contour grows; layout, text and the launcher never scale.
    static readonly DependencyProperty RevealProgressProperty=DependencyProperty.Register("RevealProgress",typeof(double),typeof(MenuWindow),new PropertyMetadata(1.0,(d,e)=>((MenuWindow)d).UpdateRevealGeometry()));
    static readonly DependencyProperty ContentRevealProgressProperty=DependencyProperty.Register("ContentRevealProgress",typeof(double),typeof(MenuWindow),new PropertyMetadata(1.0,(d,e)=>((MenuWindow)d).UpdateContentReveal()));
    readonly RectangleGeometry revealClip=new RectangleGeometry();
    readonly BlurEffect contentRevealBlur=new BlurEffect{Radius=0,KernelType=KernelType.Gaussian,RenderingBias=RenderingBias.Performance};
    double closingRevealStart=1,closingContentOpacity=1;
    int contentRevealEpoch;
    double ContentRevealProgress{get{return (double)GetValue(ContentRevealProgressProperty);}set{SetValue(ContentRevealProgressProperty,value);}}
    bool RevealAnimationsEnabled{get{return AnimationsEnabled&&SystemParameters.ClientAreaAnimation;}}
    static double SoftReveal(double value){double t=Math.Max(0,Math.Min(1,value));return t*t*(3-2*t);}
    void UpdateContentReveal(){
      if(surface==null)return;var content=surface.Child as FrameworkElement;if(content==null)return;
      double p=Math.Max(0,Math.Min(1,ContentRevealProgress));
      double closingFade=IsClosing?SoftReveal(RevealProgress/Math.Max(.001,closingRevealStart)):1;
      double opacity=IsClosing?closingContentOpacity*closingFade:SoftReveal((p-.18)/.82);
      content.Opacity=opacity;content.OpacityMask=null;
      // A short, shallow defocus resolves while the whole content group fades
      // in. Text stays at its final position throughout the glass expansion.
      double blur=p>.18&&p<.73?1.6*(1-SoftReveal((p-.18)/.55)):0;
      if(blur>0&&(content.Effect==null||content.Effect==contentRevealBlur)){
        contentRevealBlur.Radius=blur;if(content.Effect!=contentRevealBlur)content.Effect=contentRevealBlur;
      }else if(content.Effect==contentRevealBlur)content.Effect=null;
      CardPresenter.SetContentReveal(opacity,p,Above,closingFade);
      surface.IsHitTestVisible=!IsClosing&&RevealProgress>=1&&p>=1;
    }
    void StopContentReveal(){double current=ContentRevealProgress;++contentRevealEpoch;BeginAnimation(ContentRevealProgressProperty,null);ContentRevealProgress=current;}
    void SetContentReveal(double progress){StopContentReveal();ContentRevealProgress=progress;UpdateContentReveal();}
    void BeginContentReveal(){
      if(IsClosing||!IsVisible)return;
      if(!RevealAnimationsEnabled){SetContentReveal(1);return;}
      StopContentReveal();double from=ContentRevealProgress;int epoch=contentRevealEpoch;
      var animation=new DoubleAnimation(from,1,TimeSpan.FromMilliseconds(Math.Max(80,460*(1-from))));
      animation.Completed+=(s,e)=>{if(epoch==contentRevealEpoch&&!IsClosing&&IsVisible)SetContentReveal(1);};
      BeginAnimation(ContentRevealProgressProperty,animation);
    }
    Rect lastShape=Rect.Empty,lastHole=Rect.Empty,lastLauncherExtent=Rect.Empty;int lastRadius=-1;bool morphing;
    public Func<Native.RECT?> AnchorBounds{get;set;}
    double RevealProgress{get{return (double)GetValue(RevealProgressProperty);}set{SetValue(RevealProgressProperty,value);}}
    protected override void UpdateWindowShape(){UpdateRevealGeometry();}
    void UpdateRevealGeometry(){
      if(geometryUpdating)return;
      if(CardPresenter!=null&&CardPresenter.IsPresenting){CardPresenter.UpdateContour();return;}
      if(surface==null||new System.Windows.Interop.WindowInteropHelper(this).Handle==IntPtr.Zero||surface.ActualWidth<=0||surface.ActualHeight<=0)return;
      double scale=Native.ScaleFor(Handle);var window=PixelBounds;var offset=surface.TranslatePoint(new Point(),this);var full=new Rect(0,0,surface.ActualWidth,surface.ActualHeight);
      Native.RECT? anchor=AnchorBounds==null?null:AnchorBounds();Rect hole=Rect.Empty;
      Rect origin=new Rect(full.Right-50,Above?full.Bottom-50:6,44,44);
      if(anchor.HasValue){var b=anchor.Value;hole=new Rect(b.Left-window.Left,b.Top-window.Top,b.Width,b.Height);origin=new Rect(hole.X/scale-offset.X,hole.Y/scale-offset.Y,hole.Width/scale,hole.Height/scale);}
      Rect extension=Launcher!=null&&Launcher.IsVisible?hole:Rect.Empty;
      CardPresenter.UpdateLauncherBacking(extension.IsEmpty?Rect.Empty:new Rect(extension.X/scale,extension.Y/scale,extension.Width/scale,extension.Height/scale));
      if(Launcher!=null)hole=Rect.Empty;
      double p=Math.Max(0,Math.Min(1,RevealProgress));var contour=new Rect(origin.X*(1-p),origin.Y*(1-p),origin.Width+(full.Width-origin.Width)*p,origin.Height+(full.Height-origin.Height)*p);
      double radius=22+(surface.CornerRadius.TopLeft-22)*p;revealClip.Rect=contour;revealClip.RadiusX=radius;revealClip.RadiusY=radius;if(surface.Clip!=revealClip)surface.Clip=revealClip;
      UpdateContentReveal();
      var pixels=new Rect(Math.Floor((offset.X+contour.X)*scale),Math.Floor((offset.Y+contour.Y)*scale),Math.Ceiling(contour.Width*scale),Math.Ceiling(contour.Height*scale));int corner=(int)Math.Round(radius*scale);
      if(pixels==lastShape&&hole==lastHole&&corner==lastRadius&&extension==lastLauncherExtent)return;lastShape=pixels;lastHole=hole;lastRadius=corner;lastLauncherExtent=extension;GlassBackdrop.MorphShape(Handle,pixels,corner,hole,extension);
    }
    void CancelReveal(){morphing=false;BeginAnimation(RevealProgressProperty,null);RevealProgress=1;SetContentReveal(1);}
    void AnimateReveal(double target,int duration,Action complete){
      double from=RevealProgress;BeginAnimation(RevealProgressProperty,null);RevealProgress=from;int epoch=++visibilityEpoch;morphing=true;
      var animation=Motion(from,target,Math.Max(80,(int)(duration*Math.Abs(target-from))));
      animation.Completed+=(s,e)=>{if(epoch!=visibilityEpoch)return;morphing=false;BeginAnimation(RevealProgressProperty,null);RevealProgress=target;if(complete!=null)complete();};BeginAnimation(RevealProgressProperty,animation);
    }
    public void Reveal(){
      bool visible=IsVisible,reopening=IsClosing;IsClosing=false;surface.Opacity=1;reveal.Y=0;surface.VerticalAlignment=Above?VerticalAlignment.Bottom:VerticalAlignment.Top;
      if(!visible||reopening)SetContentReveal(RevealAnimationsEnabled?0:1);
      if(!visible){ReserveBacking();CommitBounds();BeginAnimation(RevealProgressProperty,null);RevealProgress=RevealAnimationsEnabled?0:1;UpdateRevealGeometry();Show();}
      if(RevealAnimationsEnabled){if(!visible||morphing||RevealProgress<1||reopening){AnimateReveal(1,340,null);BeginContentReveal();}else UpdateRevealGeometry();}
      else{++visibilityEpoch;CancelReveal();UpdateRevealGeometry();}
    }
    public void ClosePanel(){
      if(IsClosing||!IsVisible)return;StopContentReveal();closingRevealStart=RevealProgress;var content=surface.Child as FrameworkElement;closingContentOpacity=content==null?0:content.Opacity;IsClosing=true;surface.IsHitTestVisible=false;
      CardPresenter.Reset();
      if(!RevealAnimationsEnabled){HideImmediately();closed();return;}
      AnimateReveal(0,240,()=>{HideImmediately();closed();});
    }
  }
}
