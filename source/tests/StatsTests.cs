using System;
using System.IO;
using System.Linq;
using RingTimer;
class StatsTests {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static bool Near(double a,double b){return Math.Abs(a-b)<.01;}
 static int Main(string[] args){try{
  string dir=args[0];Directory.CreateDirectory(dir);string path=Path.Combine(dir,"stats-"+Guid.NewGuid()+".json");
  DateTime now=new DateTime(2026,11,1,15,59,30,DateTimeKind.Utc);var zone=TimeZoneInfo.CreateCustomTimeZone("UTC+8",TimeSpan.FromHours(8),"UTC+8","UTC+8");
  var timer=new TimerEngine(new TimerState(),()=>now);var stats=new StatsStore(path,zone);
  timer.Start(120);now=now.AddSeconds(20);timer.Pause();now=now.AddHours(1);timer.Resume();now=now.AddSeconds(101);Check(timer.Tick(),"completion produced after pause");
  Check(timer.State.PendingStats.Count==1,"completed round enters durable outbox");var completion=timer.State.PendingStats[0];Check(completion.Spans.Count==2,"pause produces separate active intervals");
  Check(stats.Collect(timer.State),"completion persists to statistics");Check(timer.State.PendingStats.Count==0,"successful persistence acknowledges outbox");
  Check(Near(stats.Day(new DateTime(2026,11,1)).Seconds,20)&&Near(stats.Day(new DateTime(2026,11,2)).Seconds,100),"local day splitting excludes paused time");
  timer.State.PendingStats.Add(completion);stats.Collect(timer.State);Check(stats.Records.Count==1,"replayed completion is idempotent");
  Check(Near(stats.Aggregate(new DateTime(2026,11,1),new DateTime(2026,11,3)).Seconds,120)&&stats.Aggregate(new DateTime(2026,11,1),new DateTime(2026,11,3)).Sessions==1,"multi-day aggregation counts a round once");
  var restored=new StatsStore(path,zone);Check(restored.Records.Count==1&&restored.Day(new DateTime(2026,11,2)).Sessions==1,"records survive reload");
  string oldJson=File.ReadAllText(path).Replace("\"Version\":1","\"Version\":1,\"DailyGoalMinutes\":120");File.WriteAllText(path,oldJson);Check(new StatsStore(path,zone).Records.Count==1,"previous statistics schema preserves history");
  now=new DateTime(2026,11,2,15,59,40,DateTimeKind.Utc);timer.Start(60);now=now.AddSeconds(60);timer.Tick();stats.Collect(timer.State);
  Check(Near(stats.Day(new DateTime(2026,11,2)).Seconds,120)&&Near(stats.Day(new DateTime(2026,11,3)).Seconds,40),"single active interval splits exactly at midnight");
  timer.Start(60);now=now.AddSeconds(10);timer.Reset();Check(timer.State.PendingStats.Count==0,"reset does not record an incomplete round");
  timer.Start(60);now=now.AddSeconds(10);timer.Start(30);now=now.AddSeconds(31);timer.Tick();stats.Collect(timer.State);Check(stats.Records.Count==3,"replacing a round records only the completed replacement");
  timer.Start(15);string state=Path.Combine(dir,"state-test.json");timer.Save(state);now=now.AddHours(4);var restarted=new TimerEngine(TimerEngine.Load(state),()=>now);restarted.Tick();stats.Collect(restarted.State);timer=restarted;Check(Near(stats.Records.Last().Slices.Sum(x=>x.Seconds),15),"overdue restart ends at deadline rather than detection time");
  var legacy=new TimerState{Status="running",Total=30,Remaining=30,Deadline=now.AddSeconds(-1).Ticks};var old=new TimerEngine(legacy,()=>now);old.Tick();stats.Collect(old.State);Check(stats.Records.Count==5,"legacy running timer upgrades to a completion");
  var finished=new TimerEngine(new TimerState{Status="finished",Remaining=0},()=>now);finished.Tick();Check(finished.State.PendingStats.Count==0,"legacy finished state does not invent history");
  timer.Start(1);now=now.AddSeconds(1);timer.Tick();string blocked=Path.Combine(dir,"blocked-"+Guid.NewGuid());Directory.CreateDirectory(blocked);var unavailable=new StatsStore(blocked,zone);Check(!unavailable.Collect(timer.State)&&timer.State.PendingStats.Count==1,"write failure retains pending completion");
  timer.Save(state);Check(TimerEngine.Load(state).PendingStats.Count==1,"pending completion survives restart");
  var liveTimer=new TimerEngine(new TimerState(),()=>now);string livePath=Path.Combine(dir,"live-"+Guid.NewGuid()+".json");var live=new StatsStore(livePath,zone);DateTime localDay=TimeZoneInfo.ConvertTimeFromUtc(now,zone).Date;
  liveTimer.Start(60);now=now.AddSeconds(10);live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(localDay).Seconds,10)&&live.Day(localDay).Sessions==1,"running elapsed time contributes before completion");
  Check(live.Records.Count==0&&!File.Exists(livePath),"live time does not write completed history");int revision=live.Revision;now=now.AddMilliseconds(100);live.UpdateLive(liveTimer.CurrentSession());Check(live.Revision==revision,"subsecond ticks do not rebuild statistics");
  liveTimer.Pause();live.UpdateLive(liveTimer.CurrentSession());double paused=live.Day(localDay).Seconds;now=now.AddHours(1);live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(localDay).Seconds,paused),"paused totals do not grow");
  liveTimer.Resume();now=now.AddSeconds(20);live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(localDay).Seconds,30.1),"resume excludes paused interval");
  liveTimer.Save(state);liveTimer=new TimerEngine(TimerEngine.Load(state),()=>now);live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(localDay).Seconds,30.1),"live elapsed time survives timer reload");
  now=now.AddSeconds(40);live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(localDay).Seconds,60),"live snapshot stops at deadline");liveTimer.Tick();live.Collect(liveTimer.State);Check(Near(live.Day(localDay).Seconds,60)&&live.Day(localDay).Sessions==1,"completion replaces live time without double counting");live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(localDay).Seconds,60)&&live.Records.Count==1,"clearing live snapshot retains completed time");
  string saved=File.ReadAllText(livePath);liveTimer.Start(30);now=now.AddSeconds(5);live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(localDay).Seconds,65)&&File.ReadAllText(livePath)==saved,"new live round leaves durable history unchanged");liveTimer.Reset();live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(localDay).Seconds,60),"reset removes only incomplete live time");
  now=new DateTime(2026,11,5,15,59,50,DateTimeKind.Utc);liveTimer.Start(60);now=now.AddSeconds(30);live.UpdateLive(liveTimer.CurrentSession());Check(Near(live.Day(new DateTime(2026,11,5)).Seconds,10)&&Near(live.Day(new DateTime(2026,11,6)).Seconds,20),"live interval splits at local midnight");
  var edge=new TimerEngine(new TimerState(),()=>now);edge.Start(1);now=now.AddSeconds(2);edge.Reset();Check(edge.State.PendingStats.Count==1,"reset after elapsed deadline preserves completed round");edge.Start(1);now=now.AddSeconds(2);edge.Start(30);Check(edge.State.PendingStats.Count==2&&edge.State.Status=="running","replacement after elapsed deadline preserves completed round");
  string corrupt=Path.Combine(dir,"corrupt-"+Guid.NewGuid()+".json");File.WriteAllText(corrupt,"broken json");var recovered=new StatsStore(corrupt,zone);Check(recovered.Recovered&&recovered.Records.Count==0&&Directory.GetFiles(dir,Path.GetFileName(corrupt)+".corrupt-*").Length==1,"corrupt history is preserved before recovery");
  Console.WriteLine(count+" statistics data checks passed");return 0;
 }catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}}
}
