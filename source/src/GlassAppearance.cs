using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace RingTimer {
  // Appearance is stored separately: an invalid preference cannot reset a timer.
  sealed class AppearanceSettings:IDisposable {
    public sealed class Data {public int Transparency=42;public string Language="zh-CN";public string SoundFile="";}
    readonly string path;readonly DispatcherTimer save;int transparency;string language="zh-CN",soundFile="";bool dirty;
    public ReminderAudio Audio{get;private set;}
    public event Action Changed;
    public AppearanceSettings(string file){path=file;transparency=42;
      try{if(path!=null&&File.Exists(path)&&new FileInfo(path).Length<4096){var data=new JavaScriptSerializer().Deserialize<Data>(File.ReadAllText(path));transparency=Math.Max(0,Math.Min(90,data.Transparency));language=L.Normalize(data.Language);soundFile=data.SoundFile??"";}}
      catch(Exception e){if(!(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException||e is NullReferenceException))throw;}
      Audio=new ReminderAudio();save=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(300)};save.Tick+=(s,e)=>Flush();Theme.SetTransparency(transparency);L.SetLanguage(language);
    }
    public int Transparency{get{return transparency;}set{int next=Math.Max(0,Math.Min(90,value));if(next==transparency)return;transparency=next;Theme.SetTransparency(next);dirty=true;save.Stop();save.Start();if(Changed!=null)Changed();}}
    public string Language{get{return language;}set{string next=L.Normalize(value);if(next==language)return;language=next;L.SetLanguage(next);dirty=true;save.Stop();save.Start();if(Changed!=null)Changed();}}
    public string SoundFile{get{return soundFile;}set{string next=value??"";if(next==soundFile)return;soundFile=next;dirty=true;save.Stop();save.Start();if(Changed!=null)Changed();}}
    public bool SaveFailed{get;private set;}
    public void Flush(){save.Stop();if(!dirty||path==null)return;
      try{Directory.CreateDirectory(Path.GetDirectoryName(path));string tmp=path+".tmp";File.WriteAllText(tmp,new JavaScriptSerializer().Serialize(new Data{Transparency=transparency,Language=language,SoundFile=soundFile}));if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);dirty=false;SaveFailed=false;}
      catch(IOException){SaveFailed=true;}catch(UnauthorizedAccessException){SaveFailed=true;}
    }
    public void Dispose(){Flush();save.Stop();Audio.Dispose();}
  }
  static partial class Theme {
    public static readonly Brush GlassFill=Live("GlassFill","#94282828"),GlassEdge=Live("GlassEdge","#50FFFFFF"),GlassBottom=Live("GlassBottom","#16000000"),GlassControl=Live("GlassControl","#16FFFFFF"),GlassSelected=Live("GlassSelected","#34FFFFFF");
    static int transparency=42;
    public static int Transparency{get{return transparency;}}
    public static void SetTransparency(int value){transparency=Math.Max(0,Math.Min(90,value));RefreshGlass();}
    static Color Alpha(Color c,byte alpha){c.A=alpha;return c;}
    static void RefreshGlass(){
      tokens["GlassFill"].Set(Alpha(((SolidColorBrush)Background).Color,(byte)Math.Round(255*(100-transparency)/100.0)));
      tokens["GlassEdge"].Set(Alpha(Colors.White,(byte)(IsDark?58:210)));
      tokens["GlassBottom"].Set(Alpha(IsDark?Colors.White:Colors.Black,(byte)(IsDark?15:25)));
      tokens["GlassControl"].Set(Alpha(IsDark?Colors.White:Colors.Black,(byte)(IsDark?16:10)));
      tokens["GlassSelected"].Set(Alpha(IsDark?Colors.White:Colors.White,(byte)(IsDark?38:195)));RefreshIslands();
    }
    public static Brush GlassRim(){var b=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(.7,1)};
      var top=new GradientStop{Offset=0};var bottom=new GradientStop{Offset=1};
      System.Windows.Data.BindingOperations.SetBinding(top,GradientStop.ColorProperty,new System.Windows.Data.Binding("Color"){Source=GlassEdge});
      System.Windows.Data.BindingOperations.SetBinding(bottom,GradientStop.ColorProperty,new System.Windows.Data.Binding("Color"){Source=GlassBottom});b.GradientStops.Add(top);b.GradientStops.Add(bottom);return b;
    }
    public static FrameworkElement ActionLabel(string text,bool pause){var row=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
      row.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse(pause?"M 2,1 L 5,1 L 5,13 L 2,13 Z M 9,1 L 12,1 L 12,13 L 9,13 Z":"M 3,1 L 13,7 L 3,13 Z"),Fill=PrimaryText,Width=12,Height=12,Stretch=Stretch.Uniform,Margin=new Thickness(0,0,9,0),VerticalAlignment=VerticalAlignment.Center});row.Children.Add(Text(text,14,PrimaryText));return row;
    }
  }
  sealed class AppearancePanel:StackPanel {
    public readonly Slider Transparency;readonly TextBlock value,hint;readonly AppearanceSettings settings;
    public AppearancePanel(AppearanceSettings preferences){settings=preferences;
      Loaded+=(s,e)=>{settings.Changed+=UpdateValue;UpdateValue();};Unloaded+=(s,e)=>settings.Changed-=UpdateValue;
      var intro=Theme.Text("让背景透过玻璃",18,Theme.Foreground);intro.TextWrapping=TextWrapping.Wrap;intro.FontWeight=FontWeights.SemiBold;intro.Margin=new Thickness(0,8,0,8);Children.Add(intro);
      var description=Theme.Text("小菜单、计时器与悬浮按钮共用此设置。",12,Theme.Muted);description.TextWrapping=TextWrapping.Wrap;description.Margin=new Thickness(0,0,0,0);Children.Add(description);
      var row=new Grid();row.Children.Add(Theme.Text("玻璃透明度",14,Theme.Foreground));value=Theme.Text("",14,Theme.Foreground);value.HorizontalAlignment=HorizontalAlignment.Right;row.Children.Add(value);Children.Add(row);
      Transparency=new Slider{Minimum=0,Maximum=90,Value=settings.Transparency,TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=1,LargeChange=10,Height=44,Margin=new Thickness(0,10,0,0),Foreground=Theme.Foreground,FocusVisualStyle=Theme.FocusRing,IsMoveToPointEnabled=true};L.Bind(Transparency,AutomationProperties.NameProperty,"玻璃透明度，百分比");
      Transparency.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type Slider}'>
        <Grid Background='Transparent' Margin='0,0,0,0'><Border Height='5' Background='{DynamicResource GlassControl}' CornerRadius='2.5'/>
          <Track x:Name='PART_Track' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' IsDirectionReversed='False'>
            <Track.DecreaseRepeatButton><RepeatButton Command='{x:Static Slider.DecreaseLarge}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Height='5' Background='{DynamicResource Ink}' CornerRadius='2.5'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
            <Track.Thumb><Thumb Width='28' Height='44' Cursor='Hand'><Thumb.Template><ControlTemplate><Grid Background='Transparent'><Ellipse Width='24' Height='24' Fill='{DynamicResource Thumb}' Stroke='#30000000' StrokeThickness='1'/></Grid></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
            <Track.IncreaseRepeatButton><RepeatButton Command='{x:Static Slider.IncreaseLarge}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
          </Track></Grid></ControlTemplate>");
      Transparency.Resources["GlassControl"]=Theme.GlassControl;Transparency.Resources["Ink"]=Theme.Foreground;Transparency.Resources["Thumb"]=Theme.SwitchThumb;Children.Add(Transparency);
      var ends=new Grid();ends.Children.Add(Theme.Text("厚实",11,Theme.Muted));var clear=Theme.Text("通透",11,Theme.Muted);clear.HorizontalAlignment=HorizontalAlignment.Right;ends.Children.Add(clear);Children.Add(ends);
      var reset=Theme.Button("恢复默认透明度",false);reset.Height=44;reset.Margin=new Thickness(0,14,0,0);reset.Click+=(s,e)=>Transparency.Value=42;Children.Add(reset);
      hint=Theme.Text("",12,Theme.Muted);hint.MinHeight=40;hint.Margin=new Thickness(0,14,0,0);hint.TextWrapping=TextWrapping.Wrap;Children.Add(hint);
      Theme.GroupIsland(this,0,2,new Thickness(16)).Margin=new Thickness(0,0,0,14);Theme.GroupIsland(this,1,3,new Thickness(16));
      Transparency.ValueChanged+=(s,e)=>{settings.Transparency=(int)Math.Round(Transparency.Value);UpdateValue();};UpdateValue();
    }
    void UpdateValue(){value.Text=settings.Transparency+"%";L.Bind(hint,TextBlock.TextProperty,settings.SaveFailed?"当前设置已生效，暂时无法保存。":GlassBackdrop.SystemTransparencyEnabled()?"外观自动跟随 Codex，调整即时生效。":"外观跟随 Codex。系统毛玻璃已关闭，当前使用透明材质。");}
  }
  // Compositor blur is configured once, never rendered by a capture/blur loop.
  // Unsupported systems retain the translucent fill and the same interaction path.
  static class GlassBackdrop {
    [StructLayout(LayoutKind.Sequential)]struct Accent {public int State,Flags,Color,Animation;}
    [StructLayout(LayoutKind.Sequential)]struct Composition {public int Attribute;public IntPtr Data;public int Size;}
    [DllImport("user32.dll")]static extern int SetWindowCompositionAttribute(IntPtr hwnd,ref Composition data);
    [DllImport("gdi32.dll")]static extern IntPtr CreateRoundRectRgn(int left,int top,int right,int bottom,int width,int height);
    [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr value);
    [DllImport("gdi32.dll")]static extern IntPtr CreateEllipticRgn(int left,int top,int right,int bottom);
    [DllImport("gdi32.dll")]static extern int CombineRgn(IntPtr destination,IntPtr first,IntPtr second,int mode);
    [StructLayout(LayoutKind.Sequential)]struct RegionPoint{public int X,Y;public RegionPoint(Point p){X=(int)Math.Round(p.X);Y=(int)Math.Round(p.Y);}}
    [DllImport("gdi32.dll")]static extern IntPtr CreatePolygonRgn(RegionPoint[] points,int count,int mode);
    [DllImport("user32.dll")]static extern int SetWindowRgn(IntPtr window,IntPtr region,bool redraw);
    public static void ProjectedShape(IntPtr window,Point[] outline,Rect hole){
      var points=new RegionPoint[outline.Length];for(int i=0;i<points.Length;i++)points[i]=new RegionPoint(outline[i]);
      IntPtr region=CreatePolygonRgn(points,points.Length,1);if(region==IntPtr.Zero)return;
      // The launcher is an independent screen-space anchor, so its cutout stays
      // aligned with that launcher while only the outer card contour tilts.
      if(!hole.IsEmpty){IntPtr cut=CreateEllipticRgn((int)hole.Left,(int)hole.Top,(int)hole.Right+1,(int)hole.Bottom+1);if(cut!=IntPtr.Zero){CombineRgn(region,region,cut,4);DeleteObject(cut);}}
      if(SetWindowRgn(window,region,true)==0)DeleteObject(region);
    }
    public static void MorphShape(IntPtr window,Rect bounds,int radius,Rect hole){IntPtr region=CreateRoundRectRgn((int)bounds.Left,(int)bounds.Top,(int)bounds.Right+1,(int)bounds.Bottom+1,radius*2,radius*2);if(region==IntPtr.Zero)return;
      if(!hole.IsEmpty){IntPtr cut=CreateEllipticRgn((int)hole.Left,(int)hole.Top,(int)hole.Right+1,(int)hole.Bottom+1);if(cut!=IntPtr.Zero){CombineRgn(region,region,cut,4);DeleteObject(cut);}}
      if(SetWindowRgn(window,region,true)==0)DeleteObject(region);
    }
    public static void Shape(IntPtr window,int width,int height,double scale,bool bubble){int inset=bubble?0:(int)Math.Round(8*scale),diameter=(int)Math.Round((bubble?44:2*Theme.MenuRadius)*scale);if(width<=2*inset||height<=2*inset)return;IntPtr region=CreateRoundRectRgn(inset,inset,width-inset+1,height-inset+1,diameter,diameter);if(region!=IntPtr.Zero&&SetWindowRgn(window,region,true)==0)DeleteObject(region);}
    internal static bool SystemTransparencyEnabled(){try{using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")){return key==null||Convert.ToInt32(key.GetValue("EnableTransparency",1))!=0;}}catch{return false;}}
    public static bool Enable(IntPtr window){if(!SystemTransparencyEnabled())return false;IntPtr memory=IntPtr.Zero;try{var accent=new Accent{State=3};memory=Marshal.AllocHGlobal(Marshal.SizeOf(accent));Marshal.StructureToPtr(accent,memory,false);var data=new Composition{Attribute=19,Data=memory,Size=Marshal.SizeOf(accent)};return SetWindowCompositionAttribute(window,ref data)!=0;}catch(EntryPointNotFoundException){return false;}finally{if(memory!=IntPtr.Zero)Marshal.FreeHGlobal(memory);}}
  }
}
