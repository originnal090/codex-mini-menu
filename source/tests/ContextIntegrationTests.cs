using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Threading;
using RingTimer;
class ContextIntegrationTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static System.Collections.Generic.IEnumerable<T> All<T>(DependencyObject n) where T:DependencyObject {if(n is T)yield return (T)n;for(int i=0;i<VisualTreeHelper.GetChildrenCount(n);i++)foreach(var x in All<T>(VisualTreeHelper.GetChild(n,i)))yield return x;}
 static void Pump(){Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle,new Action(()=>{}));}
 static ContextUsage Sample(string id,long used){return new ContextUsage{ThreadId=id,Title="当前对话 · 上下文监测",Used=used,Capacity=100000,Known=true};}
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Bubble bubble=null;MenuWindow menu=null;ContextReminder reminder=null;AppearanceSettings appearance=null;
 try {
  if(args.Length>0&&args[0]=="--probe"){
   var documents=AutomationElement.FromHandle(new IntPtr(Int64.Parse(args[1]))).FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Document));
   foreach(AutomationElement doc in documents)Console.WriteLine("DOC "+doc.Current.AutomationId+" offscreen="+doc.Current.IsOffscreen+" bounds="+doc.Current.BoundingRectangle+" title="+doc.Current.Name);
   string title=ContextTracker.VisibleTitle(new IntPtr(Int64.Parse(args[1])));Check(!String.IsNullOrEmpty(title),"current Codex document title available");
   string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");var thread=ContextSelection.Match(title,ContextThreadIndex.Read(Path.Combine(root,"state_5.sqlite")));Check(thread!=null,"visible title resolves to one local thread");
   var usage=new ContextLogReader().Read(thread);Check(usage.Known,"live token usage readable");Console.WriteLine("LIVE "+thread.Id+" "+usage.Display+" "+usage.Used+"/"+usage.Capacity);
   using(var tracker=new ContextTracker(root)){tracker.Follow(new IntPtr(Int64.Parse(args[1])));var deadline=DateTime.UtcNow.AddSeconds(8);while(!tracker.Current.Known&&DateTime.UtcNow<deadline)System.Threading.Thread.Sleep(100);Check(tracker.Current.Known&&tracker.Current.ThreadId==thread.Id,"background monitor resolves the visible conversation");tracker.Follow(IntPtr.Zero);Check(!tracker.Current.Known,"leaving Codex clears the monitor immediately");}return 0;
  }
  string output=args[0];Directory.CreateDirectory(output);Theme.Apply(true,null,null,null);Theme.ApplyGlobalResources(app);L.SetLanguage("zh-CN");
  appearance=new AppearanceSettings(null);var feature=new ContextFeature();var timer=new TimerFeature(new TimerEngine(new TimerState(),()=>DateTime.UtcNow),()=>{});var stats=new StatsFeature(new StatsStore(null));
  var registry=FeatureRegistry.Load(Path.GetFullPath(Path.Combine(output,"..","..","..","plugin","modules")),timer,stats,feature);
  Check(registry.Find("context")==feature,"third module loads from manifest");
  menu=new MenuWindow(registry,()=>{},()=>{},appearance){Left=-1800,Top=20,AnimationsEnabled=false};menu.Reveal();Pump();Check(All<Button>(menu).Any(b=>AutomationProperties.GetName(b)=="打开上下文"),"context module appears on home");Theme.Snapshot(menu,Path.Combine(output,"home.png"),1.5);
  menu.OpenFeature("context");feature.Update(Sample("a",90000));Pump();Check(menu.ActiveId=="context"&&All<TextBlock>(menu).Any(x=>x.Text=="90%"),"context page shows live percentage");Theme.Snapshot(menu,Path.Combine(output,"context-dark.png"),1.5);
  bubble=new Bubble{Left=-1700,Top=650};bubble.Show();bubble.SetStatus("25:00","running",.6);bubble.SetContext(Sample("a",79000));Pump();Theme.Snapshot(bubble,Path.Combine(output,"bubble-normal.png"),4);
  Check(All<TextBlock>(bubble).Any(x=>x.Text=="79%"),"launcher center shows context percentage");
  var ring=All<System.Windows.Shapes.Path>(bubble).First();var geometry=ring.Data;
  bubble.SetContext(Sample("a",80000));Check(All<TextBlock>(bubble).Single().Foreground==ContextFeature.Ink(Sample("a",80000)),"80 percent paints warning color");Theme.Snapshot(bubble,Path.Combine(output,"bubble-warning.png"),4);
  bubble.SetContext(Sample("a",90000));Check(All<TextBlock>(bubble).Single().Foreground==ContextFeature.Ink(Sample("a",90000)),"90 percent paints critical color");Check(ring.Data==geometry,"context updates preserve timer ring geometry");Theme.Snapshot(bubble,Path.Combine(output,"bubble-critical.png"),4);
  feature.Update(Sample("b",20000));bubble.SetContext(feature.Usage);Check(All<TextBlock>(bubble).Single().Text=="20%","switching conversations replaces old percentage");
  bubble.SetContext(ContextUsage.Unknown("new","新对话"));Check(All<TextBlock>(bubble).Single().Text=="—","unknown conversation clears stale usage");
  feature.Update(Sample("a",90000));Theme.Apply(false,null,null,null);feature.Refresh();Pump();Theme.Snapshot(menu,Path.Combine(output,"context-light.png"),1.5);
  reminder=new ContextReminder(Sample("a",90000),()=>{}){Left=-1800,Top=100};Check(!reminder.ShowActivated,"reminder does not steal focus");reminder.Show();Pump();Check(All<Button>(reminder).Count()==2,"reminder can be dismissed or inspected");Theme.Snapshot(reminder,Path.Combine(output,"reminder.png"),1.5);
  L.SetLanguage("en");feature.Refresh();Pump();Theme.Snapshot(menu,Path.Combine(output,"context-english.png"),1.5);Check(All<TextBlock>(menu).Any(x=>x.Text=="Context used"),"context page follows language preference");
  Console.WriteLine(checks+" context integration checks passed");return 0;
 }catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}finally{if(reminder!=null)reminder.Close();if(bubble!=null)bubble.Close();if(menu!=null){menu.Disposing=true;menu.Close();}if(appearance!=null)appearance.Dispose();app.Shutdown();}}
}
