using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Automation;
class WindowTests {
  delegate bool Visitor(IntPtr h,IntPtr unused);
  [DllImport("user32.dll")]static extern bool EnumWindows(Visitor callback,IntPtr x);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint id);
  [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out RECT r);
  [DllImport("user32.dll")]static extern int GetWindowText(IntPtr h,StringBuilder text,int max);
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
  [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")]static extern IntPtr GetWindow(IntPtr h,uint command);
  struct RECT{public int L,T,R,B;}
  [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
  static string output;static Process child;static int checks;
  static async Task WaitUntil(Func<bool> ready){var elapsed=Stopwatch.StartNew();while(!ready()&&elapsed.ElapsedMilliseconds<2500)await Task.Delay(50);}
  static IntPtr Find(bool panel){IntPtr result=IntPtr.Zero;EnumWindows((h,x)=>{uint id;GetWindowThreadProcessId(h,out id);var text=new StringBuilder(256);GetWindowText(h,text,256);if(id==child.Id&&IsWindowVisible(h)&&(panel?text.ToString()=="小菜单":text.Length==0))result=h;return true;},IntPtr.Zero);return result;}
  static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;File.AppendAllText(output,"PASS "+name+Environment.NewLine);}
  static void ClickBubble(IntPtr handle){var window=AutomationElement.FromHandle(handle);var button=window.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button));if(button==null)throw new Exception("No accessible timer button");((InvokePattern)button.GetCurrentPattern(InvokePattern.Pattern)).Invoke();}
  [STAThread]static void Main(string[] args){
    SetProcessDPIAware();Application.EnableVisualStyles();output=args[1];File.WriteAllText(output,"");
    var host=new Form{Text="Codex timer test fixture",StartPosition=FormStartPosition.Manual,Location=new Point(100,100),Size=new Size(1000,820),BackColor=Color.FromArgb(25,25,25)};
    host.Shown+=async(s,e)=>{
      try{
        string profile=Path.Combine(Path.GetDirectoryName(output),Path.GetFileNameWithoutExtension(output)+"-state");Directory.CreateDirectory(profile);File.WriteAllText(Path.Combine(profile,"foreground.txt"),host.Handle.ToInt64().ToString());
        child=Process.Start(new ProcessStartInfo(args[0],"--fixture-test \""+profile+"\""){UseShellExecute=false,WindowStyle=ProcessWindowStyle.Hidden});
        host.Show();ShowWindow(host.Handle,5);host.Activate();SetForegroundWindow(host.Handle);await Task.Delay(3000);
        IntPtr bubble=Find(false);
        if(bubble==IntPtr.Zero){uint foregroundPid;GetWindowThreadProcessId(GetForegroundWindow(),out foregroundPid);File.AppendAllText(output,"foreground="+foregroundPid+" fixture="+Process.GetCurrentProcess().Id+" child="+child.Id+" exited="+child.HasExited+Environment.NewLine);EnumWindows((h,x)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==child.Id){var text=new StringBuilder(256);GetWindowText(h,text,256);RECT rr;GetWindowRect(h,out rr);File.AppendAllText(output,"window="+text+" visible="+IsWindowVisible(h)+" left="+rr.L+" top="+rr.T+Environment.NewLine);}return true;},IntPtr.Zero);}
        Check(bubble!=IntPtr.Zero,"button follows foreground Codex process");
        RECT before,after;GetWindowRect(bubble,out before);host.Location=new Point(180,150);await Task.Delay(650);GetWindowRect(bubble,out after);Check(after.L-before.L==80&&after.T-before.T==50,"button tracks window movement");
        ClickBubble(bubble);await Task.Delay(450);
        IntPtr panel=Find(true);Check(panel!=IntPtr.Zero,"one click opens menu panel");
        var timerButton=AutomationElement.FromHandle(panel).FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.NameProperty,"打开计时器"));Check(timerButton!=null,"timer module appears in menu");((InvokePattern)timerButton.GetCurrentPattern(InvokePattern.Pattern)).Invoke();await Task.Delay(300);
        Check(GetWindow(bubble,4)==host.Handle&&GetWindow(panel,4)==host.Handle,"both windows belong to the selected Codex host");
        RECT hostAnchor,dialogAnchor;GetWindowRect(bubble,out hostAnchor);
        var dialog=new Form{Text="Host error dialog fixture",ShowInTaskbar=false,FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.Manual,Location=new Point(300,250),Size=new Size(420,180)};
        dialog.Show(host);host.Enabled=false;File.WriteAllText(Path.Combine(profile,"foreground.txt"),dialog.Handle.ToInt64().ToString());await Task.Delay(450);
        Check(GetWindow(bubble,4)==host.Handle&&GetWindow(panel,4)==host.Handle,"owned error dialog retains main-window ownership");
        GetWindowRect(bubble,out dialogAnchor);Check(dialogAnchor.L==hostAnchor.L&&dialogAnchor.T==hostAnchor.T,"error dialog does not move launcher into dialog bounds");
        var nested=new Form{Text="Nested dialog fixture",ShowInTaskbar=false,FormBorderStyle=FormBorderStyle.FixedDialog};nested.Show(dialog);File.WriteAllText(Path.Combine(profile,"foreground.txt"),nested.Handle.ToInt64().ToString());await Task.Delay(350);Check(GetWindow(bubble,4)==host.Handle,"nested dialog resolves original main window");nested.Close();dialog.Close();host.Enabled=true;
        File.WriteAllText(Path.Combine(profile,"foreground.txt"),host.Handle.ToInt64().ToString());await Task.Delay(350);Check(Find(false)!=IntPtr.Zero&&GetWindow(bubble,4)==host.Handle,"closing error dialog keeps launcher usable");
        var tool=new Form{Text="Standalone tool fixture",ShowInTaskbar=false,FormBorderStyle=FormBorderStyle.FixedToolWindow};tool.Show();File.WriteAllText(Path.Combine(profile,"foreground.txt"),tool.Handle.ToInt64().ToString());await Task.Delay(350);Check(Find(false)==IntPtr.Zero,"unowned tool window is not a host");tool.Close();File.WriteAllText(Path.Combine(profile,"foreground.txt"),host.Handle.ToInt64().ToString());await Task.Delay(350);bubble=Find(false);ClickBubble(bubble);await Task.Delay(350);panel=Find(true);
        timerButton=AutomationElement.FromHandle(panel).FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.NameProperty,"打开计时器"));if(timerButton!=null){((InvokePattern)timerButton.GetCurrentPattern(InvokePattern.Pattern)).Invoke();await Task.Delay(300);}
        IntPtr focusBefore=GetForegroundWindow();RECT movingStart,panelStart;GetWindowRect(bubble,out movingStart);GetWindowRect(panel,out panelStart);int hostStart=host.Left;double lagTotal=0,panelLagTotal=0;int lagMax=0,panelLagMax=0;
        for(int step=0;step<40;step++){host.Left+=4;await Task.Delay(20);RECT current,currentPanel;GetWindowRect(bubble,out current);GetWindowRect(panel,out currentPanel);int lag=Math.Abs(current.L-(movingStart.L+host.Left-hostStart)),panelLag=Math.Abs(currentPanel.L-(panelStart.L+host.Left-hostStart));lagTotal+=lag;lagMax=Math.Max(lagMax,lag);panelLagTotal+=panelLag;panelLagMax=Math.Max(panelLagMax,panelLag);}
        double lagMean=lagTotal/40;File.AppendAllText(output,"HostMoveMeanLagPx="+lagMean.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" MaxLagPx="+lagMax+Environment.NewLine);
        Check(lagMean<=8&&lagMax<=20,"continuous host movement follows within a few display frames");
        File.AppendAllText(output,"PanelMoveMeanLagPx="+(panelLagTotal/40).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" MaxLagPx="+panelLagMax+Environment.NewLine);
        Check(panelLagTotal/40<=8&&panelLagMax<=20,"open panel follows continuous host movement");
        Check(GetForegroundWindow()==focusBefore,"native following does not steal focus");
        var back=AutomationElement.FromHandle(panel).FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.NameProperty,"返回小菜单"));
        Check(back!=null,"timer return is available before transition test");((InvokePattern)back.GetCurrentPattern(InvokePattern.Pattern)).Invoke();GetWindowRect(bubble,out movingStart);GetWindowRect(panel,out panelStart);hostStart=host.Left;double transitionLag=0;int transitionMax=0;
        for(int step=0;step<20;step++){host.Left+=2;await Task.Delay(16);RECT current,currentPanel;GetWindowRect(bubble,out current);GetWindowRect(panel,out currentPanel);int lag=Math.Max(Math.Abs(current.L-movingStart.L-host.Left+hostStart),Math.Abs(currentPanel.L-panelStart.L-host.Left+hostStart));transitionLag+=lag;transitionMax=Math.Max(transitionMax,lag);}
        File.AppendAllText(output,"DuringTransitionMeanLagPx="+(transitionLag/20).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" MaxLagPx="+transitionMax+Environment.NewLine);
        Check(transitionLag/20<=8&&transitionMax<=20&&Find(true)==panel,"host follow remains responsive while the card changes height");
        ClickBubble(bubble);await Task.Delay(300);Check(Find(true)==IntPtr.Zero,"second click closes panel");
        ClickBubble(bubble);await Task.Delay(35);panel=Find(true);GetWindowRect(bubble,out movingStart);GetWindowRect(panel,out panelStart);hostStart=host.Left;double morphLag=0;int morphMax=0;
        for(int step=0;step<20;step++){host.Left+=2;await Task.Delay(16);RECT current,currentPanel;GetWindowRect(bubble,out current);GetWindowRect(panel,out currentPanel);int lag=Math.Max(Math.Abs(current.L-movingStart.L-host.Left+hostStart),Math.Abs(currentPanel.L-panelStart.L-host.Left+hostStart));morphLag+=lag;morphMax=Math.Max(morphMax,lag);}
        File.AppendAllText(output,"DuringMorphMeanLagPx="+(morphLag/20).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" MaxLagPx="+morphMax+Environment.NewLine);
        Check(morphLag/20<=8&&morphMax<=20&&Find(true)==panel,"native following stays responsive during expansion from the launcher");ClickBubble(bubble);await Task.Delay(300);
        host.WindowState=FormWindowState.Minimized;await Task.Delay(650);Check(Find(false)==IntPtr.Zero,"button hides when host is minimized");
        host.WindowState=FormWindowState.Normal;host.Activate();SetForegroundWindow(host.Handle);await Task.Delay(650);Check(Find(false)!=IntPtr.Zero,"button returns after restoring host");
        RECT resizeBefore,resizeAfter;GetWindowRect(bubble,out resizeBefore);host.Width-=120;await Task.Delay(350);GetWindowRect(bubble,out resizeAfter);Check(resizeAfter.L==resizeBefore.L-120,"right anchor tracks host resize");
        File.WriteAllText(Path.Combine(profile,"foreground.txt"),"0");await Task.Delay(350);Check(Find(false)==IntPtr.Zero&&Find(true)==IntPtr.Zero,"switching away hides ordinary menu windows");
        File.WriteAllText(Path.Combine(profile,"foreground.txt"),host.Handle.ToInt64().ToString());await Task.Delay(350);Check(Find(false)!=IntPtr.Zero,"returning to host restores launcher");
        var second=new Form{Text="Second Codex window",StartPosition=FormStartPosition.Manual,Location=new Point(80,80),Size=new Size(800,700)};second.Show();File.WriteAllText(Path.Combine(profile,"foreground.txt"),second.Handle.ToInt64().ToString());await WaitUntil(()=>GetWindow(bubble,4)==second.Handle);Check(GetWindow(bubble,4)==second.Handle,"switching Codex windows reassigns ownership");
        second.Close();await Task.Delay(350);Check(Find(false)==IntPtr.Zero,"closing host leaves no visible launcher");Check(!child.HasExited,"host closing does not terminate plugin");
        File.WriteAllText(Path.Combine(profile,"foreground.txt"),host.Handle.ToInt64().ToString());await Task.Delay(450);bubble=Find(false);Check(bubble!=IntPtr.Zero,"launcher can reattach after previous host closes");ClickBubble(bubble);await Task.Delay(300);Check(Find(true)!=IntPtr.Zero,"menu can reopen after previous host closes");
        File.AppendAllText(output,checks+" window checks passed");
      }catch(Exception ex){File.AppendAllText(output,"FAIL "+ex.Message);Environment.ExitCode=1;}
      finally{if(child!=null&&!child.HasExited){child.Kill();child.WaitForExit();}host.Close();}
    };
    Application.Run(host);
  }
}
