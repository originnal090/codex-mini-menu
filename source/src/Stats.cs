using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace RingTimer {
  public sealed class StatsSlice {
    public string Date;public string Kind="work";public bool Complete=true;public long StartUtc,EndUtc;public double StartMinute,EndMinute;
    public double Seconds{get{return (EndUtc-StartUtc)/(double)TimeSpan.TicksPerSecond;}}
  }
  public sealed class StatsRecord {public string Id;public string Kind="work";public bool Complete=true;public List<StatsSlice> Slices=new List<StatsSlice>();}
  public sealed class StatsDocument {public int Version=1;public List<StatsRecord> Records=new List<StatsRecord>();}
  public sealed class StatsBucket {public DateTime Start,End;public double Seconds,LongestSeconds,WorkSeconds,RestSeconds;public int Sessions,WorkSessions,RestSessions;}
  public sealed class StatsStore {
    readonly string path;readonly TimeZoneInfo zone;StatsDocument data=new StatsDocument();bool dirty,blocked;
    StatsRecord live;string liveKey="";
    public bool SaveFailed{get;private set;}public bool Recovered{get;private set;}public int Revision{get;private set;}
    public event Action Changed;
    public IList<StatsRecord> Records{get{return data.Records.AsReadOnly();}}
    static JavaScriptSerializer Serializer(){return new JavaScriptSerializer{MaxJsonLength=32*1024*1024};}
    public StatsStore(string file):this(file,TimeZoneInfo.Local){}
    public StatsStore(string file,TimeZoneInfo timeZone){path=file;zone=timeZone;Load();}
    void Notify(){Revision++;if(Changed!=null)Changed();}
    static bool Valid(StatsDocument d){return d!=null&&d.Version==1&&d.Records!=null&&d.Records.All(r=>r!=null&&!String.IsNullOrEmpty(r.Id)&&r.Slices!=null&&r.Slices.All(s=>s!=null&&ValidDate(s.Date)&&s.StartUtc>0&&s.EndUtc>s.StartUtc&&s.EndUtc<=DateTime.MaxValue.Ticks&&s.Seconds<=86400.01&&s.StartMinute>=0&&s.StartMinute<1440&&s.EndMinute>=0&&s.EndMinute<=1440));}
    public static string Kind(string value){return value=="work"||value=="rest"?value:"work";}
    void NormalizeKinds(){foreach(var r in data.Records){r.Kind=Kind(r.Kind);foreach(var slice in r.Slices){slice.Kind=r.Kind;slice.Complete=r.Complete;}}}
    static bool ValidDate(string value){DateTime date;return DateTime.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date);}
    void Load(){
      if(path==null||!File.Exists(path))return;
      try{var loaded=Serializer().Deserialize<StatsDocument>(File.ReadAllText(path));if(!Valid(loaded))throw new InvalidDataException();data=loaded;NormalizeKinds();}
      catch(Exception ex){if(!(ex is IOException||ex is UnauthorizedAccessException||ex is ArgumentException||ex is InvalidOperationException))throw;
        Recovered=true;try{File.Copy(path,path+".corrupt-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff",CultureInfo.InvariantCulture));if(File.Exists(path+".bak")){var backup=Serializer().Deserialize<StatsDocument>(File.ReadAllText(path+".bak"));if(Valid(backup)){data=backup;NormalizeKinds();}}}catch{blocked=true;SaveFailed=true;}
      }
    }
    bool Persist(){
      if(blocked){SaveFailed=true;return false;}
      try{if(path!=null){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temp=path+".tmp";File.WriteAllText(temp,Serializer().Serialize(data));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}dirty=false;SaveFailed=false;return true;}
      catch(Exception ex){if(!(ex is IOException||ex is UnauthorizedAccessException||ex is System.Security.SecurityException||ex is InvalidOperationException||ex is ArgumentException))throw;SaveFailed=true;return false;}
    }
    public void UpdateLive(CompletedSession session){
      live=session==null?null:Convert(session);
      string key=live==null?"":live.Id+":"+String.Join(";",live.Slices.Select(s=>s.Date+":"+s.StartUtc+":"+s.EndUtc/TimeSpan.TicksPerSecond));
      if(key==liveKey)return;liveKey=key;Notify();
    }
    IEnumerable<StatsRecord> DisplayRecords(){
      foreach(var record in data.Records)yield return record;
      // The persisted record wins during the completion handoff, including save retries.
      if(live!=null&&!data.Records.Any(r=>r.Id==live.Id))yield return live;
    }
    StatsRecord Convert(CompletedSession session){
      var record=new StatsRecord{Id=session.Id,Kind=Kind(session.Kind),Complete=session.Complete};if(session.Spans==null)return record;
      foreach(var span in session.Spans){if(span==null||span.StartUtc<=0||span.EndUtc<=span.StartUtc||span.EndUtc>DateTime.MaxValue.Ticks||span.EndUtc-span.StartUtc>TimeSpan.TicksPerDay)continue;
        DateTime start=new DateTime(span.StartUtc,DateTimeKind.Utc),end=new DateTime(span.EndUtc,DateTimeKind.Utc);
        while(start<end){DateTime local=TimeZoneInfo.ConvertTimeFromUtc(start,zone),midnight=DateTime.SpecifyKind(local.Date.AddDays(1),DateTimeKind.Unspecified);
          // Some zones move their clocks at midnight; advance to the first valid local minute.
          while(zone.IsInvalidTime(midnight))midnight=midnight.AddMinutes(1);
          DateTime boundary=TimeZoneInfo.ConvertTimeToUtc(midnight,zone),stop=end<boundary?end:boundary;if(stop<=start)break;
          DateTime localEnd=TimeZoneInfo.ConvertTimeFromUtc(stop,zone);
          record.Slices.Add(new StatsSlice{Kind=record.Kind,Complete=record.Complete,Date=local.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),StartUtc=start.Ticks,EndUtc=stop.Ticks,StartMinute=local.TimeOfDay.TotalMinutes,EndMinute=stop==boundary?1440:localEnd.TimeOfDay.TotalMinutes});start=stop;
        }
      }return record;
    }
    public bool Collect(TimerState state){
      if(state.PendingStats==null||state.PendingStats.Count==0)return !SaveFailed;bool modified=false;var known=new HashSet<string>(data.Records.Select(r=>r.Id));
      foreach(var pending in state.PendingStats){if(pending==null||String.IsNullOrEmpty(pending.Id)||known.Contains(pending.Id))continue;var record=Convert(pending);if(record.Slices.Count>0){data.Records.Add(record);known.Add(record.Id);modified=true;dirty=true;}}
      bool ok=!dirty||Persist();if(ok)state.PendingStats.Clear();if(modified||SaveFailed)Notify();return ok;
    }
    public StatsBucket Day(DateTime day){return Aggregate(day.Date,day.Date.AddDays(1));}
    public StatsBucket Aggregate(DateTime start,DateTime end){
      string first=start.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),last=end.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);var bucket=new StatsBucket{Start=start,End=end};
      foreach(var record in DisplayRecords()){bool included=false;foreach(var slice in record.Slices){if(String.CompareOrdinal(slice.Date,first)<0||String.CompareOrdinal(slice.Date,last)>=0)continue;included=true;bucket.Seconds+=slice.Seconds;if(Kind(record.Kind)=="work")bucket.WorkSeconds+=slice.Seconds;else if(Kind(record.Kind)=="rest")bucket.RestSeconds+=slice.Seconds;bucket.LongestSeconds=Math.Max(bucket.LongestSeconds,slice.Seconds);}if(included){bucket.Sessions++;if(Kind(record.Kind)=="rest")bucket.RestSessions++;else bucket.WorkSessions++;}}return bucket;
    }
    public List<StatsSlice> Slices(DateTime day){string key=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);return DisplayRecords().SelectMany(r=>r.Slices).Where(s=>s.Date==key).OrderBy(s=>s.StartUtc).ToList();}
  }
}
