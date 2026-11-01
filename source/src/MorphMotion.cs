using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RingTimer {
  sealed partial class MenuWindow {
    // The HWND keeps its final size during opening/closing. Only the visible and
    // interactive contour grows; layout, text and the launcher never scale.
    static readonly DependencyProperty RevealProgressProperty=DependencyProperty.Register("RevealProgress",typeof(double),typeof(MenuWindow),new PropertyMetadata(1.0,(d,e)=>((MenuWindow)d).UpdateRevealGeometry()));
    readonly RectangleGeometry revealClip=new RectangleGeometry();
    Rect lastShape=Rect.Empty,lastHole=Rect.Empty,lastLauncherExtent=Rect.Empty;int lastRadius=-1;bool morphing;
    public Func<Native.RECT?> AnchorBounds{get;set;}
    double RevealProgress{get{return (double)GetValue(RevealProgressProperty);}set{SetValue(RevealProgressProperty,value);}}
    protected override void UpdateWindowShape(){UpdateRevealGeometry();}
    void UpdateRevealGeometry(){
      if(CardPresenter!=null&&CardPresenter.IsPresenting){CardPresenter.UpdateContour();return;}
      if(surface==null||new System.Windows.Interop.WindowInteropHelper(this).Handle==IntPtr.Zero||surface.ActualWidth<=0||surface.ActualHeight<=0)return;
      double scale=Native.ScaleFor(Handle);var window=PixelBounds;var offset=surface.TranslatePoint(new Point(),this);var full=new Rect(0,0,surface.ActualWidth,surface.ActualHeight);
      Native.RECT? anchor=AnchorBounds==null?null:AnchorBounds();Rect hole=Rect.Empty;
      Rect origin=new Rect(full.Right-50,Above?full.Bottom-50:6,44,44);
      if(anchor.HasValue){var b=anchor.Value;hole=new Rect(b.Left-window.Left,b.Top-window.Top,b.Width,b.Height);origin=new Rect(hole.X/scale-offset.X,hole.Y/scale-offset.Y,hole.Width/scale,hole.Height/scale);}
      Rect extension=Launcher!=null?hole:Rect.Empty;
      CardPresenter.UpdateLauncherBacking(extension.IsEmpty?Rect.Empty:new Rect(extension.X/scale,extension.Y/scale,extension.Width/scale,extension.Height/scale));
      if(Launcher!=null)hole=Rect.Empty;
      double p=Math.Max(0,Math.Min(1,RevealProgress));var contour=new Rect(origin.X*(1-p),origin.Y*(1-p),origin.Width+(full.Width-origin.Width)*p,origin.Height+(full.Height-origin.Height)*p);
      double radius=22+(surface.CornerRadius.TopLeft-22)*p;revealClip.Rect=contour;revealClip.RadiusX=radius;revealClip.RadiusY=radius;if(surface.Clip!=revealClip)surface.Clip=revealClip;
      var content=surface.Child as UIElement;if(content!=null)content.Opacity=Math.Max(0,Math.Min(1,(p-.72)/.28));surface.IsHitTestVisible=!IsClosing&&p>.85;
      var pixels=new Rect(Math.Floor((offset.X+contour.X)*scale),Math.Floor((offset.Y+contour.Y)*scale),Math.Ceiling(contour.Width*scale),Math.Ceiling(contour.Height*scale));int corner=(int)Math.Round(radius*scale);
      if(pixels==lastShape&&hole==lastHole&&corner==lastRadius&&extension==lastLauncherExtent)return;lastShape=pixels;lastHole=hole;lastRadius=corner;lastLauncherExtent=extension;GlassBackdrop.MorphShape(Handle,pixels,corner,hole,extension);
    }
    void CancelReveal(){morphing=false;BeginAnimation(RevealProgressProperty,null);RevealProgress=1;}
    void AnimateReveal(double target,int duration,Action complete){
      double from=RevealProgress;BeginAnimation(RevealProgressProperty,null);RevealProgress=from;int epoch=++visibilityEpoch;morphing=true;
      var animation=Motion(from,target,Math.Max(80,(int)(duration*Math.Abs(target-from))));
      animation.Completed+=(s,e)=>{if(epoch!=visibilityEpoch)return;morphing=false;BeginAnimation(RevealProgressProperty,null);RevealProgress=target;if(complete!=null)complete();};BeginAnimation(RevealProgressProperty,animation);
    }
    public void Reveal(){
      bool visible=IsVisible;IsClosing=false;surface.Opacity=1;reveal.Y=0;surface.VerticalAlignment=Above?VerticalAlignment.Bottom:VerticalAlignment.Top;
      if(!visible){CommitBounds();BeginAnimation(RevealProgressProperty,null);RevealProgress=AnimationsEnabled?0:1;UpdateRevealGeometry();Show();}
      if(AnimationsEnabled){if(!visible||morphing||RevealProgress<1)AnimateReveal(1,280,null);else UpdateRevealGeometry();}
      else{++visibilityEpoch;CancelReveal();UpdateRevealGeometry();}
    }
    public void ClosePanel(){
      if(IsClosing||!IsVisible)return;IsClosing=true;surface.IsHitTestVisible=false;
      CardPresenter.Reset();
      if(!AnimationsEnabled){HideImmediately();closed();return;}
      AnimateReveal(0,240,()=>{HideImmediately();closed();});
    }
  }
}
