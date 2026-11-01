using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RingTimer {
  sealed class ReminderAudio:IDisposable {
    MediaPlayer player,probe;DispatcherTimer limit,probeLimit;int playEpoch,probeEpoch;
    public event Action Changed;
    public bool IsPlaying{get;private set;}
    internal bool HasStarted{get;private set;}
    public bool Failed{get;private set;}
    public bool Previewing{get;private set;}
    internal Action DefaultSound=()=>System.Media.SystemSounds.Exclamation.Play();
    static bool LocalAudio(string file){
      try{Uri uri;string ext=Path.GetExtension(file);return Uri.TryCreate(file,UriKind.Absolute,out uri)&&uri.IsFile&&File.Exists(file)&&(String.Equals(ext,".wav",StringComparison.OrdinalIgnoreCase)||String.Equals(ext,".mp3",StringComparison.OrdinalIgnoreCase));}
      catch(ArgumentException){return false;}catch(NotSupportedException){return false;}catch(System.Security.SecurityException){return false;}
    }
    static DispatcherTimer After(int seconds,Action action){var t=new DispatcherTimer{Interval=TimeSpan.FromSeconds(seconds)};t.Tick+=(s,e)=>{t.Stop();action();};t.Start();return t;}
    void Notify(){if(Changed!=null)Changed();}
    public void CancelValidation(){++probeEpoch;if(probeLimit!=null){probeLimit.Stop();probeLimit=null;}if(probe!=null){probe.Close();probe=null;}}
    public void Validate(string file,Action<bool> completed){
      CancelValidation();if(!LocalAudio(file)){completed(false);return;}
      int epoch=probeEpoch;var candidate=new MediaPlayer{Volume=0};probe=candidate;
      Action<bool> finish=ok=>{if(epoch!=probeEpoch)return;CancelValidation();completed(ok);};
      candidate.MediaOpened+=(s,e)=>finish(candidate.HasAudio);candidate.MediaFailed+=(s,e)=>finish(false);
      probeLimit=After(8,()=>finish(false));try{candidate.Open(new Uri(Path.GetFullPath(file)));}catch(Exception e){if(!(e is IOException||e is ArgumentException||e is InvalidOperationException||e is NotSupportedException))throw;finish(false);}
    }
    public void Stop(){++playEpoch;if(limit!=null){limit.Stop();limit=null;}if(player!=null){player.Close();player=null;}IsPlaying=false;HasStarted=false;Failed=false;Previewing=false;Notify();}
    public void StopPreview(){if(Previewing)Stop();}
    public void Play(string file,bool fallback){
      Stop();Failed=false;Previewing=!fallback;
      if(String.IsNullOrEmpty(file)){DefaultSound();Previewing=false;Notify();return;}
      int epoch=playEpoch;
      Action failed=()=>{if(epoch!=playEpoch)return;Stop();Failed=true;if(fallback)DefaultSound();Notify();};
      if(!LocalAudio(file)){failed();return;}
      var current=new MediaPlayer{Volume=1};player=current;IsPlaying=true;Notify();
      current.MediaOpened+=(s,e)=>{if(epoch!=playEpoch)return;if(!current.HasAudio){failed();return;}if(limit!=null)limit.Stop();current.Play();HasStarted=true;limit=After(30,Stop);};
      current.MediaFailed+=(s,e)=>failed();current.MediaEnded+=(s,e)=>{if(epoch==playEpoch)Stop();};
      limit=After(8,failed);try{current.Open(new Uri(Path.GetFullPath(file)));}catch(Exception e){if(!(e is IOException||e is ArgumentException||e is InvalidOperationException||e is NotSupportedException))throw;failed();}
    }
    public void Dispose(){CancelValidation();Stop();}
  }
  sealed class SoundSettingsPanel:StackPanel {
    readonly AppearanceSettings settings;readonly TextBlock selected,status;readonly Button choose,preview,reset;bool busy;string message="";
    public SoundSettingsPanel(AppearanceSettings preferences):this(preferences,null,null){}
    public SoundSettingsPanel(AppearanceSettings preferences,TimerEngine timer,Action save){settings=preferences;
      if(timer!=null){var toggle=new CheckBox{IsChecked=timer.State.Sound,Foreground=Theme.Foreground,FontFamily=Theme.Font,FontSize=13,MinHeight=36,Margin=new Thickness(0,0,0,12),FocusVisualStyle=Theme.FocusRing};L.Bind(toggle,ContentControl.ContentProperty,"提示音");L.Bind(toggle,System.Windows.Automation.AutomationProperties.NameProperty,"结束时播放提示音");toggle.Checked+=(s,e)=>{timer.State.Sound=true;if(save!=null)save();};toggle.Unchecked+=(s,e)=>{timer.State.Sound=false;if(save!=null)save();};   toggle.Template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'><Grid Background='Transparent'><ContentPresenter VerticalAlignment='Center'/><Border x:Name='track' Width='36' Height='22' CornerRadius='11' Background='{DynamicResource Off}' HorizontalAlignment='Right'><Ellipse x:Name='thumb' Width='16' Height='16' Fill='{DynamicResource Ink}' HorizontalAlignment='Left' Margin='3'/></Border></Grid><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='track' Property='Background' Value='{DynamicResource On}'/><Setter TargetName='thumb' Property='HorizontalAlignment' Value='Right'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
   toggle.Resources["Off"]=Theme.SwitchOff;toggle.Resources["On"]=Theme.Done;toggle.Resources["Ink"]=Theme.Background;Children.Add(toggle);}
      var intro=Theme.Text("选择你的提醒声音",18,Theme.Foreground);intro.FontWeight=FontWeights.SemiBold;intro.Margin=new Thickness(0,8,0,8);Children.Add(intro);
      var description=Theme.Text("支持 WAV、MP3，每次最多播放 30 秒。",12,Theme.Muted);description.TextWrapping=TextWrapping.Wrap;description.Margin=new Thickness(0,0,0,24);Children.Add(description);
      var label=Theme.Text("当前提示音",12,Theme.Muted);label.Margin=new Thickness(0,0,0,8);Children.Add(label);
      selected=Theme.Text("",14,Theme.Foreground);selected.TextTrimming=TextTrimming.CharacterEllipsis;
      Children.Add(new Border{Padding=new Thickness(16),CornerRadius=new CornerRadius(18),Background=Theme.GlassControl,Child=selected,Margin=new Thickness(0,0,0,16)});
      choose=Theme.Button("选择音频文件",true);choose.Click+=(s,e)=>Choose();Children.Add(choose);
      var row=new Grid{Margin=new Thickness(0,8,0,0)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition());
      preview=Theme.Button("试听",false);preview.Margin=new Thickness(0,0,4,0);preview.Click+=(s,e)=>{message="";if(settings.Audio.IsPlaying)settings.Audio.Stop();else settings.Audio.Play(settings.SoundFile,false);Update();};row.Children.Add(preview);
      reset=Theme.Button("恢复系统默认",false);reset.Margin=new Thickness(4,0,0,0);Grid.SetColumn(reset,1);reset.Click+=(s,e)=>{settings.Audio.Stop();settings.SoundFile="";message="";Update();};row.Children.Add(reset);Children.Add(row);
      status=Theme.Text("",12,Theme.Muted);status.TextWrapping=TextWrapping.Wrap;status.Height=40;status.Margin=new Thickness(0,16,0,0);Children.Add(status);
      var hint=Theme.Text("请保留所选文件。文件失效时，将使用系统提示音。",12,Theme.Muted);hint.TextWrapping=TextWrapping.Wrap;hint.Margin=new Thickness(0,8,0,0);Children.Add(hint);
      Loaded+=(s,e)=>{settings.Changed+=Update;settings.Audio.Changed+=Update;L.Changed+=Update;Update();};
      IsVisibleChanged+=(s,e)=>{if(!IsVisible)settings.Audio.StopPreview();};
      Unloaded+=(s,e)=>{settings.Changed-=Update;settings.Audio.Changed-=Update;L.Changed-=Update;settings.Audio.CancelValidation();settings.Audio.StopPreview();busy=false;message="";};Update();
    }
    void Choose(){var dialog=new Microsoft.Win32.OpenFileDialog{Title=L.T("选择音频文件"),Filter="Audio (*.wav;*.mp3)|*.wav;*.mp3",CheckFileExists=true,Multiselect=false};if(dialog.ShowDialog(Window.GetWindow(this))==true)SelectFile(dialog.FileName);}
    internal void SelectFile(string file){settings.Audio.Stop();busy=true;message="正在检查音频…";Update();settings.Audio.Validate(file,ok=>{busy=false;message=ok?"已保存，计时结束时播放此声音。":"无法读取此音频，请选择有效的 WAV 或 MP3。";if(ok){settings.SoundFile=Path.GetFullPath(file);settings.Flush();if(settings.SaveFailed)message="当前设置已生效，暂时无法保存。";}Update();});}
    void Update(){if(status==null)return;
      if(String.IsNullOrEmpty(settings.SoundFile))L.Bind(selected,TextBlock.TextProperty,"系统默认提示音");else{selected.Text=Path.GetFileName(settings.SoundFile);}selected.ToolTip=String.IsNullOrEmpty(settings.SoundFile)?L.T("系统默认提示音"):settings.SoundFile;
      choose.IsEnabled=!busy;preview.IsEnabled=!busy;reset.IsEnabled=!busy&&!String.IsNullOrEmpty(settings.SoundFile);
      L.Bind(preview,ContentControl.ContentProperty,settings.Audio.IsPlaying?"停止":"试听");
      L.Bind(status,TextBlock.TextProperty,busy?message:settings.Audio.Failed?"播放失败，请检查音频文件或恢复系统默认。":message);status.Foreground=settings.Audio.Failed?Theme.Error:Theme.Muted;
    }
  }
}
