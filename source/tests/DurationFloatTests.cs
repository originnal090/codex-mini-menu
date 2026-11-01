using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using RingTimer;

class DurationFloatTests {
  static int checks;
  static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
  static IEnumerable<T> All<T>(DependencyObject node) where T:DependencyObject {
    if(node is T)yield return (T)node;
    for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var item in All<T>(VisualTreeHelper.GetChild(node,i)))yield return item;
  }
  static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
  static Button Duration(TimerPanel panel,string kind){return All<Button>(panel).Single(b=>AutomationProperties.GetName(b)==L.T(kind=="work"?"工作时长":"休息时长"));}
  static Border Editor(TimerPanel panel){return ((Grid)panel.Surface).Children.OfType<Border>().Single(b=>All<DurationWheel>(b).Any());}
  static Button Apply(TimerPanel panel){return All<Button>(Editor(panel)).Single(b=>(b.Content as string)==L.T("确定"));}
  static Rect Bounds(FrameworkElement element,Visual parent){return element.TransformToAncestor(parent).TransformBounds(new Rect(element.RenderSize));}
  static void CaptureMotion(MenuWindow menu,TimerPanel panel,string folder){
    string frames=Path.Combine(folder,"frames");Directory.CreateDirectory(frames);var files=new List<string>();
    Action<int> capture=count=>{for(int i=0;i<count;i++){string file=Path.Combine(frames,files.Count.ToString("D3")+".png");Theme.Snapshot(menu,file,1.25);files.Add(file);Pump(40);}};
    menu.AnimationsEnabled=true;capture(5);
    foreach(string kind in new[]{"work","rest"}){panel.OpenEditor(kind);capture(12);capture(10);panel.CancelEditor();capture(10);capture(5);}
    File.WriteAllLines(Path.Combine(folder,"frames.txt"),files);
  }
  [STAThread]static int Main(string[] args){
    var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};MenuWindow menu=null;var settings=new AppearanceSettings(null);
    try {
      Directory.CreateDirectory(args[0]);L.SetLanguage("zh-CN");Theme.Apply(true,null,null,null);
      int saves=0;var engine=new TimerEngine(new TimerState(),()=>DateTime.UtcNow);var timer=new TimerFeature(engine,()=>saves++);
      menu=new MenuWindow(new FeatureRegistry(timer,new StatsFeature(new StatsStore(null))),()=>{},()=>{},settings){Left=-2200,Top=40,AnimationsEnabled=true};
      menu.OpenFeature("timer");menu.Reveal();Pump(500);var panel=timer.Panel;var root=(Grid)panel.Surface;var page=(Grid)root.Children[0];
      Theme.Snapshot(menu,Path.Combine(args[0],"timer.png"),1.5);
      foreach(string kind in new[]{"work","rest"}) {
        var source=Duration(panel,kind);var origin=Bounds(source,root);panel.OpenEditor(kind);
        var blur=page.Effect as BlurEffect;
        Check(blur!=null&&blur.Radius<.1,kind+" blur begins at zero");
        var editor=Editor(panel);var start=Bounds(editor,root);Console.WriteLine("GEOMETRY "+kind+" origin="+origin+" sheet="+start+" size="+editor.RenderSize);
        Check(Math.Abs(start.Left-origin.Left)<2&&Math.Abs(start.Top-origin.Top)<2&&Math.Abs(start.Width-origin.Width)<2&&Math.Abs(start.Height-origin.Height)<2,kind+" sheet originates at its own island");
        Check(editor.Child.Opacity<.1,kind+" text waits for the panel to unfold");
        Check(!page.IsHitTestVisible&&!menu.CanTiltCard,kind+" editor isolates background input and card tilt");
        Theme.Snapshot(menu,Path.Combine(args[0],kind+"-start.png"),1.5);
        Pump(110);Check(blur.Radius>0&&blur.Radius<14,kind+" blur increases during lift");
        Theme.Snapshot(menu,Path.Combine(args[0],kind+"-lifting.png"),1.5);
        Pump(440);Check(Math.Abs(blur.Radius-14)<.1&&editor.Child.Opacity>.99,kind+" finishes with frosted backdrop and clear content");
        Check(editor.Background.Opacity<1&&!(editor.Effect is BlurEffect),kind+" glass is translucent and controls stay sharp");
        var count=root.Children.Count;panel.OpenEditor(kind);Check(root.Children.Count==count,kind+" repeated opening keeps one sheet");
        Theme.Snapshot(menu,Path.Combine(args[0],kind+"-open.png"),1.5);
        panel.CancelEditor();Check(panel.IsEditing&&Math.Abs(blur.Radius-14)<.2,kind+" dismiss starts continuously");
        Pump(90);Check(blur.Radius>=0&&blur.Radius<14,kind+" dismiss reduces blur");
        Pump(300);Check(!panel.IsEditing&&page.Effect==null&&page.IsHitTestVisible&&source.Opacity==1,kind+" dismiss clears effects and restores island");
      }
      Check(saves==0&&engine.State.WorkSeconds==1500&&engine.State.RestSeconds==300,"cancel leaves both durations unchanged");
      panel.OpenEditor("work");Pump(450);var wheels=All<DurationWheel>(Editor(panel)).ToArray();wheels[1].SetValue(31,false);Apply(panel).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Apply(panel).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(350);
      Check(saves==1&&engine.State.WorkSeconds==1860&&engine.State.RestSeconds==300,"work apply saves exactly once and preserves rest");
      panel.OpenEditor("rest");Pump(450);wheels=All<DurationWheel>(Editor(panel)).ToArray();wheels[1].SetValue(7,false);Apply(panel).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(350);
      Check(saves==2&&engine.State.WorkSeconds==1860&&engine.State.RestSeconds==420,"rest apply changes only rest");
      foreach(int delay in new[]{0,40,120}){panel.OpenEditor("work");Pump(delay+1);panel.CancelEditor();Pump(400);Check(!panel.IsEditing&&page.Effect==null,"early cancellation leaves no overlay at "+delay+"ms");}
      panel.OpenEditor("rest");Pump(80);menu.OpenFeature("stats");Pump(100);menu.OpenFeature("timer");Pump(250);Check(!panel.IsEditing&&page.Effect==null&&page.IsHitTestVisible,"navigation clears transient editor state");
      panel.OpenEditor("work");Pump(80);menu.HideImmediately();Pump(80);menu.Reveal();Pump(350);Check(!panel.IsEditing&&page.Effect==null,"hide and reopen clears transient editor state");
      menu.AnimationsEnabled=false;panel.OpenEditor("rest");Check(panel.IsEditing&&Math.Abs(((BlurEffect)page.Effect).Radius-14)<.1,"disabled animations opens immediately");panel.CancelEditor();Check(!panel.IsEditing&&page.Effect==null,"disabled animations closes immediately");
      menu.WorkingHeight=()=>520;menu.Refresh();Pump(80);panel.OpenEditor("work");Pump(50);
      DependencyObject ancestor=panel;while(ancestor!=null&&!(ancestor is ScrollContentPresenter))ancestor=VisualTreeHelper.GetParent(ancestor);
      var viewport=(ScrollContentPresenter)ancestor;var applyBounds=Bounds(Apply(panel),viewport);
      Check(applyBounds.Top>=0&&applyBounds.Bottom<=viewport.ActualHeight+.5,"small display keeps Apply inside the visible viewport");
      Theme.Snapshot(menu,Path.Combine(args[0],"work-small.png"),1.5);panel.CancelEditor();menu.WorkingHeight=null;menu.Refresh();Pump(80);
      panel.OpenEditor("work");wheels=All<DurationWheel>(Editor(panel)).ToArray();foreach(var wheel in wheels)wheel.SetValue(0,false);Apply(panel).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(panel.IsEditing&&engine.State.WorkSeconds==1860,"zero duration stays in editor without saving");
      wheels[0].SetValue(24,false);Check(wheels[1].Value==0&&wheels[2].Value==0&&!wheels[1].IsEnabled&&!wheels[2].IsEnabled,"24 hours locks minutes and seconds to zero");
      Editor(panel).RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(menu),0,Key.Escape){RoutedEvent=Keyboard.PreviewKeyDownEvent});Check(!panel.IsEditing&&menu.IsVisible,"Escape dismisses editor and keeps menu open");
      panel.OpenEditor("rest");var shade=root.Children.OfType<Border>().Single(b=>!All<DurationWheel>(b).Any());var wheelEvent=new MouseWheelEventArgs(Mouse.PrimaryDevice,0,-120){RoutedEvent=UIElement.MouseWheelEvent};shade.RaiseEvent(wheelEvent);Check(wheelEvent.Handled,"backdrop blocks scrolling of timer page");
      shade.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonDownEvent});Check(!panel.IsEditing,"backdrop click dismisses editor");
      panel.OpenEditor("work");All<Button>(Editor(panel)).Single(b=>(b.Content as string)=="×").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(!panel.IsEditing,"close button dismisses editor");
      foreach(bool dark in new[]{false,true})foreach(string language in new[]{"en","zh-CN"}){
        settings.Language=language;Theme.Apply(dark,null,null,null);Pump(50);panel.OpenEditor("work");Pump(50);
        Theme.Snapshot(menu,Path.Combine(args[0],"work-"+language+(dark?"-dark":"-light")+".png"),1.5);
        Check(All<DurationWheel>(Editor(panel)).Count()==3,"all wheels remain present in "+language+" / "+dark);panel.CancelEditor();
      }
      if(args.Length>1&&args[1]=="--capture"){settings.Language="zh-CN";Theme.Apply(true,null,null,null);engine.ConfigureCycle(1500,300,1);panel.PaintState();Pump(80);CaptureMotion(menu,panel,args[0]);}
      engine.StartCycle(20,5,1);panel.PaintState();panel.OpenEditor("work");Check(!panel.IsEditing,"active timer keeps duration editing disabled");
      Console.WriteLine(checks+" duration float checks passed");return 0;
    }catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}
    finally{if(menu!=null){menu.Disposing=true;menu.Close();}settings.Dispose();app.Shutdown();}
  }
}
