using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;

namespace RingTimer {
  // Immutable layout shared with the native worker. All units here are physical pixels.
  sealed class FollowLayout {
    public readonly IntPtr Host,Launcher,Panel;
    public readonly int OffsetX,OffsetY,Gap,Side,DockInset;
    public FollowLayout(IntPtr host,IntPtr launcher,IntPtr panel,int x,int y,int gap,int side=0,int dockInset=0){Host=host;Launcher=launcher;Panel=panel;OffsetX=x;OffsetY=y;Gap=gap;Side=side;DockInset=dockInset;}
  }
  // One placement rule shared by the WPF thread and the native follower: the card's
  // right edge follows the launcher, it prefers to sit above the launcher and flips
  // below when there is not enough room above (instead of being clamped over it).
  static class PanelPlacement {
    public static int DockX(Native.RECT area,Native.RECT anchor,int width,int inset){return Math.Max(area.Left,Math.Min(Math.Max(area.Left,area.Right-width),anchor.Right+inset-width));}
    public static int DockY(Native.RECT area,Native.RECT anchor,int height,int inset,int side){return Math.Max(area.Top,Math.Min(Math.Max(area.Top,area.Bottom-height),side<0?anchor.Bottom+inset-height:anchor.Top-inset));}
    public static int X(Native.RECT area,Native.RECT anchor,int width){
      int max=area.Right-width;if(max<area.Left)max=area.Left;
      int x=anchor.Right-width;return x<area.Left?area.Left:(x>max?max:x);
    }
    public static int Y(Native.RECT area,Native.RECT anchor,int height,int gap,int side=0){
      if(side!=0){int wanted=side<0?anchor.Top-height-gap:anchor.Bottom+gap;return Math.Max(area.Top,Math.Min(Math.Max(area.Top,area.Bottom-height),wanted));}
      int above=anchor.Top-height-gap;if(above>=area.Top)return above;
      int below=anchor.Bottom+gap;if(below+height<=area.Bottom)return below;
      return Math.Max(area.Top,above);
    }
  }
  sealed class NativeFollower:IDisposable {
    const uint Sync=0x8001,Quit=0x12;
    internal const int MoveMessage=0x8078;
    static readonly object instancesGate=new object();static readonly Dictionary<int,NativeFollower> instances=new Dictionary<int,NativeFollower>();static int nextId;
    sealed class Position {public int X,Y,Generation;}
    readonly int id=Interlocked.Increment(ref nextId);int generation;
    readonly Dictionary<IntPtr,Position> positions=new Dictionary<IntPtr,Position>();
    [StructLayout(LayoutKind.Sequential)]struct Message {public IntPtr Window;public uint Id;public UIntPtr W;public IntPtr L;public uint Time;public Native.POINT Point;public uint Private;}
    [StructLayout(LayoutKind.Sequential)]struct MonitorInfo {public int Size;public Native.RECT Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")]static extern int GetMessage(out Message m,IntPtr w,uint a,uint b);
    [DllImport("user32.dll")]static extern bool PeekMessage(out Message m,IntPtr w,uint a,uint b,uint flags);
    [DllImport("user32.dll")]static extern bool PostThreadMessage(uint thread,uint message,UIntPtr w,IntPtr l);
    [DllImport("user32.dll")]static extern bool PostMessage(IntPtr window,int message,IntPtr w,IntPtr l);
    [DllImport("user32.dll")]static extern bool TranslateMessage(ref Message m);
    [DllImport("user32.dll")]static extern IntPtr DispatchMessage(ref Message m);
    [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")]static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll")]static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")]static extern bool IsWindow(IntPtr window);
    readonly object gate=new object();readonly Thread thread;readonly ManualResetEvent ready=new ManualResetEvent(false);
    readonly Native.WinEventProc callback;FollowLayout layout;uint threadId;int posted;bool paused,stopping;
    IntPtr locationHook,moveHook,hookTarget;public bool Running{get;private set;}public long Events,Moves;
    public NativeFollower(){callback=OnEvent;lock(instancesGate){instances.Add(id,this);}thread=new Thread(Pump){IsBackground=true,Name="Mini menu window follower"};thread.Start();ready.WaitOne(1000);}
    void Invalidate(){generation++;positions.Clear();}
    public void Update(FollowLayout next){lock(gate){if(stopping)return;if(layout==null||next==null||layout.Host!=next.Host||layout.Panel!=next.Panel||layout.Side!=next.Side)Invalidate();layout=next;}Queue();}
    public void Suspend(bool value){lock(gate){paused=value;Invalidate();}Queue();}
    void Queue(){if(threadId!=0&&Interlocked.Exchange(ref posted,1)==0)if(!PostThreadMessage(threadId,Sync,UIntPtr.Zero,IntPtr.Zero))Interlocked.Exchange(ref posted,0);}
    void OnEvent(IntPtr h,uint kind,IntPtr window,int obj,int child,uint source,uint time){lock(gate){if(stopping||layout==null||window!=layout.Host||obj!=0||child!=0)return;}Interlocked.Increment(ref Events);Queue();}
    void Hook(IntPtr target){
      if(target==hookTarget)return;Unhook(ref locationHook);Unhook(ref moveHook);hookTarget=target;if(target==IntPtr.Zero)return;
      uint pid;Native.GetWindowThreadProcessId(target,out pid);if(pid==0)return;
      locationHook=Native.SetWinEventHook(0x800B,0x800B,IntPtr.Zero,callback,pid,0,0);
      moveHook=Native.SetWinEventHook(0xA,0xB,IntPtr.Zero,callback,pid,0,0);
    }
    static void Unhook(ref IntPtr hook){if(hook!=IntPtr.Zero){Native.UnhookWinEvent(hook);hook=IntPtr.Zero;}}
    static int Clamp(int n,int low,int high){return Math.Max(low,Math.Min(Math.Max(low,high),n));}
    // The worker owns target coordinates. A tiny HWND message applies only the
    // newest generation, without waiting for a WPF rendering callback. Unlike
    // queued SWP_ASYNCWINDOWPOS requests, these can be cancelled at drag handoff.
    void Move(IntPtr h,int x,int y,Native.RECT current){
      Position old;bool queued=positions.TryGetValue(h,out old)&&old.Generation==generation;
      if(current.Left==x&&current.Top==y&&!queued)return;
      positions[h]=new Position{X=x,Y=y,Generation=generation};
      if(!queued&&!PostMessage(h,MoveMessage,new IntPtr(id),new IntPtr(generation)))positions.Remove(h);
    }
    internal static void Deliver(IntPtr window,IntPtr sender,IntPtr epoch){NativeFollower f;lock(instancesGate){if(!instances.TryGetValue(sender.ToInt32(),out f))return;}f.Apply(window,epoch.ToInt32());}
    void Apply(IntPtr window,int epoch){lock(gate){
      Position p;if(stopping||paused||epoch!=generation||layout==null||!positions.TryGetValue(window,out p)||p.Generation!=epoch)return;positions.Remove(window);
      if(!IsWindow(layout.Host)||Native.IsIconic(layout.Host)||!Native.IsWindowVisible(layout.Host))return;
      Native.RECT current;if(Native.GetWindowRect(window,out current)&&(current.Left!=p.X||current.Top!=p.Y)&&Native.SetWindowPos(window,IntPtr.Zero,p.X,p.Y,0,0,0x0015))Interlocked.Increment(ref Moves);
    }}
    void SyncLayout(){lock(gate){
      var l=layout;Hook(l==null?IntPtr.Zero:l.Host);if(stopping||paused||l==null||!IsWindow(l.Host)||Native.IsIconic(l.Host)||!Native.IsWindowVisible(l.Host))return;
      Native.RECT host,b;if(!Native.GetWindowRect(l.Host,out host)||!Native.GetWindowRect(l.Launcher,out b))return;
      var monitor=new MonitorInfo{Size=Marshal.SizeOf(typeof(MonitorInfo))};if(!GetMonitorInfo(MonitorFromWindow(l.Host,2),ref monitor))return;
      var area=monitor.Work;int left=Math.Max(host.Left,area.Left),right=Math.Min(host.Right,area.Right),top=Math.Max(host.Top,area.Top),bottom=Math.Min(host.Bottom,area.Bottom);
      if(right-left<b.Width||bottom-top<b.Height)return;
      int x=Clamp(host.Right-l.OffsetX-b.Width,left,right-b.Width),y=Clamp(host.Bottom-l.OffsetY-b.Height,top,bottom-b.Height);
      Move(l.Launcher,x,y,b);
      Native.RECT p;if(l.Panel!=IntPtr.Zero&&Native.GetWindowRect(l.Panel,out p)){var anchor=new Native.RECT{Left=x,Top=y,Right=x+b.Width,Bottom=y+b.Height};int px=l.DockInset>0?PanelPlacement.DockX(monitor.Work,anchor,p.Width,l.DockInset):PanelPlacement.X(monitor.Work,anchor,p.Width),py=l.DockInset>0?PanelPlacement.DockY(monitor.Work,anchor,p.Height,l.DockInset,l.Side):PanelPlacement.Y(monitor.Work,anchor,p.Height,l.Gap,l.Side);Move(l.Panel,px,py,p);}
    }}
    void Pump(){IntPtr previous=IntPtr.Zero;try{
      try{previous=SetThreadDpiAwarenessContext(new IntPtr(-4));}catch(EntryPointNotFoundException){}
      threadId=GetCurrentThreadId();Message m;PeekMessage(out m,IntPtr.Zero,0,0,0);Running=true;ready.Set();
      while(GetMessage(out m,IntPtr.Zero,0,0)>0){if(m.Id==Sync){Interlocked.Exchange(ref posted,0);SyncLayout();}else{TranslateMessage(ref m);DispatchMessage(ref m);}}
    }finally{Running=false;Unhook(ref locationHook);Unhook(ref moveHook);if(previous!=IntPtr.Zero)SetThreadDpiAwarenessContext(previous);ready.Set();GC.KeepAlive(callback);}}
    public void Dispose(){lock(gate){if(stopping)return;stopping=true;layout=null;Invalidate();}lock(instancesGate){instances.Remove(id);}if(threadId!=0)PostThreadMessage(threadId,Quit,UIntPtr.Zero,IntPtr.Zero);if(thread.Join(1500))ready.Dispose();}
  }
}
