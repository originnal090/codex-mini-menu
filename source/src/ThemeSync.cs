using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace RingTimer {
  static partial class Theme {
    sealed class ColorToken:INotifyPropertyChanged {
      public Color Color{get;private set;}public event PropertyChangedEventHandler PropertyChanged;
      public ColorToken(Color color){Color=color;}
      public void Set(Color color){if(Color==color)return;Color=color;if(PropertyChanged!=null)PropertyChanged(this,new PropertyChangedEventArgs("Color"));}
    }
    static Dictionary<string,ColorToken> tokens;
    static SolidColorBrush Live(string name,string hex){
      if(tokens==null)tokens=new Dictionary<string,ColorToken>();var token=new ColorToken((Color)ColorConverter.ConvertFromString(hex));tokens.Add(name,token);
      var brush=new SolidColorBrush();BindingOperations.SetBinding(brush,SolidColorBrush.ColorProperty,new Binding("Color"){Source=token,Mode=BindingMode.OneWay});return brush;
    }
    public static readonly Brush Hover=Live("Hover","#333333"),Pressed=Live("Pressed","#222222"),Primary=Live("Primary","#E4E4E4"),PrimaryText=Live("PrimaryText","#1B1B1B"),PrimaryHover=Live("PrimaryHover","#F5F5F5"),PrimaryPressed=Live("PrimaryPressed","#D2D2D2"),Selected=Live("Selected","#373737"),SelectedLine=Live("SelectedLine","#8A8A8A"),Focus=Live("Focus","#8AB4F8"),Error=Live("Error","#F39C92"),SwitchOff=Live("SwitchOff","#626266"),SwitchOn=Live("SwitchOn","#30A65A"),SwitchThumb=Live("SwitchThumb","#FFFFFF");
    public static event Action Changed;
    public static bool IsDark{get;private set;}
    static Color Mix(Color a,Color b,double amount){return Color.FromRgb((byte)Math.Round(a.R+(b.R-a.R)*amount),(byte)Math.Round(a.G+(b.G-a.G)*amount),(byte)Math.Round(a.B+(b.B-a.B)*amount));}
    static Color ReadColor(string value,string fallback){if(value!=null&&Regex.IsMatch(value,"^#[0-9a-fA-F]{6}$"))return (Color)ColorConverter.ConvertFromString(value);return (Color)ColorConverter.ConvertFromString(fallback);}
    static double Luminance(Color c){Func<byte,double> linear=v=>{double x=v/255.0;return x<=.04045?x/12.92:Math.Pow((x+.055)/1.055,2.4);};return .2126*linear(c.R)+.7152*linear(c.G)+.0722*linear(c.B);}
    static double Contrast(Color a,Color b){double x=Luminance(a),y=Luminance(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
    public static void Apply(bool dark,string surface,string ink,string accent){
      IsDark=dark;Color baseColor=ReadColor(surface,dark?"#181818":"#FFFFFF"),text=ReadColor(ink,dark?"#ECECEC":"#202020");
      if(Contrast(baseColor,text)<4.5)text=Luminance(baseColor)>.4?Colors.Black:Colors.White;
      Color card=Mix(baseColor,text,dark?.075:.025),field=Mix(baseColor,text,dark?.025:.055),focus=ReadColor(accent,dark?"#8AB4F8":"#0069CC");
      if(Contrast(card,focus)<3)focus=ReadColor(null,dark?"#8AB4F8":"#0069CC");
      var palette=new Dictionary<string,Color>{{"Background",card},{"Foreground",text},{"Muted",Mix(baseColor,text,dark?.65:.66)},{"Field",field},{"Line",Mix(baseColor,text,dark?.33:.28)},
        {"Rim",Mix(baseColor,text,.22)},{"RimHover",Mix(baseColor,text,.48)},{"Track",Mix(baseColor,text,.24)},{"Running",Mix(baseColor,text,.83)},
        {"Paused",ReadColor(null,dark?"#D4B575":"#805900")},{"Done",ReadColor(null,dark?"#99D5AB":"#16743B")},
        {"Hover",Mix(baseColor,text,dark?.13:.095)},{"Pressed",Mix(baseColor,text,dark?.18:.15)},
        {"Primary",text},{"PrimaryText",baseColor},{"PrimaryHover",Mix(text,baseColor,.1)},{"PrimaryPressed",Mix(text,baseColor,.22)},
        {"Selected",Mix(baseColor,text,dark?.16:.11)},{"SelectedLine",Mix(baseColor,text,.52)},{"Focus",focus},
        {"Error",ReadColor(null,dark?"#F39C92":"#B42318")},{"SwitchOff",Mix(baseColor,text,.34)},{"SwitchOn",ReadColor(null,dark?"#30A65A":"#218843")},{"SwitchThumb",Colors.White}};
      bool changed=false;foreach(var pair in palette){ColorToken token;if(tokens.TryGetValue(pair.Key,out token)){if(token.Color!=pair.Value)changed=true;token.Set(pair.Value);}}
      RefreshGlass();if(changed&&Changed!=null)Changed();
    }
  }

  // Observes Codex's persisted desktop appearance; never writes Codex settings.
  // File notifications are coalesced onto the UI thread. Updating shared color
  // bindings preserves handles, focus, animations, input text and timer state.
  sealed class CodexThemeSync:IDisposable {
    readonly string path;readonly Func<bool> systemLight;readonly Dispatcher dispatcher;readonly DispatcherTimer debounce,poll;FileSystemWatcher watcher;
    int queued,retries;bool disposed,initialized;long lastStamp=-1,lastLength=-1;bool lastSystem;
    public CodexThemeSync(string config,Func<bool> readSystemLight){
      path=config;systemLight=readSystemLight??SystemIsLight;dispatcher=Dispatcher.CurrentDispatcher;
      debounce=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(100)};debounce.Tick+=(s,e)=>{debounce.Stop();Refresh();};
      string directory=Path.GetDirectoryName(path);if(Directory.Exists(directory)){
        watcher=new FileSystemWatcher(directory,Path.GetFileName(path)){NotifyFilter=NotifyFilters.LastWrite|NotifyFilters.Size|NotifyFilters.FileName};
        watcher.Changed+=(s,e)=>Queue();watcher.Created+=(s,e)=>Queue();watcher.Deleted+=(s,e)=>Queue();watcher.Renamed+=(s,e)=>Queue();watcher.Error+=(s,e)=>Queue();watcher.EnableRaisingEvents=true;
      }
      SystemEvents.UserPreferenceChanged+=OnSystemChanged;
      poll=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(2)};poll.Tick+=(s,e)=>{
        try{var file=new FileInfo(path);long stamp=file.Exists?file.LastWriteTimeUtc.Ticks:0,length=file.Exists?file.Length:0;bool light=systemLight();if(stamp!=lastStamp||length!=lastLength||light!=lastSystem)Refresh();}catch(IOException){}catch(UnauthorizedAccessException){}
      };Refresh();poll.Start();
    }
    public static string ConfigPath(){string home=Environment.GetEnvironmentVariable("CODEX_HOME");if(String.IsNullOrWhiteSpace(home))home=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");return Path.Combine(home,"config.toml");}
    static bool SystemIsLight(){try{using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")){return key==null||Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))!=0;}}catch{return true;}}
    void OnSystemChanged(object sender,UserPreferenceChangedEventArgs e){Queue();}
    void Queue(){if(disposed||Interlocked.Exchange(ref queued,1)!=0)return;if(dispatcher.HasShutdownStarted)return;dispatcher.BeginInvoke(new Action(()=>{Interlocked.Exchange(ref queued,0);if(disposed)return;retries=0;debounce.Stop();debounce.Start();}));}
    static bool TryRead(string source,out Dictionary<string,string> values){
      values=new Dictionary<string,string>();string section="";bool multiline=false;
      foreach(string raw in source.Split('\n')){string line=raw.Trim();if(line.StartsWith("#")||line.Length==0)continue;
        // Ignore multiline strings outside the appearance tables, including fake
        // section names inside other settings such as custom instructions.
        int triples=Regex.Matches(line,"\"\"\"|'''").Count;if(multiline){if(triples%2!=0)multiline=false;continue;}if(triples>0){if(triples%2!=0)multiline=true;continue;}
        if(line.StartsWith("[")){if(line.StartsWith("[[")){section="";continue;}var table=Regex.Match(line,@"^\[\s*([^\]]+)\s*\]\s*(?:#.*)?$");if(!table.Success)return false;section=table.Groups[1].Value.Trim().Replace("\"","").Replace("'","");continue;}
        bool desktop=section=="desktop",colors=section=="desktop.appearanceDarkChromeTheme"||section=="desktop.appearanceLightChromeTheme";if(!desktop&&!colors)continue;
        var key=Regex.Match(line,"^(?:[\"']?)(appearanceTheme|surface|ink|accent)(?:[\"']?)\\s*=");if(!key.Success)continue;
        string rest=line.Substring(key.Length).Trim();var value=Regex.Match(rest,"^(?:\"([^\"\\r\\n]*)\"|'([^'\\r\\n]*)')\\s*(?:#.*)?$");if(!value.Success)return false;
        values[section+"."+key.Groups[1].Value]=value.Groups[1].Success?value.Groups[1].Value:value.Groups[2].Value;
      }return !multiline;
    }
    static string Get(Dictionary<string,string> values,string key){string value;return values.TryGetValue(key,out value)?value:null;}
    internal void Refresh(){
      if(disposed)return;try{
        var file=new FileInfo(path);string source="";if(file.Exists){if(file.Length>2097152)return;using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var reader=new StreamReader(stream))source=reader.ReadToEnd();}
        if(initialized&&String.IsNullOrWhiteSpace(source)&&retries<5){Retry();return;}
        Dictionary<string,string> values;if(!TryRead(source,out values)){Retry();return;}string mode=Get(values,"desktop.appearanceTheme")??"system";
        if(mode!="dark"&&mode!="light"&&mode!="system"){Retry();return;}bool light=systemLight(),dark=mode=="dark"||(mode=="system"&&!light);string prefix=dark?"desktop.appearanceDarkChromeTheme.":"desktop.appearanceLightChromeTheme.";
        Theme.Apply(dark,Get(values,prefix+"surface"),Get(values,prefix+"ink"),Get(values,prefix+"accent"));lastStamp=file.Exists?file.LastWriteTimeUtc.Ticks:0;lastLength=file.Exists?file.Length:0;lastSystem=light;retries=0;initialized=true;
      }catch(IOException){Retry();}catch(UnauthorizedAccessException){Retry();}
    }
    void Retry(){if(++retries<=5){debounce.Stop();debounce.Start();}}
    public void Dispose(){if(disposed)return;disposed=true;if(watcher!=null)watcher.Dispose();debounce.Stop();poll.Stop();SystemEvents.UserPreferenceChanged-=OnSystemChanged;}
  }
}
