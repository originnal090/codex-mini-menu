using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace RingTimer {
  public sealed class FocusSpan {public long StartUtc,EndUtc;}
  public sealed class CompletedSession {public string Id,Kind="work";public bool Complete=true;public int Group;public List<FocusSpan> Spans=new List<FocusSpan>();}
  public class TimerState {
    public string Status = "idle";
    public int Total = 1500;
    public double Remaining = 1500;
    public long Deadline;
    public bool Sound = true;
    public bool Cycle;public string Phase="work";public int WorkSeconds=1500,RestSeconds=300,Groups=1,GroupIndex=1;
    public int OffsetX = 24, OffsetY = 28;
    public string SessionId;
    public long SegmentStarted;
    public List<FocusSpan> ActiveSpans=new List<FocusSpan>();
    public List<CompletedSession> PendingStats=new List<CompletedSession>();
  }
  public sealed class TimerEngine {
    public TimerState State;
    readonly Func<DateTime> now;
    public TimerEngine(TimerState state, Func<DateTime> clock) { now=clock; State=Valid(state)?state:new TimerState();State.WorkSeconds=Math.Max(1,Math.Min(86400,State.WorkSeconds));State.RestSeconds=Math.Max(1,Math.Min(86400,State.RestSeconds));State.Groups=Math.Max(1,Math.Min(99,State.Groups));State.GroupIndex=Math.Max(1,Math.Min(State.Groups,State.GroupIndex));if(!State.Cycle)State.Phase="work";else if(State.Phase!="work"&&State.Phase!="rest")State.Phase="work";if(State.ActiveSpans==null)State.ActiveSpans=new List<FocusSpan>();if(State.PendingStats==null)State.PendingStats=new List<CompletedSession>();if(State.Status=="running"&&String.IsNullOrEmpty(State.SessionId)){State.SessionId=Guid.NewGuid().ToString("N");State.SegmentStarted=Math.Max(1,State.Deadline-(long)(State.Remaining*TimeSpan.TicksPerSecond));} }
    public static bool Valid(TimerState s) {
      return s!=null && (s.Status=="idle"||s.Status=="running"||s.Status=="paused"||s.Status=="finished") &&
        s.Total>=1 && s.Total<=86400 && !Double.IsNaN(s.Remaining) && !Double.IsInfinity(s.Remaining) &&
        s.Remaining>=0 && s.Remaining<=s.Total && (s.Status!="running" || s.Deadline>0 && s.Deadline<=DateTime.MaxValue.Ticks);
    }
    public double Remaining { get { return State.Status=="running"?Math.Max(0,Math.Min(State.Total,(State.Deadline-now().Ticks)/(double)TimeSpan.TicksPerSecond)):State.Remaining; } }
    // A detached snapshot for display; completed history is still written only by Tick.
    public CompletedSession CurrentSession() {
      if((State.Status!="running"&&State.Status!="paused")||String.IsNullOrEmpty(State.SessionId))return null;
      var session=new CompletedSession{Id=State.SessionId,Kind=State.Phase,Group=State.GroupIndex,Complete=false};
      foreach(var span in State.ActiveSpans)if(span!=null)session.Spans.Add(new FocusSpan{StartUtc=span.StartUtc,EndUtc=span.EndUtc});
      long end=Math.Min(now().Ticks,State.Deadline);
      if(State.Status=="running"&&State.SegmentStarted>0&&end>State.SegmentStarted)session.Spans.Add(new FocusSpan{StartUtc=State.SegmentStarted,EndUtc=end});
      return session;
    }
    public void Start(int seconds) { if(seconds<1||seconds>86400)throw new ArgumentOutOfRangeException("seconds");Tick();State.Cycle=false;State.Phase="work";DateTime start=now();State.Total=seconds;State.Remaining=seconds;State.Deadline=start.AddSeconds(seconds).Ticks;State.Status="running";State.SessionId=Guid.NewGuid().ToString("N");State.SegmentStarted=start.Ticks;State.ActiveSpans.Clear(); }
    public void ConfigureCycle(int work,int rest,int groups){if(work<1||work>86400||rest<1||rest>86400||groups<1||groups>99)throw new ArgumentOutOfRangeException("cycle");if(State.Status=="running"||State.Status=="paused")throw new InvalidOperationException("A timer is active");State.WorkSeconds=work;State.RestSeconds=rest;State.Groups=groups;}
    void BeginStage(string kind,long start){State.Phase=kind;State.Total=kind=="work"?State.WorkSeconds:State.RestSeconds;State.Remaining=State.Total;State.Status="running";State.SegmentStarted=start;State.Deadline=new DateTime(start,DateTimeKind.Utc).AddSeconds(State.Total).Ticks;State.SessionId=Guid.NewGuid().ToString("N");State.ActiveSpans.Clear();}
    public void StartCycle(int work,int rest,int groups){if(work<1||work>86400||rest<1||rest>86400||groups<1||groups>99)throw new ArgumentOutOfRangeException("cycle");StopCycle();State.WorkSeconds=work;State.RestSeconds=rest;State.Groups=groups;State.GroupIndex=1;State.Cycle=true;BeginStage("work",now().Ticks);}
    void QueueStage(bool complete){if(State.ActiveSpans.Count>0)State.PendingStats.Add(new CompletedSession{Id=State.SessionId??Guid.NewGuid().ToString("N"),Kind=State.Phase,Group=State.GroupIndex,Complete=complete,Spans=new List<FocusSpan>(State.ActiveSpans)});State.ActiveSpans.Clear();State.SegmentStarted=0;}
    public void StopCycle(){Tick();if(State.Status=="running")EndSpan(Math.Min(now().Ticks,State.Deadline));if(State.Status=="running"||State.Status=="paused")QueueStage(false);State.Status="idle";State.Deadline=0;State.SessionId=null;State.SegmentStarted=0;State.ActiveSpans.Clear();State.Remaining=State.Total;}
    void EndSpan(long end){if(State.SegmentStarted>0&&end>State.SegmentStarted)State.ActiveSpans.Add(new FocusSpan{StartUtc=State.SegmentStarted,EndUtc=end});State.SegmentStarted=0;}
    public void Pause() { if(State.Cycle)Tick();if(State.Status!="running")return;State.Remaining=Remaining;if(State.Remaining>0){EndSpan(Math.Min(now().Ticks,State.Deadline));State.Status="paused";} }
    public void Resume() { if(State.Status!="paused")return;DateTime start=now();State.Deadline=start.AddSeconds(State.Remaining).Ticks;State.SegmentStarted=start.Ticks;if(String.IsNullOrEmpty(State.SessionId))State.SessionId=Guid.NewGuid().ToString("N");State.Status="running"; }
    public void Reset() { Tick();State.Status="idle";State.Remaining=State.Total;State.Deadline=0;State.SegmentStarted=0;State.ActiveSpans.Clear();State.SessionId=null; }
    public bool Tick() {bool changed=false;long current=now().Ticks;while(State.Status=="running"&&State.Deadline<=current){long end=State.Deadline;EndSpan(end);QueueStage(true);changed=true;if(State.Cycle&&State.Phase=="work")BeginStage("rest",end);else if(State.Cycle&&State.GroupIndex<State.Groups){State.GroupIndex++;BeginStage("work",end);}else{State.Status="finished";State.Remaining=0;State.Deadline=0;}}return changed; }
    public static TimerState Load(string path) { try {var serializer=new JavaScriptSerializer{MaxJsonLength=32*1024*1024};string json=File.ReadAllText(path);var state=serializer.Deserialize<TimerState>(json);var fields=serializer.Deserialize<Dictionary<string,object>>(json);if(Valid(state)&&fields!=null&&!fields.ContainsKey("WorkSeconds"))state.WorkSeconds=state.Total;return state;}catch{return new TimerState();} }
    public void Save(string path) {
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      string temp=path+".tmp";
      File.WriteAllText(temp,new JavaScriptSerializer{MaxJsonLength=32*1024*1024}.Serialize(State));
      if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
    }
    public static string Format(double seconds) { int n=(int)Math.Ceiling(Math.Max(0,seconds));return n>=3600?String.Format("{0}:{1:00}:{2:00}",n/3600,n/60%60,n%60):String.Format("{0:00}:{1:00}",n/60,n%60); }
  }
}
