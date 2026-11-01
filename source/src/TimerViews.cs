using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ShapePath=System.Windows.Shapes.Path;

namespace RingTimer {
  static partial class Theme {
    public const int ControlRadius=22,MenuInset=24,MenuRadius=34,MenuWidth=368,MenuContentWidth=302;
    public const int RowInset=16,RowIconColumn=28;
    static readonly Dictionary<string,SolidColorBrush> brushes=new Dictionary<string,SolidColorBrush>();
    public static SolidColorBrush Brush(string hex){SolidColorBrush b;if(brushes.TryGetValue(hex,out b))return b;b=(SolidColorBrush)new BrushConverter().ConvertFromInvariantString(hex);b.Freeze();brushes.Add(hex,b);return b;}
    // #282828 card. Line/Muted are chosen so that borders stay visible on the card
    // (Line is about 2.1:1 against the card instead of the previous invisible 1.5:1).
    public static readonly Brush Background=Live("Background","#282828"),Foreground=Live("Foreground","#ECECEC"),Muted=Live("Muted","#A6A6A6"),Field=Live("Field","#1E1E1E"),Line=Live("Line","#5E5E5E");
    public static readonly Brush Rim=Live("Rim","#3E3E3E"),RimHover=Live("RimHover","#7C7C7C"),Track=Live("Track","#4A4A4A"),Running=Live("Running","#C8C8C8"),Paused=Live("Paused","#D4B575"),Done=Live("Done","#99D5AB");
    public static readonly FontFamily ChineseFont=new FontFamily("Microsoft YaHei");
    public static readonly FontFamily NumberFont=LoadNumberFont();
    public static readonly FontFamily Font=CreateTextFont();
    static FontFamily LoadNumberFont(){
      string root=AppDomain.CurrentDomain.BaseDirectory;
      foreach(string relative in new[]{"../assets/fonts","../plugin/assets/fonts","../../plugin/assets/fonts"}){
        string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(root,relative));
        if(File.Exists(System.IO.Path.Combine(folder,"Roboto-Regular.ttf")))return new FontFamily(new Uri(folder+System.IO.Path.DirectorySeparatorChar),"./#Roboto");
      }
      return new FontFamily("Roboto");
    }
    static FontFamily CreateTextFont(){
      var family=new FontFamily();
      // Keep the family separator literal: resolving a Windows file URI escapes '#' as a filename.
      string numbers=NumberFont.BaseUri==null?NumberFont.Source:NumberFont.BaseUri.AbsoluteUri+"#Roboto";
      family.FamilyMaps.Add(new FontFamilyMap{Unicode="0030-0039,003A,0025",Target=numbers});
      family.FamilyMaps.Add(new FontFamilyMap{Target="Microsoft YaHei"});
      return family;
    }
    static Style buttonStyle,primaryStyle,bareStyle;
    const string ButtonTemplate=@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type Button}'>
      <Setter Property='Cursor' Value='Hand'/><Setter Property='BorderThickness' Value='1'/>
      <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type Button}'>
        <Border x:Name='surface' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='__RADIUS__'>
          <Grid><ContentPresenter Margin='{TemplateBinding Padding}' HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center' TextElement.FontFamily='{TemplateBinding FontFamily}' TextElement.FontSize='{TemplateBinding FontSize}' TextElement.FontWeight='{TemplateBinding FontWeight}' TextElement.Foreground='{TemplateBinding Foreground}'/><Border x:Name='depth' IsHitTestVisible='False' Opacity='0' CornerRadius='__RADIUS__'><Border.Background><LinearGradientBrush StartPoint='0,0' EndPoint='0,1'><GradientStop Color='#88000000' Offset='0'/><GradientStop Color='#22000000' Offset='.28'/><GradientStop Color='#00000000' Offset='.6'/></LinearGradientBrush></Border.Background><Border BorderBrush='#22000000' BorderThickness='1.5,2,1.5,0' CornerRadius='__RADIUS__'/></Border></Grid>
        </Border>
        <ControlTemplate.Triggers>
          <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='surface' Property='Background' Value='__HOVER__'/></Trigger>
          <Trigger Property='IsPressed' Value='True'><Setter TargetName='depth' Property='Opacity' Value='1'/><Setter TargetName='surface' Property='Background' Value='__PRESS__'/></Trigger>
          <Trigger Property='IsEnabled' Value='False'><Setter TargetName='surface' Property='Opacity' Value='0.45'/></Trigger>
        </ControlTemplate.Triggers>
      </ControlTemplate></Setter.Value></Setter>
    </Style>";
    // Hover and press paint a real surface instead of fading the whole button, so a
    // transparent (ghost) button gains a visible hover state instead of dimming.
    public static Style ButtonStyle{get{if(buttonStyle==null)buttonStyle=MakeStyle(IslandHover,IslandPressed);return buttonStyle;}}
    public static Style PrimaryStyle{get{if(primaryStyle==null)primaryStyle=MakeStyle(PrimaryHover,PrimaryPressed);return primaryStyle;}}
    public static Style RoundedButtonStyle(bool primary,int radius){return MakeStyle(primary?PrimaryHover:IslandHover,primary?PrimaryPressed:IslandPressed,radius);}
    static Style MakeStyle(Brush hover,Brush press,int radius=ControlRadius){var style=(Style)XamlReader.Parse(ButtonTemplate.Replace("__RADIUS__",radius.ToString(CultureInfo.InvariantCulture)).Replace("__HOVER__","{DynamicResource Hover}").Replace("__PRESS__","{DynamicResource Press}"));style.Resources["Hover"]=hover;style.Resources["Press"]=press;return style;}
    // Keyboard-only focus ring: WPF shows a FocusVisualStyle for keyboard navigation
    // and not for a plain mouse click, unlike an IsKeyboardFocused trigger.
    static Style focusRing;
    public static Style FocusRing{get{if(focusRing==null)focusRing=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type Control}'>
      <Setter Property='Template'><Setter.Value><ControlTemplate><Border BorderBrush='{DynamicResource MenuFocus}' BorderThickness='2' CornerRadius='9' Margin='1'/></ControlTemplate></Setter.Value></Setter></Style>");focusRing.Resources["MenuFocus"]=Focus;return focusRing;}}
    // The floating button must not paint a rounded square behind its circular face.
    public static Style BareStyle{get{if(bareStyle==null)bareStyle=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type Button}'>
      <Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Cursor' Value='Hand'/>
      <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type Button}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></ControlTemplate></Setter.Value></Setter></Style>");return bareStyle;}}
    public static Button Button(string text,bool primary){var result=new Button{Content=text,Style=primary?PrimaryStyle:ButtonStyle,FocusVisualStyle=FocusRing,HorizontalContentAlignment=HorizontalAlignment.Center,FontFamily=Font,FontSize=14,Foreground=primary?PrimaryText:Foreground,Background=primary?Primary:IslandFill,BorderBrush=primary?Brushes.Transparent:IslandEdge,Padding=new Thickness(0),Height=44,Effect=IslandElevation()};L.Bind(result,ContentControl.ContentProperty,text);MicroMotion.Attach(result);return result;}
    public static TextBlock Text(string text,double size,Brush color){var result=new TextBlock{FontFamily=Font,FontSize=size,Foreground=color};L.Bind(result,TextBlock.TextProperty,text);return result;}
    // WPF defaults assume a light theme: a dark card would still pop a white tooltip.
    public static void ApplyGlobalResources(Application app){
      if(app==null)return;
      var tip=new Style(typeof(ToolTip));
      tip.Setters.Add(new Setter(ToolTip.BackgroundProperty,Background));
      tip.Setters.Add(new Setter(ToolTip.ForegroundProperty,Foreground));
      tip.Setters.Add(new Setter(ToolTip.BorderBrushProperty,Line));
      tip.Setters.Add(new Setter(ToolTip.BorderThicknessProperty,new Thickness(1)));
      tip.Setters.Add(new Setter(ToolTip.PaddingProperty,new Thickness(8,4,8,4)));
      tip.Setters.Add(new Setter(ToolTip.FontFamilyProperty,Font));
      tip.Setters.Add(new Setter(ToolTip.FontSizeProperty,12.0));
      app.Resources[typeof(ToolTip)]=tip;
    }
    public static void Snapshot(FrameworkElement visual,string file,double scale){
      visual.UpdateLayout();int w=(int)Math.Ceiling(visual.ActualWidth*scale),h=(int)Math.Ceiling(visual.ActualHeight*scale);
      var bitmap=new RenderTargetBitmap(w,h,96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(visual);
      var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(file))encoder.Save(stream);
    }
  }
  abstract class TimerWindow:Window {
    internal long PositionUpdates {get;private set;}
    public IntPtr Handle {get{return new WindowInteropHelper(this).EnsureHandle();}}
    protected TimerWindow(){WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;Topmost=false;WindowStartupLocation=WindowStartupLocation.Manual;FontFamily=Theme.Font;TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);TextOptions.SetTextRenderingMode(this,TextRenderingMode.Grayscale);SourceInitialized+=(s,e)=>{var h=new WindowInteropHelper(this).Handle;HwndSource.FromHwnd(h).AddHook(WindowMessage);GlassBackdrop.Enable(h);};}
    int regionWidth,regionHeight;double regionScale;
    protected virtual void UpdateWindowShape(){var r=PixelBounds;double scale=Native.ScaleFor(Handle);if(regionWidth!=r.Width||regionHeight!=r.Height||regionScale!=scale){regionWidth=r.Width;regionHeight=r.Height;regionScale=scale;GlassBackdrop.Shape(Handle,r.Width,r.Height,scale,this is Bubble);}}
    IntPtr WindowMessage(IntPtr window,int message,IntPtr w,IntPtr l,ref bool handled){if(message==NativeFollower.MoveMessage){NativeFollower.Deliver(window,w,l);handled=true;}else if(message==5||message==0x2E0||message==0x47)UpdateWindowShape();return IntPtr.Zero;}
    public Native.RECT PixelBounds {get{Native.RECT r;Native.GetWindowRect(Handle,out r);return r;}}
    public void MovePixels(int x,int y){var current=PixelBounds;if(current.Left==x&&current.Top==y)return;if(Native.SetWindowPos(Handle,IntPtr.Zero,x,y,0,0,0x0015))PositionUpdates++;}
    public void SetBoundsPixels(int x,int y,int width,int height){var current=PixelBounds;if(current.Left==x&&current.Top==y&&current.Width==width&&current.Height==height)return;if(Native.SetWindowPos(Handle,IntPtr.Zero,x,y,width,height,0x0014))PositionUpdates++;}
  }
  sealed class Bubble:TimerWindow {
    readonly Ellipse ring;readonly ShapePath arc;readonly Border surface;readonly Button button;readonly TextBlock contextText;string timerStatus="";
    public bool Dragging {get;private set;}bool moved,pending,renderAttached;int pendingX,pendingY;Native.POINT down;Native.RECT origin;
    string paintedKey;
    public Action Clicked,Dragged,PositionChanged,DragStarted,DragEnded;
    public Func<Native.RECT?> DragArea;
    public Bubble(){
      // A fixed, device-independent square determines both the ring geometry and its hit target.
      Width=44;Height=44;ShowActivated=false;Title="";
      L.Bind(this,AutomationProperties.NameProperty,"小菜单悬浮按钮");
      button=new Button{Style=Theme.BareStyle,Background=Brushes.Transparent,BorderBrush=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0),ToolTip="打开小菜单 · 拖动换位置",Focusable=true};
      surface=new Border{Width=44,Height=44,CornerRadius=new CornerRadius(22),Background=Theme.GlassFill,BorderBrush=Theme.GlassRim(),BorderThickness=new Thickness(1)};
      var canvas=new Canvas{Width=28,Height=28,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};surface.Child=canvas;
      ring=new Ellipse{Width=24,Height=24,Stroke=Theme.Running,StrokeThickness=2};Canvas.SetLeft(ring,2);Canvas.SetTop(ring,2);canvas.Children.Add(ring);
      // Remaining-time arc: the ring is a track while a timer runs, so the floating
      // button reports progress without opening the panel.
      arc=new ShapePath{Stroke=Theme.Running,StrokeThickness=2,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,Visibility=Visibility.Collapsed,IsHitTestVisible=false};
      canvas.Children.Add(arc);
      contextText=Theme.Text("—",10,Theme.Muted);contextText.FontFamily=Theme.NumberFont;contextText.FontWeight=FontWeights.SemiBold;
      var contextBox=new Viewbox{Width=20,Height=16,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Child=contextText,IsHitTestVisible=false};Canvas.SetLeft(contextBox,4);Canvas.SetTop(contextBox,6);canvas.Children.Add(contextBox);
      L.Bind(button,FrameworkElement.ToolTipProperty,"打开小菜单 · 拖动换位置");button.Content=surface;Content=button;
      button.MouseEnter+=(s,e)=>PaintHover(true);button.MouseLeave+=(s,e)=>PaintHover(false);
      button.Click+=(s,e)=>{if(!moved&&Clicked!=null)Clicked();};
      button.PreviewMouseLeftButtonDown+=(s,e)=>{Native.POINT p;Native.GetCursorPos(out p);surface.Background=Theme.Pressed;BeginDrag(p);};
      button.PreviewMouseMove+=(s,e)=>{if(!Dragging||e.LeftButton!=MouseButtonState.Pressed)return;Native.POINT p;Native.GetCursorPos(out p);UpdateDrag(p);};
      button.PreviewMouseLeftButtonUp+=(s,e)=>{bool dragged=moved;EndDrag();PaintHover(button.IsMouseOver);if(dragged){e.Handled=true;button.ReleaseMouseCapture();}};
      button.LostMouseCapture+=(s,e)=>EndDrag();
      Closed+=(s,e)=>{DetachRendering();Dragging=false;pending=false;};
    }
    void PaintHover(bool over){if(Dragging)return;surface.Background=Theme.GlassFill;surface.BorderBrush=over?Theme.RimHover:Theme.GlassRim();}
    internal void BeginDrag(Native.POINT point){if(DragStarted!=null)DragStarted();Dragging=true;moved=false;pending=false;down=point;origin=PixelBounds;}
    internal void UpdateDrag(Native.POINT point){
      if(!Dragging)return;int dx=point.X-down.X,dy=point.Y-down.Y;if(Math.Abs(dx)+Math.Abs(dy)>5)moved=true;if(!moved)return;
      pendingX=origin.Left+dx;pendingY=origin.Top+dy;pending=true;
      if(!renderAttached){CompositionTarget.Rendering+=OnDragFrame;renderAttached=true;}
    }
    void OnDragFrame(object sender,EventArgs args){FlushDragFrame();}
    internal void FlushDragFrame(){if(!pending)return;pending=false;var bounds=DragArea==null?null:DragArea();if(bounds.HasValue){var area=bounds.Value;var b=PixelBounds;pendingX=Math.Max(area.Left,Math.Min(Math.Max(area.Left,area.Right-b.Width),pendingX));pendingY=Math.Max(area.Top,Math.Min(Math.Max(area.Top,area.Bottom-b.Height),pendingY));}MovePixels(pendingX,pendingY);if(PositionChanged!=null)PositionChanged();}
    void DetachRendering(){if(renderAttached){CompositionTarget.Rendering-=OnDragFrame;renderAttached=false;}}
    internal void EndDrag(){if(!Dragging)return;FlushDragFrame();DetachRendering();bool didMove=moved;Dragging=false;moved=false;if(didMove&&Dragged!=null)Dragged();if(DragEnded!=null)DragEnded();PaintHover(button.IsMouseOver);}
    public void PaintState(){}
    public void SetContext(ContextUsage usage){contextText.Text=usage.Display;contextText.Foreground=usage.Known?ContextFeature.Ink(usage):Theme.Muted;string name=L.T("已用上下文")+" "+usage.Display;AutomationProperties.SetName(contextText,name);AutomationProperties.SetName(button,L.T("打开小菜单")+" · "+name);button.ToolTip=L.T("打开小菜单")+" · "+timerStatus+"\n"+name;}
    public void SetStatus(string text,string status){SetStatus(text,status,1);}
    // progress is the remaining fraction (1 = full ring, 0 = empty).
    public void SetStatus(string text,string status,double progress){
      if(progress<0)progress=0;if(progress>1)progress=1;
      int step=(int)Math.Round(progress*720);string key=text+"|"+status+"|"+step;
      if(paintedKey==key)return;paintedKey=key;
      timerStatus=text;button.ToolTip=L.T("打开小菜单")+" · "+text;
      Brush color=status=="finished"?Theme.Done:status=="paused"?Theme.Paused:Theme.Running;
      bool full=status=="idle"||status=="finished"||progress>=0.999||step==720;
      ring.Stroke=full?(status=="idle"?Theme.Running:color):Theme.Track;
      arc.Stroke=color;
      if(full||progress<=0.002){arc.Visibility=Visibility.Collapsed;}
      else{arc.Data=ArcGeometry(progress);arc.Visibility=Visibility.Visible;}
    }
    static Geometry ArcGeometry(double fraction){
      const double center=14,radius=12;double angle=fraction*Math.PI*2;
      var figure=new PathFigure{StartPoint=new Point(center,center-radius)};
      figure.Segments.Add(new ArcSegment{Point=new Point(center+radius*Math.Sin(angle),center-radius*Math.Cos(angle)),Size=new Size(radius,radius),IsLargeArc=fraction>0.5,SweepDirection=SweepDirection.Clockwise});
      var geometry=new PathGeometry();geometry.Figures.Add(figure);geometry.Freeze();return geometry;
    }
  }

}
