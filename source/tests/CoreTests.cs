using System;
using System.IO;
using RingTimer;
class CoreTests {
  static int count;
  static void Check(bool b,string name){if(!b)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
  static void Main(){
    DateTime now=new DateTime(2026,10,29,10,0,0,DateTimeKind.Utc);
    var t=new TimerEngine(new TimerState(),()=>now);
    t.Start(60);now=now.AddSeconds(20);Check(t.Remaining==40,"absolute deadline");
    t.Pause();now=now.AddHours(2);Check(t.Remaining==40,"paused time remains frozen");
    t.Resume();now=now.AddSeconds(41);Check(t.Tick()&&t.State.Status=="finished","resume and expiry");Check(!t.Tick(),"expiry exactly once");
    t.Reset();Check(t.State.Status=="idle"&&t.Remaining==60,"reset duration");
    bool rejected=false;try{t.Start(0);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected&&t.State.Status=="idle","invalid duration does not change state");
    t.Start(86400);Check(t.Remaining==86400,"24 hour boundary");now=now.AddDays(2);Check(t.Tick(),"sleep resumes overdue timer");
    var bad=new TimerState();bad.Status="running";bad.Deadline=-1;Check(new TimerEngine(bad,()=>now).State.Status=="idle","corrupt state recovery");
    string path=Path.Combine(Path.GetTempPath(),"ring-timer-test-"+Guid.NewGuid(),"state.json");
    try{t.Start(15);t.State.Sound=false;t.Save(path);now=now.AddSeconds(16);var restored=new TimerEngine(TimerEngine.Load(path),()=>now);Check(restored.Tick()&&!restored.State.Sound,"restart catches overdue timer and restores preference");restored.Save(path);Check(!new TimerEngine(TimerEngine.Load(path),()=>now).Tick(),"saved completion does not notify again");}finally{if(File.Exists(path))File.Delete(path);Directory.Delete(Path.GetDirectoryName(path));}
    Check(TimerEngine.Format(3601)=="1:00:01"&&TimerEngine.Format(.1)=="00:01","readout rounding");
    t.Start(1);now=now.AddSeconds(2);t.Pause();Check(t.Tick(),"pause at deadline preserves completion notification");
    Console.WriteLine(count+" tests passed");
  }
}
