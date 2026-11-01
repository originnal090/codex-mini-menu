using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RingTimer;
class StatsViewTests {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static IEnumerable<T> All<T>(DependencyObject root) where T:DependencyObject{if(root is T)yield return (T)root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in All<T>(VisualTreeHelper.GetChild(root,i)))yield return child;}
 static void Pump(){Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle,new Action(()=>{}));}
 [STAThread]static int Main(string[] args){Native.SetProcessDPIAware();var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};MenuWindow menu=null;
  try{Directory.CreateDirectory(args[0]);var store=new StatsStore(null);var timer=new TimerEngine(new TimerState(),()=>DateTime.UtcNow);var timerFeature=new TimerFeature(timer,()=>{});var statsFeature=new StatsFeature(store);menu=new MenuWindow(new FeatureRegistry(timerFeature,statsFeature),()=>{},()=>{}){AnimationsEnabled=false,Left=-1800,Top=0};menu.OpenFeature("timer");menu.Show();Pump();double height=menu.MeasurePopup().Height;
   All<Button>(menu).First(b=>AutomationProperties.GetName(b)=="打开统计").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();var panel=(StatsPanel)statsFeature.View;
   Check(menu.ActiveId=="stats"&&Math.Abs(menu.MeasurePopup().Height-height)<.1,"timer and statistics share compact window geometry");
   panel.SelectView("sessions");Pump();Check(All<TextBlock>(panel).Any(t=>t.Text=="暂无记录"),"empty statistics show no sample history");Theme.Snapshot(menu,Path.Combine(args[0],"empty.png"),1.5);
   DateTime day=DateTime.Today;for(int i=399;i>=0;i--){if(i%5==0)continue;var start=DateTime.SpecifyKind(day.AddDays(-i).AddHours(9),DateTimeKind.Local).ToUniversalTime();timer.Start(60);timer.State.PendingStats.Add(new CompletedSession{Id="sample-"+i,Spans=new List<FocusSpan>{new FocusSpan{StartUtc=start.Ticks,EndUtc=start.AddMinutes(15+i%4*10).Ticks}}});store.Collect(timer.State);}
   foreach(int minute in new[]{620,845}){var start=DateTime.SpecifyKind(day.AddMinutes(minute),DateTimeKind.Local).ToUniversalTime();timer.State.PendingStats.Add(new CompletedSession{Id="today-"+minute,Spans=new List<FocusSpan>{new FocusSpan{StartUtc=start.Ticks,EndUtc=start.AddMinutes(25).Ticks}}});store.Collect(timer.State);}
   Check(store.Records.Any(r=>r.Slices.Any(s=>String.CompareOrdinal(s.Date,day.AddDays(-365).ToString("yyyy-MM-dd"))<0)),"history retains more than a year");
   foreach(string view in new[]{"calendar","total","sessions"}){panel.SelectView(view);Pump();Check(Math.Abs(menu.MeasurePopup().Height-height)<.1&&menu.MeasurePopup().Width==344,"fixed compact geometry for "+view);Check(panel.SelectedView==view,"view switch selects "+view);Theme.Snapshot(menu,Path.Combine(args[0],view+"-150.png"),1.5);foreach(double scale in new[]{1.0,2.0})Theme.Snapshot(menu,Path.Combine(args[0],view+"-"+(int)(scale*100)+".png"),scale);}
   panel.SelectView("calendar");Pump();var dates=All<Button>(panel).Where(b=>AutomationProperties.GetName(b).StartsWith(day.Year.ToString()+"-")).ToList();Check(dates.Count>=80,"calendar exposes accessible daily cells");All<Button>(panel).First(b=>AutomationProperties.GetName(b)=="上一页").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Theme.Snapshot(menu,Path.Combine(args[0],"calendar-previous.png"),1.5);
   panel.SelectView("total");Pump();foreach(string period in new[]{"周","月","日"}){All<Button>(panel).First(b=>(b.Content as string)==period).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Check(menu.MeasurePopup().Height==height,"aggregation switch keeps height: "+period);Theme.Snapshot(menu,Path.Combine(args[0],"total-"+(period=="周"?"week":period=="月"?"month":"day")+".png"),1.5);}
   Check(!All<TextBlock>(panel).Any(t=>t.Text.Contains("目标"))&&!All<Button>(panel).Any(b=>AutomationProperties.GetName(b).Contains("目标")),"totals contain no goal controls or labels");
   var focusedBar=All<Button>(panel).Last(b=>AutomationProperties.GetName(b).Contains(" · "));focusedBar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));focusedBar.Focus();Pump();DateTime liveNow=DateTime.UtcNow;var liveTimer=new TimerEngine(new TimerState(),()=>liveNow);liveTimer.Start(120);liveNow=liveNow.AddSeconds(30);double before=store.Day(day).Seconds;store.UpdateLive(liveTimer.CurrentSession());Pump();Check(Math.Abs(store.Day(day).Seconds-before-30)<.01&&All<TextBlock>(panel).Any(t=>t.Text==StatsPanel.Duration(before+30)),"visible summary includes running elapsed time");Check(All<Button>(panel).Any(b=>b.IsKeyboardFocused&&AutomationProperties.GetName(b).Contains(" · ")),"live refresh preserves keyboard focus on the chart");Check(All<TextBlock>(panel).Any(t=>t.Text.StartsWith(day.ToString("MM.dd")+" · ")&&t.Text.Contains("工作")&&t.Text.Contains("休息")),"live refresh preserves selected bucket details");var held=All<Button>(panel).Last(b=>AutomationProperties.GetName(b).Contains(" · "));held.CaptureMouse();liveNow=liveNow.AddSeconds(1);store.UpdateLive(liveTimer.CurrentSession());Pump();Check(All<Button>(panel).Contains(held)&&held.IsMouseCaptured,"live refresh preserves an active mouse capture");held.ReleaseMouseCapture();Pump();Check(All<TextBlock>(panel).Any(t=>t.Text==StatsPanel.Duration(before+31)),"pending live refresh applies after mouse release");Theme.Snapshot(menu,Path.Combine(args[0],"live-total.png"),1.5);
   L.SetLanguage("en");Pump();Check(All<TextBlock>(panel).Any(t=>t.Text=="Minutes"),"statistics labels follow language");Theme.Apply(false,null,null,null);Theme.Snapshot(menu,Path.Combine(args[0],"total-en-light.png"),1.5);L.SetLanguage("zh-CN");
   All<Button>(menu).First(b=>AutomationProperties.GetName(b)=="打开计时器").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Check(menu.ActiveId=="timer"&&timer.State.Status=="running","navigation preserves the running timer");
   Console.WriteLine(count+" statistics view checks passed");return 0;
  }catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}finally{if(menu!=null){menu.Disposing=true;menu.Close();}app.Shutdown();}
 }
}
