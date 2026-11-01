using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
namespace RingTimer {
 static class MicroMotion {
  public static DoubleAnimation Ease(double from,double to,int ms){return new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(ms)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop};}
  public static void TextIn(FrameworkElement text){if(!text.IsVisible||!SystemParameters.ClientAreaAnimation)return;var blur=new BlurEffect{Radius=0};text.Effect=blur;var a=Ease(8,0,220);a.Completed+=(s,e)=>{if(text.Effect==blur)text.Effect=null;};blur.BeginAnimation(BlurEffect.RadiusProperty,a);text.BeginAnimation(UIElement.OpacityProperty,Ease(.6,1,220));}
  public static void Attach(Button button){
   bool held=false;Action<bool> motion=down=>{if(held==down)return;held=down;button.ApplyTemplate();var face=button.Template.FindName("surface",button) as FrameworkElement;if(face==null)return;var scale=face.RenderTransform as ScaleTransform;if(scale==null){scale=new ScaleTransform(1,1);face.RenderTransform=scale;face.RenderTransformOrigin=new Point(.5,.5);}
    if(!SystemParameters.ClientAreaAnimation){scale.ScaleX=scale.ScaleY=down?.955:1;return;}
    var a=new DoubleAnimationUsingKeyFrames{Duration=TimeSpan.FromMilliseconds(down?100:340),FillBehavior=FillBehavior.Stop};
    a.KeyFrames.Add(new EasingDoubleKeyFrame(scale.ScaleX,KeyTime.FromTimeSpan(TimeSpan.Zero)));
    if(down)a.KeyFrames.Add(new EasingDoubleKeyFrame(.955,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100)),new CubicEase{EasingMode=EasingMode.EaseOut}));
    else{a.KeyFrames.Add(new SplineDoubleKeyFrame(1.025,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)),new KeySpline(.2,.8,.2,1)));a.KeyFrames.Add(new SplineDoubleKeyFrame(.995,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(255)),new KeySpline(.2,.8,.2,1)));a.KeyFrames.Add(new EasingDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(340))));}
    scale.ScaleX=scale.ScaleY=down?.955:1;scale.BeginAnimation(ScaleTransform.ScaleXProperty,a);scale.BeginAnimation(ScaleTransform.ScaleYProperty,a);
   };
   button.PreviewMouseLeftButtonDown+=(s,e)=>motion(true);button.PreviewMouseLeftButtonUp+=(s,e)=>motion(false);button.LostMouseCapture+=(s,e)=>motion(false);button.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Space||e.Key==Key.Enter)motion(true);};button.PreviewKeyUp+=(s,e)=>{if(e.Key==Key.Space||e.Key==Key.Enter)motion(false);};button.IsEnabledChanged+=(s,e)=>{if(!button.IsEnabled)motion(false);};button.Unloaded+=(s,e)=>{held=false;var face=button.Template.FindName("surface",button) as FrameworkElement;if(face!=null)face.RenderTransform=Transform.Identity;};
  }
  static ImageSource spot;
  public static void AddSpecular(Grid layers){
   if(spot==null){var v=new DrawingVisual();using(var dc=v.RenderOpen()){dc.DrawEllipse(new RadialGradientBrush(Colors.White,Colors.Transparent),null,new Point(150,150),150,150);}var bitmap=new RenderTargetBitmap(300,300,96,96,PixelFormats.Pbgra32);bitmap.Render(v);bitmap.Freeze();spot=bitmap;}
   var shift=new TranslateTransform();var layer=new Image{Source=spot,Width=300,Height=300,Opacity=0,IsHitTestVisible=false,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,RenderTransform=shift,Stretch=Stretch.Fill};layers.Children.Add(layer);
   layers.MouseMove+=(s,e)=>{var at=e.GetPosition(layers);shift.X=at.X-150;shift.Y=at.Y-150;if(layer.Opacity<.044){layer.BeginAnimation(UIElement.OpacityProperty,null);layer.Opacity=.045;}};
   layers.MouseLeave+=(s,e)=>{double from=layer.Opacity;layer.Opacity=0;layer.BeginAnimation(UIElement.OpacityProperty,Ease(from,0,200));};
  }
 }
 sealed class SlidingRail:Grid {
  // A paint-only copy of each label; accessible actions stay on the original buttons.
  sealed class ContrastText:TextBlock {protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer(){return null;}}
  readonly RectangleGeometry textClip=new RectangleGeometry();
  readonly Border thumb;readonly TranslateTransform move=new TranslateTransform();readonly List<Button> buttons=new List<Button>();
  bool dragging,subscribed,settling;int selected;double grab,target,settleFrom,elapsed;TimeSpan? lastFrame;
  public event Action<int> Changed;public event Action GestureEnded;
  public int Selected{get{return selected;}}public bool IsInteracting{get{return dragging;}}
  internal double ThumbPosition{get{return move.X;}}
  public SlidingRail(params string[] labels){Height=34;Background=Brushes.Transparent;Focusable=true;FocusVisualStyle=Theme.FocusRing;
   Children.Add(new Border{Background=Theme.GlassControl,CornerRadius=new CornerRadius(10)});
   thumb=new Border{Background=Theme.Primary,CornerRadius=new CornerRadius(8),Height=28,Margin=new Thickness(3,3,0,3),HorizontalAlignment=HorizontalAlignment.Left,RenderTransform=move,IsHitTestVisible=false};Children.Add(thumb);
   var grid=new System.Windows.Controls.Primitives.UniformGrid{Columns=labels.Length,Margin=new Thickness(3)};Children.Add(grid);
   for(int i=0;i<labels.Length;i++){int n=i;var button=Theme.Button(labels[i],false);button.Style=Theme.BareStyle;button.Background=Brushes.Transparent;button.Height=28;button.FontSize=12;button.Click+=(s,e)=>Select(n,true);buttons.Add(button);grid.Children.Add(button);}
   var contrast=new System.Windows.Controls.Primitives.UniformGrid{Columns=labels.Length,Margin=new Thickness(3),IsHitTestVisible=false,Clip=textClip};
   foreach(string label in labels){var text=new ContrastText{FontFamily=Theme.Font,FontSize=12,Foreground=Theme.PrimaryText,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};L.Bind(text,TextBlock.TextProperty,label);contrast.Children.Add(text);}Children.Add(contrast);
   SizeChanged+=(s,e)=>{if(dragging)EndGesture(false);thumb.Width=Step;move.X=target=selected*Step;settling=false;PaintLabels();};
   PreviewMouseLeftButtonDown+=(s,e)=>{if(e.StylusDevice!=null)return;Focus();if(CaptureMouse())BeginGesture(e.GetPosition(this).X);e.Handled=true;};
   PreviewMouseMove+=(s,e)=>{if(dragging&&IsMouseCaptured){if(e.LeftButton==MouseButtonState.Pressed)MoveGesture(e.GetPosition(this).X);else EndGesture(false);e.Handled=true;}};
   PreviewMouseLeftButtonUp+=(s,e)=>{if(!dragging||!IsMouseCaptured)return;MoveGesture(e.GetPosition(this).X);EndGesture(true);ReleaseMouseCapture();e.Handled=true;};
   LostMouseCapture+=(s,e)=>{if(dragging)EndGesture(false);};
   PreviewTouchDown+=(s,e)=>{if(dragging)return;Focus();if(e.TouchDevice.Capture(this))BeginGesture(e.GetTouchPoint(this).Position.X);e.Handled=true;};
   PreviewTouchMove+=(s,e)=>{if(dragging&&e.TouchDevice.Captured==this){MoveGesture(e.GetTouchPoint(this).Position.X);e.Handled=true;}};
   PreviewTouchUp+=(s,e)=>{if(dragging&&e.TouchDevice.Captured==this){MoveGesture(e.GetTouchPoint(this).Position.X);EndGesture(true);e.TouchDevice.Capture(null);e.Handled=true;}};
   LostTouchCapture+=(s,e)=>{if(dragging)EndGesture(false);};
   PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape&&dragging){EndGesture(false);ReleaseMouseCapture();ReleaseAllTouchCaptures();e.Handled=true;return;}int n=selected;if(e.Key==Key.Right)n++;else if(e.Key==Key.Left)n--;else if(e.Key==Key.Home)n=0;else if(e.Key==Key.End)n=buttons.Count-1;else return;Select(n,true);e.Handled=true;};
   Unloaded+=(s,e)=>{if(dragging)EndGesture(false);StopFrames();settling=false;move.X=target=selected*Step;PaintContrast();};
   IsEnabledChanged+=(s,e)=>{if(!IsEnabled&&dragging){EndGesture(false);ReleaseMouseCapture();ReleaseAllTouchCaptures();}};
   AutomationProperties.SetName(this,"统计视图，左右拖动或使用方向键");PaintLabels();
  }
  double Step{get{return Math.Max(1,(ActualWidth-6)/buttons.Count);}}
  double Clamp(double x){return Math.Max(0,Math.Min((buttons.Count-1)*Step,x));}
  internal void BeginGesture(double x){dragging=true;settling=false;double local=x-3;grab=local>=move.X&&local<=move.X+Step?local-move.X:Step/2;target=Clamp(local-grab);StartFrames();}
  internal void MoveGesture(double x){if(dragging)target=Clamp(x-3-grab);}
  internal void EndGesture(bool commit){if(!dragging)return;dragging=false;if(IsMouseCaptured)ReleaseMouseCapture();ReleaseAllTouchCaptures();int next=commit?(int)Math.Floor(target/Step+.5):selected;Select(next,true);if(GestureEnded!=null)GestureEnded();}
  void StartFrames(){if(subscribed)return;lastFrame=null;CompositionTarget.Rendering+=OnFrame;subscribed=true;}
  void StopFrames(){if(!subscribed)return;CompositionTarget.Rendering-=OnFrame;subscribed=false;lastFrame=null;}
  void OnFrame(object sender,EventArgs args){var time=((RenderingEventArgs)args).RenderingTime;if(lastFrame==time)return;double dt=lastFrame.HasValue?(time-lastFrame.Value).TotalSeconds:0;lastFrame=time;AdvanceFrame(dt);}
  internal void AdvanceFrame(double seconds){if(dragging){double dt=Math.Max(0,seconds);move.X=SystemParameters.ClientAreaAnimation?move.X+(target-move.X)*(1-Math.Exp(-dt/.055)):target;if(Math.Abs(move.X-target)<.02)move.X=target;PaintContrast();return;}if(!settling){StopFrames();return;}elapsed+=Math.Max(0,seconds);double t=Math.Min(1,elapsed/.26),ease=1-Math.Pow(1-t,3);move.X=settleFrom+(target-settleFrom)*ease;PaintContrast();if(t>=1){move.X=target;settling=false;StopFrames();}}
  public void Select(int value,bool notify){int next=Math.Max(0,Math.Min(buttons.Count-1,value));bool changed=next!=selected;selected=next;target=selected*Step;settleFrom=move.X;elapsed=0;PaintLabels();
   if(IsVisible&&SystemParameters.ClientAreaAnimation&&Math.Abs(target-move.X)>.01){settling=true;StartFrames();}else{settling=false;move.X=target;StopFrames();PaintContrast();}if(changed&&notify&&Changed!=null)Changed(selected);
  }
  void PaintContrast(){textClip.Rect=new Rect(move.X,0,Step,28);textClip.RadiusX=textClip.RadiusY=8;}
  void PaintLabels(){PaintContrast();for(int i=0;i<buttons.Count;i++){buttons[i].Foreground=Theme.Muted;AutomationProperties.SetItemStatus(buttons[i],i==selected?L.T("已选择"):"");}}
 }
 sealed class CycleRing:FrameworkElement {
  public static readonly DependencyProperty ProgressProperty=DependencyProperty.Register("Progress",typeof(double),typeof(CycleRing),new FrameworkPropertyMetadata(1.0,FrameworkPropertyMetadataOptions.AffectsRender));
  public double Progress{get{return (double)GetValue(ProgressProperty);}set{SetValue(ProgressProperty,value);}}
  public Brush Ink=Theme.Done;
  protected override void OnRender(DrawingContext dc){base.OnRender(dc);var center=new Point(100,100);dc.PushOpacity(.04);dc.DrawEllipse(Ink,null,center,81,81);dc.Pop();dc.DrawEllipse(null,new Pen(Theme.Track,8),center,88,88);double p=Math.Max(0,Math.Min(1,Progress));var pen=new Pen(Ink,8){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};if(p>=.99999)dc.DrawEllipse(null,pen,center,88,88);else if(p>0){double a=p*2*Math.PI;var end=new Point(100+88*Math.Sin(a),100-88*Math.Cos(a));var geometry=new StreamGeometry();using(var g=geometry.Open()){g.BeginFigure(new Point(100,12),false,false);g.ArcTo(end,new Size(88,88),0,p>.5,SweepDirection.Clockwise,true,false);}dc.DrawGeometry(null,pen,geometry);}if(p>0)dc.DrawEllipse(Theme.Foreground,null,new Point(100+88*Math.Sin(p*2*Math.PI),100-88*Math.Cos(p*2*Math.PI)),4,4);}
 }
}
