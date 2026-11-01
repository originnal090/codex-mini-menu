using System;using System.IO;using System.Linq;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Imaging;using System.Windows.Threading;using RingTimer;
class RotatingFieldTests {
 static int checks,failures;
 static void Check(bool ok,string label){checks++;if(!ok)failures++;Console.WriteLine((ok?"PASS ":"FAIL ")+label);}
 static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 static RenderTargetBitmap Capture(Visual v,int w,int h){var b=new RenderTargetBitmap(w,h,96,96,PixelFormats.Pbgra32);b.Render(v);return b;}
 static byte[] Pixel(BitmapSource b,int x,int y){var p=new byte[4];b.CopyPixels(new Int32Rect(x,y,1,1),p,4,0);return p;}
 static int Warmth(BitmapSource b,int x,int y){var p=Pixel(b,x,y);return p[2]-p[0];}
 static void Save(BitmapSource b,string path){var e=new PngBitmapEncoder();e.Frames.Add(BitmapFrame.Create(b));using(var stream=File.Create(path))e.Save(stream);}
 [STAThread]static int Main(string[] args){Native.SetProcessDPIAware();var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window window=null;MenuWindow menu=null;try{
 Directory.CreateDirectory(args[0]);Theme.Apply(true,null,null,null);var layer=new CardAtmosphere{Width=352,Height=610};window=new Window{Content=layer,Width=352,Height=610,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Left=-2200,Top=0};window.Show();Pump(80);
 var field=layer.Children.OfType<Border>().FirstOrDefault(b=>b.Background is ImageBrush);var rim=layer.Children.OfType<Border>().FirstOrDefault(b=>b.BorderBrush is ImageBrush);
 Check(field!=null,"diffuse angular field covers the card background");if(field==null)return 1;
 Check(field.ActualHeight>590&&field.ActualWidth>340,"light field spans the full card");Check(!layer.IsHitTestVisible,"lighting preserves pointer access");
 Check(Object.ReferenceEquals(field.Background,rim.BorderBrush),"background and border share one angular field and phase");var brush=(ImageBrush)field.Background;var rotation=(RotateTransform)brush.Transform;
 foreach(bool dark in new[]{true,false}){Theme.Apply(dark,null,null,null);layer.RefreshTheme();brush=(ImageBrush)field.Background;rotation=(RotateTransform)brush.Transform;
 foreach(int angle in new[]{0,90,180,270}){rotation.Angle=angle;Pump(30);var b=Capture(layer,352,610);Save(b,Path.Combine(args[0],(dark?"dark":"light")+"-"+angle+".png"));int cx=176,cy=305,dx=angle%180==0?145:0,dy=angle%180==0?0:275,sign=angle<180?1:-1;int coolX=cx-dx*sign,coolY=cy-dy*sign,warmX=cx+dx*sign,warmY=cy+dy*sign;Check(Warmth(b,coolX,coolY)<-2&&Warmth(b,warmX,warmY)>2,(dark?"dark":"light")+" opposite soft colors rotate through "+angle+" degrees");int bx=angle%180==0?(sign>0?1:350):176,by=angle%180==0?305:(sign>0?1:608);Check(Warmth(b,bx,by)<-2,"border follows the cool background sector at "+angle);Check(Pixel(b,176,305)[3]<5,"rotation center stays softly transparent");Check(Pixel(b,0,0)[3]==0,"rounded corner stays clear");}
 }
 rotation.Angle=45;layer.SetMotion(true);Pump(100);double before=rotation.Angle;Pump(400);double delta=rotation.Angle-before;Check(delta>5&&delta<25,"continuous rotation advances at a calm steady speed");layer.SetMotion(false);double stopped=rotation.Angle;Pump(150);Check(rotation.Angle==stopped&&!rotation.HasAnimatedProperties,"reduced motion removes the rotation clock");
 layer.Width=500;layer.Height=400;window.Width=500;window.Height=400;Pump(80);Check(Math.Abs(rotation.CenterX-249)<1&&Math.Abs(rotation.CenterY-199)<1,"rotation center follows card size");
 Theme.Apply(true,null,null,null);var timer=new TimerFeature(new TimerEngine(new TimerState(),()=>DateTime.UtcNow),()=>{});menu=new MenuWindow(new FeatureRegistry(timer,new StatsFeature(new StatsStore(null)),new ContextFeature()),()=>{},()=>{}){AnimationsEnabled=false,Left=-2200,Top=0};menu.Reveal();Pump(120);Theme.Snapshot(menu,Path.Combine(args[0],"menu.png"),1.5);menu.OpenFeature("timer");Pump(120);Theme.Snapshot(menu,Path.Combine(args[0],"timer.png"),1.5);
 Console.WriteLine(checks+" rotating field checks, "+failures+" failures");return failures==0?0:1;
 }catch(Exception e){Console.WriteLine(e);return 1;}finally{if(menu!=null){menu.Disposing=true;menu.Close();}if(window!=null)window.Close();app.Shutdown();}}
}
