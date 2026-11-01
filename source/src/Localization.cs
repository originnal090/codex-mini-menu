using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace RingTimer {
  // One shared token per phrase. WPF bindings use weak subscriptions, so closed
  // pages are not retained by the language service.
  static class L {
    static readonly Dictionary<string,string> english=new Dictionary<string,string>{
      {"上下文","Context"},{"已用上下文","Context used"},{"等待当前对话数据","Waiting for current chat"},{"暂时无法确认用量","Usage unavailable"},
      {"80% 变橙 · 90% 变红","Orange at 80% · Red at 90%"},{"每个对话达到 90% 时提醒一次","One reminder per chat at 90%"},
      {"上下文即将用满，建议开启新对话。","Context is nearly full. Consider starting a new chat."},{"上下文已超过 80%，可以准备切换对话。","Context has reached 80%. Prepare to start a new chat."},
      {"跟随当前对话，按最近一次用量更新。","Follows the current chat using the latest usage sample."},{"正在识别当前对话；数据不明确时显示 —。","Identifying the current chat. Unavailable usage is shown as —."},
      {"上下文提醒","Context reminder"},{"查看上下文","View context"},{"知道了","Dismiss"},{"打开上下文","Open context"},
{"暂无完成记录","No completed rounds"},{"执行完成","Completed"},{"再次开始","Start again"},{"3 分钟","3 min"},{"10 分钟","10 min"},{"15 分钟","15 min"},
      {"● 工作","● Work"},{"● 休息","● Rest"},{"工作","Work"},{"休息","Rest"},{"未分类","Unclassified"},{"连续执行","Run"},{"组后等待","sets, then wait"},{"开始工作","Start work"},{"继续","Resume"},{"再来一组","Start again"},{"本组已完成","Cycle complete"},{"单次计时","Single timer"},{"工作时长","Work duration"},{"休息时长","Rest duration"},{"工作与休息循环","Work and rest cycle"},{"确定","Apply"},{"● 今日工作","● Work today"},{"● 今日休息","● Rest today"},{"工作 → 休息 → 等待","Work → Rest → Wait"},{"声音","Tone"},{"音量","Volume"},{"柔和钟声","Soft bell"},{"清脆双音","Double tone"},{"自定义音频","Custom audio"},{"工作或休息结束时播放","Plays after work or rest"},{"更多设置","More settings"},{"结束本次循环","End this cycle"},
      {"小菜单","Mini Menu"},{"计时器","Timer"},{"设置","Settings"},
      {"计时","Timer"},{"统计","Statistics"},{"打开统计","Open statistics"},{"今日","Today"},{"次","rounds"},
      {"日历","Calendar"},{"总量","Totals"},{"分段","Sessions"},{"已选择","Selected"},{"上一页","Previous page"},{"下一页","Next page"},
      {"日","Day"},{"周","Week"},{"月","Month"},{"少","Less"},{"多","More"},{"合计","Total"},
      {"查看当天分段","View sessions for this day"},{"暂无记录","No sessions yet"},
      {"统计暂未保存，将自动重试","Stats not saved; retrying"},{"已恢复统计，原文件已备份","Stats recovered; original backed up"},
      {"提示音设置","Reminder sound"},{"选择你的提醒声音","Choose your reminder sound"},
      {"支持 WAV、MP3，每次最多播放 30 秒。","WAV or MP3. Plays for up to 30 seconds."},
      {"当前提示音","Current sound"},{"选择音频文件","Choose audio file"},{"试听","Preview"},{"停止","Stop"},
      {"恢复系统默认","Use system default"},{"系统默认提示音","System default sound"},
      {"请保留所选文件。文件失效时，将使用系统提示音。","Keep the selected file in place. If unavailable, the system sound will play."},
      {"正在检查音频…","Checking audio…"},{"已保存，计时结束时播放此声音。","Saved. This sound will play when the timer ends."},
      {"无法读取此音频，请选择有效的 WAV 或 MP3。","Couldn't read this file. Choose a valid WAV or MP3."},
      {"播放失败，请检查音频文件或恢复系统默认。","Couldn't play audio. Check the file or use the system default."},
      {"外观","Appearance"},{"提示音","Sound"},{"界面语言","Interface language"},{"返回设置","Back to settings"},{"跟随 Codex","Follows Codex"},{"透明度","Transparency"},
      {"语言","Language"},{"切换后立即生效","Changes apply immediately"},
      {"让背景透过玻璃","Let the background shine through"},
      {"小菜单、计时器与悬浮按钮共用此设置。","Shared by the menu, timer and floating button."},
      {"玻璃透明度","Glass transparency"},{"玻璃透明度，百分比","Glass transparency, percent"},
      {"厚实","Solid"},{"通透","Clear"},{"恢复默认透明度","Reset transparency"},
      {"当前设置已生效，暂时无法保存。","Applied, but couldn't save your settings."},
      {"外观自动跟随 Codex，调整即时生效。","Appearance follows Codex. Changes apply immediately."},
      {"外观跟随 Codex。系统毛玻璃已关闭，当前使用透明材质。","Appearance follows Codex. System blur is off; using a transparent surface."},
      {"已安装","Installed"},{"尚未安装功能","No tools installed"},{"恢复位置","Reset position"},
      {"拖动圆环调整位置","Drag the ring to reposition"},
      {"打开计时器","Open timer"},{"返回","Back"},{"返回小菜单","Back to menu"},{"关闭小菜单","Close menu"},
      {"打开小菜单","Open menu"},{"小菜单悬浮按钮","Floating menu button"},
      {"打开小菜单 · 拖动换位置","Open menu · Drag to reposition"},
      {"自定义时长，到时提醒","Set a duration and get a reminder"},{"时间到了","Time's up"},
      {"正在计时","Counting down"},{"已暂停","Paused"},{"时间到了，休息一下吧","Time's up. Take a break."},
      {"选择时长","Choose duration"},{"上下方向键调整时长","Use the up and down arrow keys to change duration"},
      {"暂停","Pause"},{"继续计时","Resume"},{"再来一轮","Start again"},{"开始计时","Start timer"},
      {"剩余时间","Time remaining"},{"小时","Hours"},{"分钟","Minutes"},{"秒数","Seconds"},
      {"时","h"},{"分","m"},{"秒","s"},
      {"秒钟","Seconds"},{"5 分钟","5 min"},{"25 分钟","25 min"},{"45 分钟","45 min"},
      {"5 分","5 min"},{"15 分","15 min"},{"25 分","25 min"},{"45 分","45 min"},{"60 分","60 min"},
      {"重置计时器","Reset timer"},{"结束时播放提示音","Play sound when finished"},
      {"关闭面板后继续计时；退出或休眠期间，恢复后补报提醒。","Keeps counting when closed. Missed reminders appear when you return."},
      {"时长需在 1 秒到 24 小时之间","Choose a duration from 1 second to 24 hours"},
      {"恢复按钮位置","Reset button position"},{"登录 Windows 时启动","Start with Windows"},{"退出小菜单","Quit Mini Menu"},
      {"状态暂时无法保存，当前计时继续运行。","Couldn't save the timer. The countdown will continue."},
      {"无法更改登录启动设置","Couldn't change the startup setting"},{"小菜单启动失败：","Couldn't start Mini Menu: "},
      {"功能目录不存在，请重新安装。","Tools folder missing. Please reinstall."},
      {"有一个功能无法加载，请检查模块清单。","Couldn't load a tool. Check its module manifest."}
    };
    sealed class Token:INotifyPropertyChanged {
      readonly string key;public Token(string value){key=value;}
      public string Text{get{return T(key);}}
      public event PropertyChangedEventHandler PropertyChanged;
      public void Refresh(){if(PropertyChanged!=null)PropertyChanged(this,new PropertyChangedEventArgs("Text"));}
    }
    static readonly Dictionary<string,Token> tokens=new Dictionary<string,Token>();
    static string language="zh-CN";
    public static string Language{get{return language;}}
    public static event Action Changed;
    public static string Normalize(string value){return value=="en"?"en":"zh-CN";}
    public static string T(string key){string value;return language=="en"&&english.TryGetValue(key,out value)?value:key;}
    public static void SetLanguage(string value){value=Normalize(value);if(value==language)return;language=value;foreach(var token in tokens.Values)token.Refresh();if(Changed!=null)Changed();}
    public static void Bind(DependencyObject target,DependencyProperty property,string key){
      if(!english.ContainsKey(key)){foreach(var pair in english)if(pair.Value==key){key=pair.Key;break;}}
      if(!english.ContainsKey(key)){target.SetValue(property,key);return;}
      Token token;if(!tokens.TryGetValue(key,out token)){token=new Token(key);tokens.Add(key,token);}
      BindingOperations.SetBinding(target,property,new Binding("Text"){Source=token,Mode=BindingMode.OneWay});
    }
  }
}
