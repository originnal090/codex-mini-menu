using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RingTimer {
  sealed class DurationWheel:Control {
    const double RowHeight=32;
    public static readonly DependencyProperty PositionProperty=DependencyProperty.Register("Position",typeof(double),typeof(DurationWheel),new FrameworkPropertyMetadata(0.0,FrameworkPropertyMetadataOptions.AffectsRender));
    public double Position {get{return (double)GetValue(PositionProperty);}set{SetValue(PositionProperty,value);}}
    public int Maximum {get;private set;}
    public int Value {get;private set;}
    public event Action ValueChanged;
    bool dragging,moved;double lastY,dragStartY,velocity;long lastMove;
    public DurationWheel(int maximum,string name){
      Maximum=maximum;Height=160;MinWidth=70;Focusable=true;IsTabStop=true;ClipToBounds=true;Cursor=Cursors.Hand;
      Foreground=Theme.Foreground;FontFamily=Theme.NumberFont;FocusVisualStyle=Theme.FocusRing;
      IsManipulationEnabled=true;L.Bind(this,AutomationProperties.NameProperty,name);L.Bind(this,AutomationProperties.HelpTextProperty,"上下方向键调整时长");
      // Absolute coordinates keep fading inside the viewport even when offscreen rows enlarge the drawing bounds.
      UpdateFade();SizeChanged+=(s,e)=>UpdateFade();
      Unloaded+=(s,e)=>{dragging=false;ReleaseMouseCapture();Snap(false);};
      IsEnabledChanged+=(s,e)=>{if(!IsEnabled){dragging=false;ReleaseMouseCapture();Snap(false);}};
    }
    void UpdateFade(){var mask=new LinearGradientBrush(new GradientStopCollection{new GradientStop(Colors.Transparent,0),new GradientStop(Colors.Black,.34),new GradientStop(Colors.Black,.66),new GradientStop(Colors.Transparent,1)},new Point(0,0),new Point(0,ActualHeight>0?ActualHeight:Height)){MappingMode=BrushMappingMode.Absolute};mask.Freeze();OpacityMask=mask;}
    public void SetValue(int value,bool animate){
      value=Math.Max(0,Math.Min(Maximum,value));int previous=Value;double from=Position;
      Value=value;BeginAnimation(PositionProperty,null);Position=value;
      if(animate&&IsVisible&&SystemParameters.ClientAreaAnimation&&Math.Abs(from-value)>.001){BeginAnimation(PositionProperty,new DoubleAnimation(from,value,TimeSpan.FromMilliseconds(220)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});}
      if(previous!=value){var peer=UIElementAutomationPeer.FromElement(this);if(peer!=null)peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,(double)previous,(double)value);if(ValueChanged!=null)ValueChanged();}
    }
    void Move(double delta){double to=Math.Max(0,Math.Min(Maximum,Position+delta));BeginAnimation(PositionProperty,null);Position=to;int n=(int)Math.Round(to);if(n!=Value){int old=Value;Value=n;var peer=UIElementAutomationPeer.FromElement(this);if(peer!=null)peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,(double)old,(double)n);if(ValueChanged!=null)ValueChanged();}}
    void Snap(bool animate){SetValue(Value,animate);}
    protected override void OnRender(DrawingContext dc){
      base.OnRender(dc);dc.DrawRectangle(Brushes.Transparent,null,new Rect(RenderSize));double position=Position;
      int center=(int)Math.Round(position);for(int i=Math.Max(0,center-3);i<=Math.Min(Maximum,center+3);i++){
        double distance=Math.Abs(i-position),strength=Math.Max(0,1-distance);double size=24+4*strength;
        var text=new FormattedText(i.ToString("00",CultureInfo.InvariantCulture),CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface(FontFamily,FontStyles.Normal,strength>.5?FontWeights.Medium:FontWeights.Normal,FontStretches.Normal),size,Foreground,VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.PushOpacity(.55+.45*strength);dc.DrawText(text,new Point((ActualWidth-text.Width)/2,ActualHeight/2+(i-position)*RowHeight-text.Height/2));dc.Pop();
      }
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e){base.OnMouseWheel(e);if(!IsEnabled)return;Focus();int step=e.Delta/120;if(step==0)step=Math.Sign(e.Delta);SetValue(Value+step,true);e.Handled=true;}
    protected override void OnKeyDown(KeyEventArgs e){
      base.OnKeyDown(e);int next=Value;
      switch(e.Key){case Key.Up:next++;break;case Key.Down:next--;break;case Key.PageUp:next+=5;break;case Key.PageDown:next-=5;break;case Key.Home:next=0;break;case Key.End:next=Maximum;break;default:return;}
      SetValue(next,true);e.Handled=true;
    }
    internal void BeginDirectManipulation(){double current=Position;BeginAnimation(PositionProperty,null);Position=current;Move(0);}
    internal void UpdatePointer(double y,long stamp){double delta=lastY-y;double elapsed=(stamp-lastMove)*1000.0/Stopwatch.Frequency;if(Math.Abs(y-dragStartY)>SystemParameters.MinimumVerticalDragDistance)moved=true;Move(delta/RowHeight);if(elapsed>0)velocity=delta/RowHeight/elapsed;lastY=y;lastMove=stamp;}
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){base.OnMouseLeftButtonDown(e);if(e.StylusDevice!=null)return;Focus();BeginDirectManipulation();lastY=dragStartY=e.GetPosition(this).Y;lastMove=Stopwatch.GetTimestamp();velocity=0;dragging=CaptureMouse();moved=false;e.Handled=true;}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging||e.LeftButton!=MouseButtonState.Pressed)return;UpdatePointer(e.GetPosition(this).Y,Stopwatch.GetTimestamp());e.Handled=true;}
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){base.OnMouseLeftButtonUp(e);if(!dragging)return;dragging=false;ReleaseMouseCapture();if(moved){double age=(Stopwatch.GetTimestamp()-lastMove)*1000.0/Stopwatch.Frequency;double inertia=age<100?Math.Max(-4,Math.Min(4,velocity*100)):0;SetValue((int)Math.Round(Position+inertia),true);}else SetValue(Value+(int)Math.Round((e.GetPosition(this).Y-ActualHeight/2)/RowHeight),true);e.Handled=true;}
    protected override void OnLostMouseCapture(MouseEventArgs e){base.OnLostMouseCapture(e);if(dragging){dragging=false;Snap(true);}}
    protected override void OnManipulationStarting(ManipulationStartingEventArgs e){base.OnManipulationStarting(e);Focus();BeginDirectManipulation();e.ManipulationContainer=this;e.Mode=ManipulationModes.TranslateY;e.Handled=true;}
    protected override void OnManipulationDelta(ManipulationDeltaEventArgs e){base.OnManipulationDelta(e);if(!IsEnabled){e.Complete();e.Handled=true;return;}Move(-e.DeltaManipulation.Translation.Y/RowHeight);if(e.IsInertial&&(Position<=0||Position>=Maximum))e.Complete();e.Handled=true;}
    protected override void OnManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e){base.OnManipulationInertiaStarting(e);e.TranslationBehavior.DesiredDeceleration=.004;e.Handled=true;}
    protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e){base.OnManipulationCompleted(e);Snap(true);e.Handled=true;}
    protected override AutomationPeer OnCreateAutomationPeer(){return new WheelPeer(this);}
    sealed class WheelPeer:FrameworkElementAutomationPeer,IRangeValueProvider {
      DurationWheel Wheel {get{return (DurationWheel)Owner;}}
      public WheelPeer(DurationWheel owner):base(owner){}
      protected override string GetClassNameCore(){return "DurationWheel";}
      protected override AutomationControlType GetAutomationControlTypeCore(){return AutomationControlType.Spinner;}
      public override object GetPattern(PatternInterface pattern){return pattern==PatternInterface.RangeValue?this:base.GetPattern(pattern);}
      public bool IsReadOnly{get{return !Wheel.IsEnabled;}}public double LargeChange{get{return 5;}}public double SmallChange{get{return 1;}}public double Maximum{get{return Wheel.Maximum;}}public double Minimum{get{return 0;}}public double Value{get{return Wheel.Value;}}
      public void SetValue(double value){if(!Wheel.IsEnabled)throw new ElementNotEnabledException();if(Double.IsNaN(value)||Double.IsInfinity(value)||value<0||value>Maximum||value!=Math.Truncate(value))throw new ArgumentOutOfRangeException("value");Wheel.SetValue((int)value,true);}
    }
  }
}
