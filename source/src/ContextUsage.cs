using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace RingTimer {
  sealed class ContextUsage {
    public string ThreadId, Title;
    public long Used, Capacity;
    public bool Known;
    public int Percent {get{return Known?(int)Math.Min(100,Math.Floor(100.0*Used/Capacity)):0;}}
    public string Level {get{return !Known?"unknown":Used>=Capacity*.9?"critical":Used>=Capacity*.8?"warning":"normal";}}
    public string Display {get{return Known?Percent+"%":"—";}}
    public static ContextUsage Unknown(string id,string title){return new ContextUsage{ThreadId=id,Title=title};}
    public static ContextUsage Parse(string id,string title,string json){
      var result=Unknown(id,title);
      try{
        var root=new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string,object>;
        var payload=Map(root,"payload");
        if(Text(root,"type")!="event_msg"||Text(payload,"type")!="token_count")return result;
        var info=Map(payload,"info");var usage=Map(info,"last_token_usage");
        long used,capacity;
        if(Number(usage,"total_tokens",out used)&&Number(info,"model_context_window",out capacity)&&used>=0&&capacity>0){result.Used=used;result.Capacity=capacity;result.Known=true;}
      }catch(ArgumentException){}catch(InvalidOperationException){}
      return result;
    }
    internal static Dictionary<string,object> Map(Dictionary<string,object> data,string key){object value;return data!=null&&data.TryGetValue(key,out value)?value as Dictionary<string,object>:null;}
    internal static string Text(Dictionary<string,object> data,string key){object value;return data!=null&&data.TryGetValue(key,out value)?value as string:null;}
    static bool Number(Dictionary<string,object> data,string key,out long value){value=0;object raw;return data!=null&&data.TryGetValue(key,out raw)&&raw!=null&&!(raw is string)&&Int64.TryParse(Convert.ToString(raw,System.Globalization.CultureInfo.InvariantCulture),out value);}
  }
  sealed class ContextThread {
    public string Id,Title,Path;
    public ContextThread(string id,string title,string path){Id=id;Title=title;Path=path;}
  }
  static class ContextSelection {
    public static string Normalize(string title){return Regex.Replace(title??"",@"\s+"," ").Trim();}
    public static ContextThread Match(string visibleTitle,IEnumerable<ContextThread> threads){
      string visible=Normalize(visibleTitle);if(visible.Length==0)return null;
      bool truncated=visible.EndsWith("…",StringComparison.Ordinal)||visible.EndsWith("...",StringComparison.Ordinal);
      string prefix=truncated?visible.Substring(0,visible.Length-(visible.EndsWith("…",StringComparison.Ordinal)?1:3)):visible;
      if(truncated&&prefix.Length<12)return null;
      ContextThread match=null;
      foreach(var item in threads){string title=Normalize(item.Title);if(title!=visible&&!(truncated&&title.StartsWith(prefix,StringComparison.Ordinal)))continue;
        if(match!=null&&match.Id!=item.Id)return null;match=item;
      }return match;
    }
  }
  sealed class ContextLogReader {
    string path;long length=-1;DateTime modified;ContextUsage cached;
    public ContextUsage Read(ContextThread thread){
      if(thread==null)return ContextUsage.Unknown(null,null);
      try{
        var file=new FileInfo(thread.Path);
        if(cached!=null&&path==thread.Path&&length==file.Length&&modified==file.LastWriteTimeUtc){cached.Title=thread.Title;return cached;}
        var result=ReadTail(thread);path=thread.Path;length=file.Length;modified=file.LastWriteTimeUtc;cached=result;return result;
      }catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}
      return ContextUsage.Unknown(thread.Id,thread.Title);
    }
    static ContextUsage ReadTail(ContextThread thread){
      using(var stream=new FileStream(thread.Path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){
        long end=stream.Length;
        for(int size=65536;size<=8388608;size*=4){
          long start=Math.Max(0,end-size);stream.Position=start;
          byte[] bytes=new byte[(int)(end-start)];int read=0,n;while(read<bytes.Length&&(n=stream.Read(bytes,read,bytes.Length-read))>0)read+=n;
          string text=Encoding.UTF8.GetString(bytes,0,read);string[] lines=text.Split('\n');
          // Only complete records are accepted. Writers can pause in the middle of a JSON line.
          for(int i=lines.Length-2;i>=(start==0?0:1);i--){string line=lines[i];
            if(line.IndexOf("token_count",StringComparison.Ordinal)<0&&line.IndexOf("compacted",StringComparison.Ordinal)<0)continue;
            try{
              var root=new JavaScriptSerializer().DeserializeObject(line) as Dictionary<string,object>;
              var payload=ContextUsage.Map(root,"payload");string kind=ContextUsage.Text(payload,"type");
              if(ContextUsage.Text(root,"type")=="compacted"||(ContextUsage.Text(root,"type")=="event_msg"&&kind=="context_compacted"))return ContextUsage.Unknown(thread.Id,thread.Title);
              if(ContextUsage.Text(root,"type")!="event_msg"||kind!="token_count")continue;
              // Rate-limit-only events contain null info and do not replace usage.
              if(ContextUsage.Map(payload,"info")==null)continue;
              return ContextUsage.Parse(thread.Id,thread.Title,line);
            }catch(ArgumentException){}catch(InvalidOperationException){}
          }
          if(start==0)break;
        }
      }return ContextUsage.Unknown(thread.Id,thread.Title);
    }
  }
  sealed class ContextAlerts {
    readonly HashSet<string> notified=new HashSet<string>(StringComparer.Ordinal);readonly string path;
    public ContextAlerts(string file){path=file;try{if(file!=null&&File.Exists(file)){var ids=new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(file));if(ids!=null)foreach(var id in ids)notified.Add(id);}}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}}
    public bool Take(ContextUsage usage){
      if(usage==null||!usage.Known||usage.Level!="critical"||String.IsNullOrEmpty(usage.ThreadId)||!notified.Add(usage.ThreadId))return false;
      if(path!=null)try{Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(new List<string>(notified)),Encoding.UTF8);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}catch(IOException){}catch(UnauthorizedAccessException){}
      return true;
    }
  }
}
