using System;using System.Runtime.InteropServices;using System.Windows;using System.Windows.Threading;using RingTimer;
class LauncherInputTests {
 [DllImport("user32.dll")]static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")]static extern void mouse_event(uint flags,uint x,uint y,uint data,UIntPtr extra);
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Native.POINT p);
 static int checks,failed,clicks;static MenuWindow menu;static Bubble bubble;
 static void Check(bool ok,string label){checks++;if(!ok)failed++;Console.WriteLine((ok?"PASS ":"FAIL ")+label);}
 static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 static void Pointer(int x,int y){SetCursorPos(x,y);Pump(35);}
 static void Down(){mouse_event(2,0,0,0,UIntPtr.Zero);Pump(60);}
 static void Up(){mouse_event(4,0,0,0,UIntPtr.Zero);Pump(80);}
 static void Drag(string label,double fraction){var b=bubble.PixelBounds;var m=menu.PixelBounds;int x=b.Left+(int)(b.Width*fraction),y=b.Top+b.Height/2;Pointer(x,y);Console.WriteLine("HIT "+label+" "+WindowFromPoint(new Native.POINT{X=x,Y=y})+" menu="+menu.Handle+" bubble="+bubble.Handle);Down();Check(bubble.Dragging,label+" mouse press starts launcher drag");Pointer(x-70,y);Up();var after=bubble.PixelBounds;Check(after.Left==b.Left-70&&after.Top==b.Top,label+" real mouse moves launcher");var card=menu.PixelBounds;Check(card.Left==m.Left-70&&card.Top==m.Top,label+" card follows launcher");Check(!bubble.Dragging&&clicks==0,label+" release ends drag without toggling menu");}
 [STAThread]static int Main(){Native.SetProcessDPIAware();Native.POINT original;Native.GetCursorPos(out original);var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};try{
 Theme.Apply(true,null,null,null);var timer=new TimerFeature(new TimerEngine(new TimerState(),()=>DateTime.UtcNow),()=>{});menu=new MenuWindow(new FeatureRegistry(timer,new StatsFeature(new StatsStore(null))),()=>{},()=>{}){AnimationsEnabled=false,Topmost=true};bubble=new Bubble();bubble.Show();bubble.MovePixels(950,850);menu.Launcher=bubble;menu.AnchorBounds=()=>bubble.PixelBounds;menu.PresentationRequested=()=>{var size=menu.MeasurePopup();double dpi=Native.ScaleFor(menu.Handle);menu.SetBoundsPixels(400,100,(int)Math.Round(size.Width*dpi),(int)Math.Round(size.Height*dpi));MenuApplication.PlacePanel(bubble,menu,-1);};bubble.PositionChanged=()=>MenuApplication.PlacePanel(bubble,menu,-1);bubble.Clicked=()=>{clicks++;menu.ClosePanel();};menu.Reveal();menu.Activate();Pump(250);menu.RestoreCardContour();Pump(80);
 Drag("opened home / clear face",.18);menu.OpenFeature("timer");Pump(150);menu.Activate();Drag("timer / ring",.27);
 menu.CardPresenter.Begin(new Point(180,200));menu.CardPresenter.Move(new Point(300,80));menu.CardPresenter.Release();Pump(400);Drag("after tilt return",.5);
 var b=bubble.PixelBounds;Pointer(b.Left+b.Width/2,b.Top+b.Height/2);Down();Up();Check(clicks==1&&!menu.IsVisible,"click on docked launcher closes menu exactly once");
 menu.Reveal();menu.Activate();Pump(150);clicks=0;Drag("reopened menu",.18);
 Console.WriteLine(checks+" launcher input checks, "+failed+" failures");return failed==0?0:1;
 }catch(Exception e){Console.WriteLine(e);return 1;}finally{mouse_event(4,0,0,0,UIntPtr.Zero);SetCursorPos(original.X,original.Y);if(menu!=null){menu.Disposing=true;menu.Close();}if(bubble!=null)bubble.Close();app.Shutdown();}}
}
