using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RingTimer;
class SettingsTests {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Pump(){Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle,new Action(()=>{}));}
 static IEnumerable<T> All<T>(DependencyObject o) where T:DependencyObject {if(o is T)yield return (T)o;for(int i=0;i<VisualTreeHelper.GetChildrenCount(o);i++)foreach(var x in All<T>(VisualTreeHelper.GetChild(o,i)))yield return x;}
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};MenuWindow menu=null;
 try{Directory.CreateDirectory(args[0]);Check(typeof(MenuWindow).GetMethod("OpenLanguageSettings")!=null,"language has its own settings page");
 using(var prefs=new AppearanceSettings(null)){
 var engine=new TimerEngine(new TimerState(),()=>DateTime.UtcNow);engine.Start(900);long deadline=engine.State.Deadline;
 menu=new MenuWindow(new FeatureRegistry(new TimerFeature(engine,()=>{})),()=>{},()=>{},prefs){AnimationsEnabled=false,Left=180,Top=160};menu.Show();menu.OpenSettings();Pump();IntPtr hwnd=menu.Handle;
 Check(menu.ActiveId=="settings","settings opens category overview");
 var rows=All<Button>(menu).Where(b=>new[]{"语言","外观","提示音"}.Contains(AutomationProperties.GetName(b))).ToArray();
 Check(rows.Length==3,"overview has exactly three category entries");Check(rows.Select(b=>b.ActualHeight).Distinct().Count()==1&&rows[0].ActualHeight>=64,"category entries have equal generous hit targets");Check(rows.All(b=>b.Margin==rows[0].Margin&&b.Padding==rows[0].Padding),"category spacing and padding are consistent");
 Check(!All<Slider>(menu).Any()&&!All<CheckBox>(menu).Any(),"overview keeps detailed controls inside category pages");
 Theme.Apply(true,null,null,null);Theme.Snapshot(menu,Path.Combine(args[0],"settings-zh-dark.png"),1.5);
 string[] ids={"language","appearance","sound"};
 for(int i=0;i<3;i++){rows[i].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Check(menu.ActiveId==ids[i]&&menu.Handle==hwnd,"category opens in the same native window: "+ids[i]);var back=All<Button>(menu).First(b=>AutomationProperties.GetName(b)=="返回设置");back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Check(menu.ActiveId=="settings","back returns to category overview: "+ids[i]);}
 prefs.Language="en";Pump();Check(All<TextBlock>(menu).Any(t=>t.Text=="Appearance")&&All<TextBlock>(menu).Any(t=>t.Text=="Sound"),"overview translates immediately");Theme.Apply(false,null,null,null);Theme.Snapshot(menu,Path.Combine(args[0],"settings-en-light.png"),1.5);
 foreach(string method in new[]{"OpenLanguageSettings","OpenAppearanceSettings","OpenSoundSettings"}){typeof(MenuWindow).GetMethod(method).Invoke(menu,null);Pump();if(method=="OpenAppearanceSettings"){var hint=All<TextBlock>(menu).First(t=>t.Text.StartsWith("Appearance follows"));var card=menu.CardSurface;Check(hint.ActualHeight>=30&&hint.TranslatePoint(new Point(0,hint.ActualHeight),card).Y<=card.ActualHeight-card.Padding.Bottom+1,"English appearance hint fits inside content bounds");}Theme.Snapshot(menu,Path.Combine(args[0],method+"-en.png"),1.5);Check(menu.MeasurePopup().Height<650,"category page stays compact: "+method);menu.OpenSettings();Pump();}
 Check(engine.State.Deadline==deadline&&engine.State.Status=="running","settings navigation preserves running deadline");
 All<Button>(menu).First(b=>AutomationProperties.GetName(b)=="Back to menu").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(menu.ActiveId==null,"overview back returns to main menu");
 }
 Console.WriteLine(count+" settings checks passed");return 0;
 }catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}finally{if(menu!=null){menu.Disposing=true;menu.Close();}app.Shutdown();}}
}
