using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Threading;
using RingTimer;
class MenuStructureTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static IEnumerable<T> All<T>(DependencyObject n) where T:DependencyObject {if(n is T)yield return (T)n;for(int i=0;i<VisualTreeHelper.GetChildrenCount(n);i++)foreach(var x in All<T>(VisualTreeHelper.GetChild(n,i)))yield return x;}
 static void Pump(){Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle,new Action(()=>{}));}
 [STAThread] static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};MenuWindow menu=null;var appearance=new AppearanceSettings(null);
 try{Directory.CreateDirectory(args[0]);Theme.Apply(true,null,null,null);Theme.ApplyGlobalResources(app);var now=DateTime.UtcNow;var engine=new TimerEngine(new TimerState(),()=>now);int saves=0;var timer=new TimerFeature(engine,()=>saves++);var stats=new StatsFeature(new StatsStore(null));menu=new MenuWindow(new FeatureRegistry(timer,stats),()=>{},()=>{},appearance){Left=-1800,Top=20,AnimationsEnabled=false};menu.Reveal();Pump();
 Check(menu.ActiveId==null,"launch opens small menu home");Theme.Snapshot(menu,Path.Combine(args[0],"home.png"),1.5);
 menu.OpenFeature("timer");Pump();var back=All<Button>(menu).Single(x=>AutomationProperties.GetName(x)=="返回小菜单");Check(back.IsVisible,"timer exposes return to menu");var point=back.TranslatePoint(new Point(0,0),menu);Check(point.X<menu.ActualWidth/2&&point.Y>menu.ActualHeight/2,"return is in lower left");Check(!All<CheckBox>(timer.Panel).Any(),"timer sound control lives in settings");
 var groups=All<TextBox>(timer.Panel).Single();groups.Text="3";Check(engine.State.Groups==3,"group choice is saved");Theme.Snapshot(menu,Path.Combine(args[0],"timer.png"),1.5);L.SetLanguage("en");timer.Panel.PaintState();Pump();Theme.Snapshot(menu,Path.Combine(args[0],"timer-en-idle.png"),2);timer.Panel.DoPrimary();Pump();Theme.Snapshot(menu,Path.Combine(args[0],"timer-en-running.png"),2);L.SetLanguage("zh-CN");timer.Panel.PaintState();long deadline=engine.State.Deadline;back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Check(menu.ActiveId==null&&engine.State.Deadline==deadline&&engine.State.Status=="running","return preserves active countdown");
 menu.OpenFeature("stats");Pump();Check(back.IsVisible,"statistics exposes return to menu");var rail=All<SlidingRail>(menu).Single();rail.Select(1,true);Pump();Check(((StatsPanel)stats.View).SelectedView=="total","sliding rail selects total statistics");Check(!All<Button>(menu).Any(x=>AutomationProperties.GetName(x).Contains("目标")),"daily goal controls removed");Theme.Snapshot(menu,Path.Combine(args[0],"statistics.png"),1.5);
 menu.ShowHome();menu.OpenSettings();Pump();Check(menu.ActiveId=="settings","settings overview remains accessible");Theme.Snapshot(menu,Path.Combine(args[0],"settings.png"),1.5);menu.OpenSoundSettings();Pump();var sound=All<CheckBox>(menu).Single();sound.IsChecked=false;Check(!engine.State.Sound&&saves>=3,"settings toggle saves sound preference");string state=Path.Combine(args[0],"state.json");engine.Save(state);Check(!TimerEngine.Load(state).Sound,"sound preference survives reload");Theme.Snapshot(menu,Path.Combine(args[0],"sound.png"),1.5);back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(menu.ActiveId=="settings","sound return opens settings overview");back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(menu.ActiveId==null,"settings return opens home");
 menu.OpenFeature("timer");engine.StopCycle();timer.Panel.PaintState();timer.Panel.OpenEditor("work");Pump();Check(timer.Panel.IsEditing,"duration editor opens");var apply=All<Button>(timer.Panel).Single(x=>(x.Content as string)=="确定");Check(apply.TranslatePoint(new Point(0,apply.ActualHeight),timer.Panel).Y<=timer.Panel.ActualHeight,"duration apply button fits inside panel");Check(!back.IsEnabled,"duration editing suspends back navigation");Theme.Snapshot(menu,Path.Combine(args[0],"duration.png"),1.5);timer.Panel.CancelEditor();
 Check(All<Image>(menu).Any(x=>!x.IsHitTestVisible&&x.Width==300),"mouse highlight layer preserves hit targets");Console.WriteLine(checks+" structure checks passed");return 0;
 }catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}finally{if(menu!=null){menu.Disposing=true;menu.Close();}appearance.Dispose();app.Shutdown();}}
}
