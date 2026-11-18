using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RingTimer {
 sealed class SoundSwitchMotion {
  public double Position{get;private set;} public double Glass{get;private set;}
  public bool Running{get;private set;}
  double start,fromPosition,fromGlass,grow,shrinkAt;bool pressed,target;
  static double Clamp(double x){return Math.Max(0,Math.Min(1,x));}
  static double Ease(double x){return 1-Math.Pow(1-Clamp(x),3);}
  public void Reset(bool value){target=value;Position=value?1:0;Glass=0;Running=pressed=false;}
  public void Press(double now){start=now;fromGlass=Glass;pressed=true;Running=true;}
  public void Drag(double position){Position=Clamp(position);}
  public void Release(bool value,double now){target=value;pressed=false;start=now;fromPosition=Position;fromGlass=Glass;grow=100*(1-Glass);shrinkAt=Math.Max(grow,325);Running=true;}
  public void Sample(double now){if(!Running)return;double ms=Math.Max(0,now-start);
   if(pressed){Glass=fromGlass+(1-fromGlass)*Ease(ms/100);if(ms>=100){Glass=1;Running=false;}return;}
   Position=fromPosition+((target?1:0)-fromPosition)*Ease(ms/500);
   Glass=ms<grow?fromGlass+(1-fromGlass)*Ease(ms/grow):ms<shrinkAt?1:1-Ease((ms-shrinkAt)/100);
   if(ms>=500){Position=target?1:0;Glass=0;Running=false;}
  }
 }
 sealed class GlassSoundSwitch:CheckBox {
  readonly SoundSwitchMotion motion=new SoundSwitchMotion();readonly Stopwatch clock=Stopwatch.StartNew();
  SoundSwitchRail rail;bool held,dragged,subscribed;double origin,originPosition;
  public GlassSoundSwitch(){
   MinHeight=44;FocusVisualStyle=Theme.FocusRing;IslandDepthLayers.SetElevation(this,6);
   var grid=new FrameworkElementFactory(typeof(Grid));grid.SetValue(Panel.BackgroundProperty,Brushes.Transparent);
   var label=new FrameworkElementFactory(typeof(ContentPresenter));label.SetValue(VerticalAlignmentProperty,VerticalAlignment.Center);label.SetValue(MarginProperty,new Thickness(0,0,84,0));grid.AppendChild(label);
   var drawing=new FrameworkElementFactory(typeof(SoundSwitchRail),"Rail");drawing.SetValue(WidthProperty,SoundSwitchRail.VisualWidth);drawing.SetValue(HeightProperty,SoundSwitchRail.VisualHeight);drawing.SetValue(VerticalAlignmentProperty,VerticalAlignment.Center);drawing.SetValue(HorizontalAlignmentProperty,HorizontalAlignment.Right);drawing.SetValue(IsHitTestVisibleProperty,false);grid.AppendChild(drawing);
   Template=new ControlTemplate(typeof(CheckBox)){VisualTree=grid};
   Loaded+=(s,e)=>{Theme.Changed+=Repaint;motion.Reset(IsChecked==true);Repaint();};
   Unloaded+=(s,e)=>{Theme.Changed-=Repaint;Stop();};IsVisibleChanged+=(s,e)=>{if(!IsVisible)Stop();};
  }
  public override void OnApplyTemplate(){base.OnApplyTemplate();rail=GetTemplateChild("Rail") as SoundSwitchRail;if(rail!=null)rail.Owner=this;Repaint();}
  internal SoundSwitchMotion Motion{get{return motion;}}
  bool Animate{get{var menu=Window.GetWindow(this) as MenuWindow;return IsVisible&&SystemParameters.ClientAreaAnimation&&(menu==null||menu.AnimationsEnabled);}}
  void Repaint(){if(rail!=null)rail.InvalidateVisual();}
  void Subscribe(){if(!Animate){motion.Reset(IsChecked==true);Repaint();return;}if(!subscribed){CompositionTarget.Rendering+=Frame;subscribed=true;}}
  void Frame(object sender,EventArgs e){if(!Animate){Stop();return;}motion.Sample(clock.Elapsed.TotalMilliseconds);Repaint();if(!motion.Running){CompositionTarget.Rendering-=Frame;subscribed=false;}}
  void Stop(){if(subscribed){CompositionTarget.Rendering-=Frame;subscribed=false;}held=false;if(IsMouseCaptured)ReleaseMouseCapture();motion.Reset(IsChecked==true);Repaint();}
  void Changed(){motion.Release(IsChecked==true,clock.Elapsed.TotalMilliseconds);Subscribe();Repaint();}
  protected override void OnChecked(RoutedEventArgs e){base.OnChecked(e);Changed();}
  protected override void OnUnchecked(RoutedEventArgs e){base.OnUnchecked(e);Changed();}
  protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){Focus();if(!CaptureMouse())return;held=true;dragged=false;origin=e.GetPosition(this).X;originPosition=motion.Position;motion.Press(clock.Elapsed.TotalMilliseconds);Subscribe();e.Handled=true;}
  protected override void OnMouseMove(MouseEventArgs e){if(held){double delta=e.GetPosition(this).X-origin;if(Math.Abs(delta)>2)dragged=true;if(dragged){motion.Drag(originPosition+delta/SoundSwitchRail.Travel);Repaint();}e.Handled=true;}else base.OnMouseMove(e);}
  protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){if(!held){base.OnMouseLeftButtonUp(e);return;}held=false;bool value=dragged?motion.Position>=.5:IsChecked!=true;ReleaseMouseCapture();if(IsChecked==value)Changed();else SetCurrentValue(IsCheckedProperty,(bool?)value);e.Handled=true;}
  protected override void OnLostMouseCapture(MouseEventArgs e){base.OnLostMouseCapture(e);if(held){held=false;Changed();}}
  protected override void OnKeyDown(KeyEventArgs e){if(e.Key==Key.Escape&&held){held=false;ReleaseMouseCapture();Changed();e.Handled=true;return;}base.OnKeyDown(e);}
 }
 sealed class SoundSwitchRail:FrameworkElement {
  public GlassSoundSwitch Owner;
  internal const double VisualWidth=68,VisualHeight=34,RestWidth=30,RestHeight=18,ExpandedWidth=48,ExpandedHeight=32;
  internal const double Inset=2,LeftCenter=Inset+RestWidth/2,Travel=VisualWidth-2*Inset-RestWidth;
  static Brush Alpha(Color color,double opacity){var b=new SolidColorBrush(Color.FromArgb((byte)Math.Round(255*opacity),color.R,color.G,color.B));b.Freeze();return b;}
  static Color ColorOf(Brush brush){return ((SolidColorBrush)brush).Color;}
  protected override void OnRender(DrawingContext dc){base.OnRender(dc);if(Owner==null)return;double p=Owner.Motion.Position,g=Owner.Motion.Glass;
   var track=new Rect(0,6,VisualWidth,22);dc.DrawRoundedRectangle(Theme.SwitchOff,null,track,11,11);
   dc.PushOpacity(p);dc.DrawRoundedRectangle(Alpha(ColorOf(Theme.Done),.65),null,track,11,11);dc.Pop();
   double width=RestWidth+(ExpandedWidth-RestWidth)*g,height=RestHeight+(ExpandedHeight-RestHeight)*g,center=LeftCenter+Travel*p;var lens=new Rect(center-width/2,VisualHeight/2-height/2,width,height);
   // Layered translucent glass lets the track and the existing diffuse card field
   // remain visible through the expanded lens. No opaque image or per-frame blur.
   dc.DrawRoundedRectangle(Alpha(Colors.Black,.08+.08*g),null,new Rect(lens.X,lens.Y+2,lens.Width,lens.Height),height/2,height/2);
   dc.DrawRoundedRectangle(Alpha(ColorOf(Theme.SwitchThumb),1-.97*g),null,lens,height/2,height/2);
   var edge=new LinearGradientBrush();edge.StartPoint=new Point(0,0);edge.EndPoint=new Point(1,1);edge.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(60+125*g),255,255,255),0));edge.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(20+25*g),255,255,255),.4));edge.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(40+70*g),255,255,255),1));edge.Freeze();
   dc.DrawRoundedRectangle(null,new Pen(edge,1),lens,height/2,height/2);
   var shine=new LinearGradientBrush();shine.StartPoint=new Point(0,0);shine.EndPoint=new Point(0,1);shine.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(25+100*g),255,255,255),0));shine.GradientStops.Add(new GradientStop(Colors.Transparent,.3));shine.GradientStops.Add(new GradientStop(Colors.Transparent,.7));shine.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(15+70*g),255,255,255),1));shine.Freeze();dc.DrawRoundedRectangle(shine,null,lens,height/2,height/2);
  }
 }
}
