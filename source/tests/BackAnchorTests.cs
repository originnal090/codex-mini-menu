using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RingTimer;
class BackAnchorTests {
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static void Wait(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 static Point Center(Button b){return b.PointToScreen(new Point(b.ActualWidth/2,b.ActualHeight/2));}
 static double Distance(Point a,Point b){return (a-b).Length;}
 [STAThread]static int Main(string[] args){Native.SetProcessDPIAware();var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};MenuWindow menu=null;Bubble bubble=null;
 try{Directory.CreateDirectory(args[0]);Theme.Apply(true,null,null,null);
  var area=System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
  var timer=new TimerEngine(new TimerState(),()=>DateTime.UtcNow);timer.Start(900);long deadline=timer.State.Deadline;
  bubble=new Bubble();bubble.Show();bubble.MovePixels(area.Right-120,area.Bottom-70);
  menu=new MenuWindow(new FeatureRegistry(new TimerFeature(timer,()=>{})),()=>{},()=>{}){AnimationsEnabled=true,Above=true};
  menu.AnchorBounds=()=>bubble.PixelBounds;
  menu.PresentationRequested=()=>{var size=menu.MeasurePopup();double scale=Native.ScaleFor(menu.Handle);var b=bubble.PixelBounds;int height=(int)Math.Round(size.Height*scale),width=(int)Math.Round(size.Width*scale);int bottom=b.Bottom+(int)Math.Round(14*scale);menu.SetBoundsPixels(b.Right+(int)(14*scale)-width,menu.Above?bottom-height:b.Top-(int)(14*scale),width,height);};
  bubble.PositionChanged=()=>menu.PresentationRequested();
  menu.OpenSettings();menu.Reveal();Wait(350);
  var back=(Button)typeof(MenuWindow).GetField("back",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(menu);
  IntPtr handle=menu.Handle;Point anchor=Center(back);var heights=new List<int>();
  Check(back.ActualHeight>=40,"return hit target remains generous");
  foreach(string page in new[]{"Language","Appearance","Sound"}){
   double maxDrift=0;EventHandler sample=(s,e)=>maxDrift=Math.Max(maxDrift,Distance(Center(back),anchor));CompositionTarget.Rendering+=sample;
   typeof(MenuWindow).GetMethod("Open"+page+"Settings").Invoke(menu,null);Wait(260);CompositionTarget.Rendering-=sample;
   Console.WriteLine(page+" return drift="+maxDrift.ToString("F2"));Check(maxDrift<=1.5,"return stays fixed throughout "+page+" resizing");
   heights.Add(menu.PixelBounds.Height);
   Point hit=back.TranslatePoint(new Point(back.ActualWidth/2,back.ActualHeight/2),menu);var target=menu.InputHitTest(hit) as DependencyObject;
   while(target!=null&&target!=back)target=VisualTreeHelper.GetParent(target);
   Check(target==back,"return remains clickable in "+page);
   Theme.Snapshot(menu,Path.Combine(args[0],page+"-dark.png"),1.5);
   back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Wait(260);Check(menu.ActiveId=="settings"&&Distance(Center(back),anchor)<=1.5,"return lands in settings without moving");
  }
  Check(heights.Distinct().Count()==3,"original content-dependent page heights remain");
  for(int i=0;i<8;i++){menu.OpenSoundSettings();Wait(18);back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Wait(18);menu.OpenLanguageSettings();Wait(18);back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
  Wait(300);Check(menu.ActiveId=="settings"&&menu.Handle==handle&&Distance(Center(back),anchor)<=1.5,"rapid navigation settles without replacing window or moving return");
  menu.OpenSoundSettings();Wait(24);Point before=Center(back);bubble.BeginDrag(new Native.POINT{X=0,Y=0});bubble.UpdateDrag(new Native.POINT{X=-36,Y=-18});bubble.FlushDragFrame();Point after=Center(back);bubble.EndDrag();Wait(300);
  Check(Math.Abs(after.X-before.X+36)<=1.5&&Math.Abs(after.Y-before.Y+18)<=1.5,"return follows drag in the same frame during resizing");
  Check(Distance(Center(back),after)<=1.5,"return does not jump after drag settles");
  menu.ClosePanel();Wait(65);menu.Reveal();Wait(330);Check(menu.IsVisible&&!menu.IsClosing&&Distance(Center(back),after)<=1.5,"collapse reversal preserves return position");
  menu.Above=false;bubble.MovePixels(area.Right-120,area.Top+80);menu.PresentationRequested();menu.OpenSettings();Wait(280);Point below=Center(back);menu.OpenLanguageSettings();Wait(280);
  Check(Distance(Center(back),below)<=1.5,"screen-edge mirrored layout keeps return beside launcher");
  L.SetLanguage("en");Theme.Apply(false,null,null,null);Wait(250);Theme.Snapshot(menu,Path.Combine(args[0],"Language-light-en.png"),1.5);
  Check(timer.State.Deadline==deadline&&timer.State.Status=="running","navigation preserves running timer");
  Console.WriteLine(checks+" return anchor checks passed");return 0;
 }catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}finally{if(menu!=null){menu.Disposing=true;menu.Close();}if(bubble!=null)bubble.Close();app.Shutdown();}}
}
