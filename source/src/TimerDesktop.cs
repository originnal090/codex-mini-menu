using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CompositionTarget=System.Windows.Media.CompositionTarget;
using Forms=System.Windows.Forms;

namespace RingTimer {
  static class Native {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out RECT rect);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window,IntPtr insert,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetOwner64(IntPtr window,int index,IntPtr owner);
    [DllImport("user32.dll",EntryPoint="SetWindowLongW")]static extern int SetOwner32(IntPtr window,int index,IntPtr owner);
    public static void SetOwner(IntPtr window,IntPtr owner){if(IntPtr.Size==8)SetOwner64(window,-8,owner);else SetOwner32(window,-8,owner);}
    public delegate void WinEventProc(IntPtr hook,uint kind,IntPtr window,int objectId,int childId,uint thread,uint time);
    [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint minimum,uint maximum,IntPtr module,WinEventProc callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
    public struct POINT{public int X,Y;}
    public struct RECT{public int Left,Top,Right,Bottom;public int Width{get{return Right-Left;}}public int Height{get{return Bottom-Top;}}public Rectangle Box{get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}}}
    public static double ScaleFor(IntPtr h){try{uint dpi=GetDpiForWindow(h);return dpi>0?dpi/96.0:1;}catch{return 1;}}
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr window,uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int GetWindowStyle(IntPtr window,int index);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr window,System.Text.StringBuilder name,int capacity);
    static bool IsHostSurface(IntPtr window){
      if(window==IntPtr.Zero||(GetWindowStyle(window,-16)&0x40000000)!=0||(GetWindowStyle(window,-20)&0x80)!=0)return false;
      var name=new System.Text.StringBuilder(256);if(GetClassName(window,name,name.Capacity)==0)return false;
      return name.ToString()!="#32770"&&name.ToString()!="#32768";
    }
    // A dialog belongs to the host; its own bounds must never become the anchor.
    public static IntPtr CodexHost(IntPtr foreground){
      IntPtr result=IntPtr.Zero,current=GetAncestor(foreground,2);
      for(int depth=0;current!=IntPtr.Zero&&depth<32;depth++){
        if(IsWindowVisible(current)&&!IsIconic(current)&&IsCodex(current))result=current;
        IntPtr next=GetWindow(current,4);if(next==current)break;current=next;
      }
      return result;
    }
    public static bool IsCodex(IntPtr h){if(!IsHostSurface(h))return false;try{uint pid;GetWindowThreadProcessId(h,out pid);using(var p=Process.GetProcessById((int)pid)){if(p.ProcessName.Equals("Codex",StringComparison.OrdinalIgnoreCase))return true;if(!p.ProcessName.Equals("ChatGPT",StringComparison.OrdinalIgnoreCase))return false;string path=p.MainModule.FileName;return path.IndexOf("OpenAI.Codex_",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("\\codex-timer\\runtime\\",StringComparison.OrdinalIgnoreCase)>=0;}}catch{return false;}}
    public static bool IsOurWindow(IntPtr h){uint pid;GetWindowThreadProcessId(h,out pid);return pid==(uint)Process.GetCurrentProcess().Id;}
  }
  sealed class HostTracker:IDisposable {
    readonly Native.WinEventProc callback;readonly Dispatcher dispatcher;readonly Action changed;IntPtr foreground,minimize;int pending;bool disposed;
    public HostTracker(Action action){changed=action;dispatcher=Dispatcher.CurrentDispatcher;callback=OnEvent;foreground=Native.SetWinEventHook(3,3,IntPtr.Zero,callback,0,0,0);minimize=Native.SetWinEventHook(0x10,0x11,IntPtr.Zero,callback,0,0,0);}
    void OnEvent(IntPtr hook,uint kind,IntPtr window,int obj,int child,uint thread,uint time){if(disposed||Interlocked.Exchange(ref pending,1)!=0)return;dispatcher.BeginInvoke(new Action(()=>{Interlocked.Exchange(ref pending,0);if(!disposed)changed();}));}
    public void Dispose(){disposed=true;if(foreground!=IntPtr.Zero)Native.UnhookWinEvent(foreground);if(minimize!=IntPtr.Zero)Native.UnhookWinEvent(minimize);GC.KeepAlive(callback);}
  }
  // The tray menu uses the same live palette as the floating WPF surfaces.
  sealed class DarkMenuColors:Forms.ProfessionalColorTable {
    internal static Color FromBrush(System.Windows.Media.Brush brush){var c=((System.Windows.Media.SolidColorBrush)brush).Color;return Color.FromArgb(c.R,c.G,c.B);}
    static Color Surface{get{return FromBrush(Theme.Background);}}static Color Hover{get{return FromBrush(Theme.Hover);}}static Color Edge{get{return FromBrush(Theme.Line);}}
    public override Color ToolStripDropDownBackground{get{return Surface;}}
    public override Color MenuItemSelected{get{return Hover;}}
    public override Color MenuItemSelectedGradientBegin{get{return Hover;}}
    public override Color MenuItemSelectedGradientEnd{get{return Hover;}}
    public override Color MenuItemBorder{get{return Hover;}}
    public override Color MenuBorder{get{return Edge;}}
    public override Color ImageMarginGradientBegin{get{return Surface;}}
    public override Color ImageMarginGradientMiddle{get{return Surface;}}
    public override Color ImageMarginGradientEnd{get{return Surface;}}
    public override Color SeparatorDark{get{return Edge;}}
    public override Color SeparatorLight{get{return Surface;}}
    public override Color CheckBackground{get{return Hover;}}
    public override Color CheckSelectedBackground{get{return Hover;}}
    public override Color CheckPressedBackground{get{return Hover;}}
    public override Color ButtonSelectedHighlight{get{return Hover;}}
  }
  sealed class DarkMenuRenderer:Forms.ToolStripProfessionalRenderer {
    public DarkMenuRenderer():base(new DarkMenuColors()){RoundedEdges=false;}
    protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e){e.TextColor=DarkMenuColors.FromBrush(e.Item.Enabled?Theme.Foreground:Theme.Muted);base.OnRenderItemText(e);}
    protected override void OnRenderItemCheck(Forms.ToolStripItemImageRenderEventArgs e){
      var r=e.ImageRectangle;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
      using(var pen=new Pen(DarkMenuColors.FromBrush(Theme.Foreground),1.6f))g.DrawLines(pen,new[]{new System.Drawing.Point(r.Left+3,r.Top+6),new System.Drawing.Point(r.Left+6,r.Top+9),new System.Drawing.Point(r.Left+11,r.Top+2)});
    }
  }
  sealed class MenuApplication:IDisposable {
    readonly TimerEngine engine;readonly TimerFeature timerFeature;readonly StatsStore stats;DateTime statsRetry;readonly FeatureRegistry registry;readonly Bubble bubble;readonly MenuWindow panel;readonly Forms.NotifyIcon tray;readonly Icon icon;
    readonly DispatcherTimer tick;readonly EventWaitHandle showEvent,exitEvent;readonly string stateFile,testDirectory;readonly bool smokeRun;readonly AppearanceSettings appearance;
    readonly NativeFollower follower;readonly HostTracker tracker;IntPtr target,observed,owner;bool alert,manual,pinned,disposed,smokeExpired;int smokeTicks;string paintKey,trayText;
    readonly ContextFeature contextFeature;readonly ContextTracker contextTracker;readonly ContextAlerts contextAlerts;ContextReminder contextReminder;
    public MenuApplication(EventWaitHandle show,EventWaitHandle exit,string testRoot,bool runSmoke,bool openInitially){
      showEvent=show;exitEvent=exit;testDirectory=testRoot;smokeRun=runSmoke;
      string data=testRoot??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex","timer-data");
      stateFile=Path.Combine(data,"state.json");engine=new TimerEngine(TimerEngine.Load(stateFile),()=>DateTime.UtcNow);timerFeature=new TimerFeature(engine,Save);stats=new StatsStore(Path.Combine(data,"stats.json"));
      string moduleRoot=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","modules"));if(!Directory.Exists(moduleRoot))moduleRoot=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","plugin","modules"));
      contextFeature=new ContextFeature();contextAlerts=new ContextAlerts(Path.Combine(data,"context-alerts.json"));
      appearance=new AppearanceSettings(Path.Combine(data,"appearance.json"));registry=FeatureRegistry.Load(moduleRoot,timerFeature,new StatsFeature(stats),contextFeature);bubble=new Bubble();panel=new MenuWindow(registry,()=>{alert=false;manual=false;pinned=false;panel.Topmost=false;Track();},ResetPosition,appearance);
      if(testRoot==null&&registry.Find("context")!=null)contextTracker=new ContextTracker(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex"));
      follower=new NativeFollower();tracker=new HostTracker(Track);
      panel.AnchorBounds=()=>bubble.IsVisible?(Native.RECT?)bubble.PixelBounds:null;
      bubble.Clicked=()=>{if(panel.IsVisible&&!panel.IsClosing)panel.ClosePanel();else OpenPanel(false,null);};
      bubble.DragStarted=()=>follower.Suspend(true);bubble.PositionChanged=()=>{if(panel.IsVisible)PlacePanel();};
      bubble.DragArea=()=>{Native.RECT host;if(target==IntPtr.Zero||!Native.GetWindowRect(target,out host))return null;var area=Forms.Screen.FromHandle(target).WorkingArea;return (Native.RECT?)new Native.RECT{Left=Math.Max(host.Left,area.Left),Top=Math.Max(host.Top,area.Top),Right=Math.Min(host.Right,area.Right),Bottom=Math.Min(host.Bottom,area.Bottom)};};
      bubble.Dragged=()=>{Native.RECT r;if(target!=IntPtr.Zero&&Native.GetWindowRect(target,out r)){var b=bubble.PixelBounds;double scale=Native.ScaleFor(bubble.Handle);engine.State.OffsetX=(int)((r.Right-b.Right)/scale);engine.State.OffsetY=(int)((r.Bottom-b.Bottom)/scale);Save();}};
      bubble.DragEnded=()=>{PublishLayout();follower.Suspend(false);};panel.PresentationRequested=PreparePanel;
      icon=MakeIcon();tray=new Forms.NotifyIcon{Icon=icon,Text="小菜单",Visible=true};tray.BalloonTipClicked+=(s,e)=>OpenPanel(true,"timer");
      tray.MouseClick+=(s,e)=>{if(e.Button==Forms.MouseButtons.Left){if(panel.IsVisible&&!panel.IsClosing)panel.ClosePanel();else OpenPanel(true,null);}};
      var menu=new Forms.ContextMenuStrip{ShowImageMargin=false,BackColor=DarkMenuColors.FromBrush(Theme.Background),ForeColor=DarkMenuColors.FromBrush(Theme.Foreground),Renderer=new DarkMenuRenderer(),Font=new Font("Microsoft YaHei",9f)};
      menu.Items.Add("打开小菜单",null,(s,e)=>OpenPanel(true,null));menu.Items.Add("恢复按钮位置",null,(s,e)=>ResetPosition());
      var startup=new Forms.ToolStripMenuItem("登录 Windows 时启动"){CheckOnClick=true,Checked=File.Exists(StartupPath())};
      startup.CheckedChanged+=(s,e)=>SetStartup(startup.Checked);menu.Items.Add(startup);menu.Items.Add(new Forms.ToolStripSeparator());menu.Items.Add("退出小菜单",null,(s,e)=>Application.Current.Shutdown());tray.ContextMenuStrip=menu;foreach(Forms.ToolStripItem item in menu.Items)item.Tag=item.Text;L.Changed+=UpdateTrayLanguage;UpdateTrayLanguage();Theme.Changed+=UpdateTrayTheme;
      tick=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(200)};tick.Tick+=(s,e)=>OnTick();tick.Start();
      if(smokeRun){Directory.CreateDirectory(testRoot);engine.State.Sound=false;engine.Start(2);timerFeature.Panel.SetDuration(2);OpenPanel(true,"timer");}else{Track();if(openInitially)OpenPanel(true,null);}
    }
    void UpdateTrayLanguage(){if(disposed)return;foreach(Forms.ToolStripItem item in tray.ContextMenuStrip.Items)item.Text=L.T((string)item.Tag);string text=L.T("小菜单")+" · "+timerFeature.Summary;tray.Text=text.Length>63?text.Substring(0,63):text;paintKey=null;}
    void UpdateTrayTheme(){if(disposed||tray.ContextMenuStrip==null)return;var menu=tray.ContextMenuStrip;menu.BackColor=DarkMenuColors.FromBrush(Theme.Background);menu.ForeColor=DarkMenuColors.FromBrush(Theme.Foreground);menu.Invalidate(true);}
    void ResetPosition(){engine.State.OffsetX=24;engine.State.OffsetY=28;Save();Track();}
    void Save(){TrySave();}
    bool TrySave(){try{engine.Save(stateFile);return true;}catch(Exception ex){if(!(ex is IOException||ex is UnauthorizedAccessException||ex is System.Security.SecurityException||ex is InvalidOperationException||ex is ArgumentException))throw;if(tray!=null)tray.ShowBalloonTip(4000,L.T("小菜单"),L.T("状态暂时无法保存，当前计时继续运行。"),Forms.ToolTipIcon.Warning);return false;}}
    void OnTick(){
      if(exitEvent.WaitOne(0)){Application.Current.Shutdown();return;}if(showEvent.WaitOne(0))OpenPanel(true,null);if(engine.State.Status!="finished"){alert=false;}panel.Topmost=alert||pinned;
      foreach(var feature in registry.Features)if(feature.Tick()){Save();smokeExpired=true;if(engine.State.Status=="finished"){alert=true;OpenPanel(true,feature.Id);tray.ShowBalloonTip(8000,L.Language=="en"?feature.Title+" reminder":feature.Title+"提醒",feature.Summary,Forms.ToolTipIcon.Info);}if(feature.PlaySound)appearance.Audio.Play(appearance.SoundFile,true);}
      if(engine.State.PendingStats.Count>0&&DateTime.UtcNow>=statsRetry){if(TrySave()&&stats.Collect(engine.State)){Save();statsRetry=DateTime.MinValue;}else statsRetry=DateTime.UtcNow.AddSeconds(10);}
      stats.UpdateLive(engine.CurrentSession());
      Track();string summary=timerFeature.Summary,status=engine.State.Status;
      if(contextTracker!=null){contextTracker.Follow(target);var usage=contextTracker.Current;contextFeature.Update(usage);
        if(contextReminder!=null&&contextReminder.ThreadId!=usage.ThreadId){contextReminder.Close();contextReminder=null;}
        if(bubble.IsVisible&&contextAlerts.Take(usage)){if(contextReminder!=null)contextReminder.Close();var reminder=new ContextReminder(usage,()=>OpenPanel(true,"context"));contextReminder=reminder;reminder.Closed+=(s,e)=>{if(contextReminder==reminder)contextReminder=null;};reminder.Place(bubble.PixelBounds,target);reminder.Show();}
        if(!bubble.IsVisible&&contextReminder!=null){contextReminder.Close();contextReminder=null;}
      }
      if(!bubble.Dragging){
        // Repaint only when a displayed value actually changed: the panel is measured
        // for every refresh, so an unchanged 200ms tick must not trigger layout.
        string key=status+"|"+(int)Math.Ceiling(engine.Remaining)+"|"+engine.State.Total+"|"+summary+"|"+panel.ActiveId+"|"+stats.Revision+"|"+DateTime.Today.Ticks+"|"+contextFeature.Summary+"|"+contextFeature.Usage.ThreadId;
        if(panel.IsVisible&&key!=paintKey){paintKey=key;panel.Refresh();}
        double progress=status=="running"||status=="paused"?(engine.State.Total>0?Math.Max(0,Math.Min(1,engine.Remaining/engine.State.Total)):0):1;
        bubble.SetStatus(summary,status,progress);
        bubble.SetContext(contextFeature.Usage);
      }
      // Writing NotifyIcon.Text posts a shell message; only write on change.
      string next=L.T("小菜单")+" · "+summary;if(next.Length>63)next=next.Substring(0,63);if(tray!=null&&trayText!=next){trayText=next;tray.Text=next;}
      if(smokeRun&&++smokeTicks==18){Theme.Snapshot(panel,Path.Combine(testDirectory,"panel.png"),1.5);File.WriteAllText(Path.Combine(testDirectory,"smoke.json"),"{\"expired\":"+smokeExpired.ToString().ToLowerInvariant()+",\"panelVisible\":"+panel.IsVisible.ToString().ToLowerInvariant()+",\"status\":\""+engine.State.Status+"\"}");Application.Current.Shutdown();}
    }
    void Track(){
      if(disposed)return;IntPtr fg=Native.GetForegroundWindow();
      if(testDirectory!=null&&!smokeRun){long h;string f=Path.Combine(testDirectory,"foreground.txt");try{if(File.Exists(f)&&Int64.TryParse(File.ReadAllText(f),out h))fg=new IntPtr(h);}catch(IOException){return;}}
      bool own=Native.IsOurWindow(fg);if(fg!=observed){observed=fg;if(!own){target=Native.CodexHost(fg);if(!smokeRun)manual=false;}}
      Native.RECT r;bool valid=target!=IntPtr.Zero&&Native.IsWindowVisible(target)&&!Native.IsIconic(target)&&Native.GetWindowRect(target,out r);
      if(!valid){follower.Update(null);bubble.Hide();if(!alert&&!manual)panel.HideImmediately();if(owner!=IntPtr.Zero){Native.SetOwner(bubble.Handle,IntPtr.Zero);Native.SetOwner(panel.Handle,IntPtr.Zero);owner=IntPtr.Zero;}return;}
      if(owner!=target){Native.SetOwner(bubble.Handle,target);Native.SetOwner(panel.Handle,target);owner=target;}
      if(bubble.Dragging)return;
      if(!bubble.IsVisible){Native.GetWindowRect(target,out r);var b=bubble.PixelBounds;var area=Forms.Screen.FromHandle(target).WorkingArea;double scale=Native.ScaleFor(bubble.Handle);bubble.MovePixels(Math.Max(area.Left,Math.Min(area.Right-b.Width,r.Right-(int)(engine.State.OffsetX*scale)-b.Width)),Math.Max(area.Top,Math.Min(area.Bottom-b.Height,r.Bottom-(int)(engine.State.OffsetY*scale)-b.Height)));bubble.Show();}
      PublishLayout();
    }
    void PublishLayout(){if(follower==null)return;double scale=Native.ScaleFor(bubble.Handle);follower.Update(target==IntPtr.Zero?null:new FollowLayout(target,bubble.Handle,panel.IsVisible?panel.Handle:IntPtr.Zero,(int)(engine.State.OffsetX*scale),(int)(engine.State.OffsetY*scale),(int)(8*scale),panel.Above?-1:1,(int)Math.Round(14*scale)));}
    void PlacePanel(){PlacePanel(bubble,panel,panel.Above?-1:1);}
    static Native.RECT ToRect(Rectangle r){Native.RECT n=new Native.RECT();n.Left=r.Left;n.Top=r.Top;n.Right=r.Right;n.Bottom=r.Bottom;return n;}
    int PanelGap(){return (int)(8*Native.ScaleFor(bubble.Handle));}
    void PreparePanel(){
      // Cancel old follow coordinates before a page changes its native rectangle.
      // Position and size are committed together before the next visible frame.
      follower.Suspend(true);
      try{var size=panel.MeasurePopup();double scale=Native.ScaleFor(panel.Handle);int width=(int)Math.Round(size.Width*scale),height=(int)Math.Round(size.Height*scale);var b=bubble.PixelBounds;Native.POINT cursor;Native.GetCursorPos(out cursor);
        var area=ToRect(Forms.Screen.FromPoint(bubble.IsVisible?new System.Drawing.Point(b.Left,b.Top):new System.Drawing.Point(cursor.X,cursor.Y)).WorkingArea);
        int x,y;
        if(bubble.IsVisible){int inset=(int)Math.Round(14*scale);x=PanelPlacement.DockX(area,b,width,inset);y=PanelPlacement.DockY(area,b,height,inset,panel.Above?-1:1);}
        else{x=Math.Max(area.Left,area.Right-width-24);y=Math.Max(area.Top,area.Bottom-height-24);}
        panel.SetBoundsPixels(x,y,width,height);PublishLayout();
      }finally{follower.Suspend(bubble.Dragging);}
    }
    internal static void PlacePanel(Bubble bubble,TimerWindow panel,int side=0){
      var b=bubble.PixelBounds;Native.POINT cursor;Native.GetCursorPos(out cursor);
      var area=ToRect(Forms.Screen.FromPoint(bubble.IsVisible?new System.Drawing.Point(b.Left,b.Top):new System.Drawing.Point(cursor.X,cursor.Y)).WorkingArea);
      var p=panel.PixelBounds;int gap=(int)(8*Native.ScaleFor(bubble.Handle));int x,y;
      if(bubble.IsVisible){int inset=(int)Math.Round(14*Native.ScaleFor(bubble.Handle));x=PanelPlacement.DockX(area,b,p.Width,inset);y=PanelPlacement.DockY(area,b,p.Height,inset,side==0?-1:side);}
      else{x=Math.Max(area.Left,area.Right-p.Width-24);y=Math.Max(area.Top,area.Bottom-p.Height-24);}
      panel.MovePixels(x,y);
    }
    // A panel opened from the tray, the shortcut or a Codex request cannot rely on
    // Activate() to come forward, so it is pinned above other windows until dismissed.
    void OpenPanel(bool force,string id){
      manual=force;pinned=force;Track();panel.Topmost=alert||pinned;
      if(!panel.IsVisible){var b=bubble.PixelBounds;var area=Forms.Screen.FromHandle(bubble.Handle).WorkingArea;double height=panel.ExpandedHeight()*Native.ScaleFor(panel.Handle);panel.Above=!bubble.IsVisible||b.Top-PanelGap()-height>=area.Top||area.Bottom-b.Bottom<b.Top-area.Top;}
      if(id!=null)panel.OpenFeature(id);paintKey=null;panel.Refresh();panel.Reveal();panel.Activate();PublishLayout();
    }
    static Icon MakeIcon(){
      int size=Forms.SystemInformation.SmallIconSize.Width;if(size<16)size=16;if(size>64)size=64;double k=size/32.0;
      using(var bitmap=new Bitmap(size,size)){using(var g=Graphics.FromImage(bitmap)){g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.Clear(Color.Transparent);var shade=Color.FromArgb(214,214,214);
        using(var pen=new Pen(shade,Math.Max(1f,(float)(2.5*k)))){float inset=(float)(3*k);g.DrawEllipse(pen,inset,inset,(float)(size-2*inset)-0.02f,(float)(size-2*inset)-0.02f);}
        using(var dots=new SolidBrush(shade)){foreach(int x in new[]{11,18})foreach(int y in new[]{11,18})g.FillEllipse(dots,(float)(x*k),(float)(y*k),(float)(3*k),(float)(3*k));}}
        IntPtr h=bitmap.GetHicon();try{return (Icon)Icon.FromHandle(h).Clone();}finally{Native.DestroyIcon(h);}}
    }
    static string StartupPath(){return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"CodexRingTimerLite.lnk");}
    void SetStartup(bool enabled){try{if(enabled){object shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")),link=null;try{link=shell.GetType().InvokeMember("CreateShortcut",System.Reflection.BindingFlags.InvokeMethod,null,shell,new object[]{StartupPath()});string canonical=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex","tools","codex-ring-timer","plugin","bin","CodexRingTimer.exe");string exe=File.Exists(canonical)?canonical:Forms.Application.ExecutablePath;link.GetType().InvokeMember("TargetPath",System.Reflection.BindingFlags.SetProperty,null,link,new object[]{exe});link.GetType().InvokeMember("Save",System.Reflection.BindingFlags.InvokeMethod,null,link,null);}finally{if(link!=null)Marshal.FinalReleaseComObject(link);Marshal.FinalReleaseComObject(shell);}}else if(File.Exists(StartupPath()))File.Delete(StartupPath());}catch{tray.ShowBalloonTip(4000,L.T("小菜单"),L.T("无法更改登录启动设置"),Forms.ToolTipIcon.Warning);}}
    public void Dispose(){if(disposed)return;disposed=true;L.Changed-=UpdateTrayLanguage;Theme.Changed-=UpdateTrayTheme;tick.Stop();if(contextTracker!=null)contextTracker.Dispose();if(contextReminder!=null)contextReminder.Close();tracker.Dispose();follower.Dispose();appearance.Dispose();Save();tray.Visible=false;tray.Dispose();icon.Dispose();panel.Disposing=true;panel.Close();bubble.Close();}
  }
  static class Program {
    [STAThread]static void Main(string[] args){
      Native.SetProcessDPIAware();
      string test=args.Length==2&&(args[0]=="--smoke-test"||args[0]=="--fixture-test")?Path.GetFullPath(args[1]):null;
      string suffix=test==null?"":(".test."+Process.GetCurrentProcess().Id);bool fresh;
      using(var mutex=new Mutex(true,"Local\\CodexRingTimerLite"+suffix,out fresh))using(var show=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexRingTimerLite.Show"+suffix))using(var exit=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexRingTimerLite.Exit"+suffix)){
        if(args.Length==1&&args[0]=="--exit"){exit.Set();return;}if(!fresh){show.Set();return;}
        try{var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};using(var theme=new CodexThemeSync(test==null?CodexThemeSync.ConfigPath():Path.Combine(test,"config.toml"),null)){Theme.ApplyGlobalResources(app);using(var context=new MenuApplication(show,exit,test,test!=null&&args[0]=="--smoke-test",Array.IndexOf(args,"--open")>=0)){app.Run();}}}
        catch(Exception ex){MessageBox.Show(L.T("小菜单启动失败：")+ex.Message,L.T("小菜单"));}finally{mutex.ReleaseMutex();}
      }
    }
  }
}
