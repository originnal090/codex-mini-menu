using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace RingTimer {
  // Windows ships SQLite. The connection is read-only; no third-party runtime is required.
  static class ContextThreadIndex {
    [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_open_v2(byte[] path,out IntPtr db,int flags,IntPtr vfs);
    [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_close(IntPtr db);
    [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_busy_timeout(IntPtr db,int ms);
    [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_prepare_v2(IntPtr db,byte[] sql,int count,out IntPtr statement,IntPtr tail);
    [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_step(IntPtr statement);
    [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr sqlite3_column_text(IntPtr statement,int column);
    [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_column_bytes(IntPtr statement,int column);
    [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_finalize(IntPtr statement);
    static byte[] Utf8(string text){return Encoding.UTF8.GetBytes(text+"\0");}
    static string Column(IntPtr statement,int index){IntPtr ptr=sqlite3_column_text(statement,index);if(ptr==IntPtr.Zero)return "";int size=sqlite3_column_bytes(statement,index);byte[] bytes=new byte[size];Marshal.Copy(ptr,bytes,0,size);return Encoding.UTF8.GetString(bytes);}
    public static List<ContextThread> Read(string file){
      var results=new List<ContextThread>();IntPtr db=IntPtr.Zero,statement=IntPtr.Zero;
      try{
        if(sqlite3_open_v2(Utf8(file),out db,1,IntPtr.Zero)!=0)return results;sqlite3_busy_timeout(db,150);
        byte[] sql=Utf8("SELECT id, COALESCE(NULLIF(name,''),title), rollout_path FROM threads");
        if(sqlite3_prepare_v2(db,sql,sql.Length,out statement,IntPtr.Zero)!=0){
          if(statement!=IntPtr.Zero){sqlite3_finalize(statement);statement=IntPtr.Zero;}
          sql=Utf8("SELECT id,title,rollout_path FROM threads");if(sqlite3_prepare_v2(db,sql,sql.Length,out statement,IntPtr.Zero)!=0)return results;
        }
        int status;while((status=sqlite3_step(statement))==100)results.Add(new ContextThread(Column(statement,0),Column(statement,1),Column(statement,2)));
        if(status!=101)results.Clear();
      }finally{if(statement!=IntPtr.Zero)sqlite3_finalize(statement);if(db!=IntPtr.Zero)sqlite3_close(db);}
      return results;
    }
  }
  sealed class ContextTracker:IDisposable {
    readonly string codexRoot;readonly ContextLogReader reader=new ContextLogReader();readonly AutoResetEvent wake=new AutoResetEvent(false);readonly Thread worker;
    readonly object gate=new object();IntPtr host;int generation;bool stopped;ContextUsage current=ContextUsage.Unknown(null,null);DateTime sampled=DateTime.MinValue;
    public ContextTracker(string root){codexRoot=root;worker=new Thread(Run){IsBackground=true,Name="Mini menu context reader"};worker.SetApartmentState(ApartmentState.MTA);worker.Start();}
    public void Follow(IntPtr window){lock(gate){if(host==window)return;host=window;generation++;current=ContextUsage.Unknown(null,null);sampled=DateTime.MinValue;}wake.Set();}
    public ContextUsage Current {get{lock(gate){return DateTime.UtcNow-sampled>TimeSpan.FromSeconds(4)?ContextUsage.Unknown(null,null):current;}}}
    // Only the top-level Codex document is inspected, never message contents or other apps.
    internal static string VisibleTitle(IntPtr window){
      if(window==IntPtr.Zero)return null;
      var root=AutomationElement.FromHandle(window);if(root==null)return null;
      var doc=root.FindFirst(TreeScope.Descendants,new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Document),new PropertyCondition(AutomationElement.AutomationIdProperty,"RootWebArea")));
      if(doc==null||doc.Current.IsOffscreen)return null;
      return doc.Current.Name;
    }
    void Run(){
      while(true){IntPtr window;int version;lock(gate){if(stopped)return;window=host;version=generation;}
        var usage=ContextUsage.Unknown(null,null);
        if(window!=IntPtr.Zero)try{
          string title=VisibleTitle(window);
          if(!String.IsNullOrWhiteSpace(title)){
            var thread=ContextSelection.Match(title,ContextThreadIndex.Read(Path.Combine(codexRoot,"state_5.sqlite")));
            if(thread!=null&&AllowedPath(thread.Path))usage=reader.Read(thread);else usage=ContextUsage.Unknown(null,title);
            // A navigation during disk reads must not publish the previous conversation's sample.
            if(title!=VisibleTitle(window))usage=ContextUsage.Unknown(null,null);
          }
        }catch(ElementNotAvailableException){}catch(InvalidOperationException){}catch(COMException){}catch(IOException){}catch(UnauthorizedAccessException){}catch(System.Security.SecurityException){}catch(ArgumentException){}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
        lock(gate){if(stopped)return;if(version==generation){current=usage;sampled=DateTime.UtcNow;}}
        wake.WaitOne(1000);
      }
    }
    bool AllowedPath(string path){if(String.IsNullOrEmpty(path))return false;string full=Path.GetFullPath(path);return full.StartsWith(Path.Combine(codexRoot,"sessions")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||full.StartsWith(Path.Combine(codexRoot,"archived_sessions")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}
    public void Dispose(){lock(gate){stopped=true;}wake.Set();if(worker.Join(100))wake.Dispose();}
  }
}
