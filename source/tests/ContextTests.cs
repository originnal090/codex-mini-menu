using System;
using System.Reflection;
using System.IO;
using RingTimer;
class ContextTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static string Record(long used,long capacity){return "{\"type\":\"event_msg\",\"payload\":{\"type\":\"token_count\",\"info\":{\"last_token_usage\":{\"total_tokens\":"+used+"},\"total_token_usage\":{\"total_tokens\":100000},\"model_context_window\":"+capacity+"}}}";}
 static int Main(){
  try {
   var type=Assembly.GetExecutingAssembly().GetType("RingTimer.ContextUsage");
   if(type==null)throw new Exception("Context usage reader must exist to distinguish current usage from cumulative token consumption");
   var parse=type.GetMethod("Parse",BindingFlags.Public|BindingFlags.Static);
   var usage=parse.Invoke(null,new object[]{"chat-a","A","{\"type\":\"event_msg\",\"payload\":{\"type\":\"token_count\",\"info\":{\"last_token_usage\":{\"total_tokens\":900},\"total_token_usage\":{\"total_tokens\":100000},\"model_context_window\":1000}}}"});
   if((int)type.GetProperty("Percent").GetValue(usage,null)!=90)throw new Exception("Expected current context 90%, independent of lifetime usage");
   Check(true,"current context uses last usage, not cumulative consumption");
   Check(ContextUsage.Parse("a","A",Record(799,1000)).Level=="normal","79.9 percent remains below warning");
   Check(ContextUsage.Parse("a","A",Record(800,1000)).Level=="warning","80 percent turns orange");
   Check(ContextUsage.Parse("a","A",Record(899,1000)).Level=="warning","89.9 percent does not notify");
   Check(ContextUsage.Parse("a","A",Record(900,1000)).Level=="critical","90 percent turns red");
   Check(ContextUsage.Parse("a","A",Record(1200,1000)).Display=="100%","overflow display is bounded");
   Check(!ContextUsage.Parse("a","A",Record(0,0)).Known,"missing capacity is unknown");
   Check(!ContextUsage.Parse("a","A",Record(-1,1000)).Known,"negative usage is rejected");
   Check(ContextUsage.Parse("a","A",Record(0,1000)).Display=="0%","known zero remains distinct from unknown");
   Check(!ContextUsage.Parse("a","A","{}").Known,"absent data is unknown");
   var a=new ContextThread("a","A","a.jsonl");var b=new ContextThread("b","B","b.jsonl");
   Check(ContextSelection.Match("B",new[]{a,b}).Id=="b","visible chat determines selection, not record order");
   Check(ContextSelection.Match("A",new[]{a,new ContextThread("c","A","c.jsonl")})==null,"ambiguous identical titles fail closed");
   var longTitle=new ContextThread("long","This is a sufficiently long chat title\nwith details","x");
   Check(ContextSelection.Match("This is a sufficiently long…",new[]{longTitle}).Id=="long","unique truncated visible title resolves");
   Check(ContextSelection.Match("This is a sufficiently long…",new[]{longTitle,new ContextThread("other","This is a sufficiently long different title","y")})==null,"ambiguous truncated titles fail closed");
   Check(ContextSelection.Match("ChatGPT",new[]{a,b})==null,"home screen does not inherit last active conversation");
   string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"context-fixtures");Directory.CreateDirectory(dir);
   string log=Path.Combine(dir,"rollout.jsonl"),alerts=Path.Combine(dir,"alerts.json");if(File.Exists(alerts))File.Delete(alerts);
   a.Path=log;File.WriteAllText(log,Record(800,1000)+"\n"+Record(900,1000)+"\n");var reader=new ContextLogReader();
   Check(reader.Read(a).Percent==90,"latest usage event wins");
   File.AppendAllText(log,"{\"type\":\"event_msg\",\"payload\":{\"type\":\"token_count\",\"info\":null}}\n");
   Check(reader.Read(a).Percent==90,"rate-only update preserves latest usage");
   File.AppendAllText(log,"{\"type\":\"compacted\",\"payload\":{}}\n");
   Check(!reader.Read(a).Known,"compaction invalidates pre-compaction usage");
   File.AppendAllText(log,Record(150,1000)+"\n");Check(reader.Read(a).Percent==15,"usage updates after compaction");
   File.AppendAllText(log,Record(990,1000).Substring(0,50));Check(reader.Read(a).Percent==15,"partial trailing record does not corrupt last sample");
   File.WriteAllText(log,Record(200,1000)+"\n");Check(reader.Read(a).Percent==20,"truncated log is re-read");
   b.Path=Path.Combine(dir,"missing.jsonl");Check(!reader.Read(b).Known,"switch to missing chat clears old percentage");
   var notices=new ContextAlerts(alerts);var high=ContextUsage.Parse("a","A",Record(900,1000));
   Check(!notices.Take(ContextUsage.Parse("a","A",Record(899,1000))),"no reminder below 90 percent");
   Check(notices.Take(high),"first crossing produces reminder");Check(!notices.Take(high),"repeated updates do not repeat reminder");
   Check(notices.Take(ContextUsage.Parse("b","B",Record(950,1000))),"other conversation has independent reminder");
   Check(!notices.Take(high),"switching back does not repeat reminder");
   Check(!new ContextAlerts(alerts).Take(high),"restart preserves reminder history");
   Console.WriteLine(checks+" context checks passed");return 0;
  }catch(Exception e){Console.WriteLine("FAIL "+e.Message);return 1;}
 }
}
