using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace RingTimer {
  sealed class ContextFeature:IMenuFeature {
    ContextUsage usage=ContextUsage.Unknown(null,null);StackPanel view;TextBlock percent,title,detail,status;bool dark;
    public event Action ContentChanged;
    public string Id{get{return "context";}}public string Title{get{return L.T("上下文");}}
    public string Summary{get{return usage.Known?usage.Display+" · "+L.T("已用上下文"):L.T("等待当前对话数据");}}
    public string Icon{get{return "M 2,1 L 12,1 L 12,13 L 2,13 Z M 4,4 L 10,4 M 4,7 L 10,7 M 4,10 L 8,10";}}
    public bool PlaySound{get{return false;}}public bool Tick(){return false;}
    static readonly Brush redLight=new SolidColorBrush(Color.FromRgb(190,38,55)),redDark=new SolidColorBrush(Color.FromRgb(255,112,125)),orangeLight=new SolidColorBrush(Color.FromRgb(164,77,0)),orangeDark=new SolidColorBrush(Color.FromRgb(255,172,66));
    public static Brush Ink(ContextUsage value){return value.Level=="critical"?(Theme.IsDark?redDark:redLight):value.Level=="warning"?(Theme.IsDark?orangeDark:orangeLight):Theme.Foreground;}
    public ContextUsage Usage{get{return usage;}}
    public void Update(ContextUsage value){if(value==null)value=ContextUsage.Unknown(null,null);bool changed=dark!=Theme.IsDark||usage.ThreadId!=value.ThreadId||usage.Title!=value.Title||usage.Known!=value.Known||usage.Used!=value.Used||usage.Capacity!=value.Capacity;dark=Theme.IsDark;usage=value;if(changed){Refresh();if(ContentChanged!=null)ContentChanged();}}
    public FrameworkElement View{get{if(view==null){
      view=new StackPanel();var label=Theme.Text("已用上下文",12,Theme.Muted);label.TextAlignment=TextAlignment.Center;view.Children.Add(label);
      percent=Theme.Text("—",44,Theme.Foreground);percent.FontFamily=Theme.NumberFont;percent.TextAlignment=TextAlignment.Center;percent.Margin=new Thickness(0,8,0,10);view.Children.Add(percent);
      title=Theme.Text("",13,Theme.Foreground);title.TextWrapping=TextWrapping.Wrap;title.MaxHeight=60;title.TextTrimming=TextTrimming.CharacterEllipsis;view.Children.Add(title);
      detail=Theme.Text("",12,Theme.Muted);detail.Margin=new Thickness(0,12,0,0);view.Children.Add(detail);
      status=Theme.Text("",12,Theme.Muted);status.TextWrapping=TextWrapping.Wrap;status.Margin=new Thickness(0,12,0,0);view.Children.Add(status);
      var rule=Theme.Text("80% 变橙 · 90% 变红",12,Theme.Muted);view.Children.Add(rule);
      var hint=Theme.Text("每个对话达到 90% 时提醒一次",12,Theme.Muted);hint.Margin=new Thickness(0,6,0,0);hint.TextWrapping=TextWrapping.Wrap;view.Children.Add(hint);Theme.GroupIsland(view,0,2,new Thickness(16)).Margin=new Thickness(0,0,0,14);Theme.GroupIsland(view,1,3,new Thickness(16)).Margin=new Thickness(0,0,0,14);Theme.GroupIsland(view,2,2,new Thickness(16));Refresh();
    }return view;}}
    public void Refresh(){if(view==null)return;percent.Text=usage.Display;percent.Foreground=Ink(usage);AutomationProperties.SetName(percent,L.T("已用上下文")+" "+usage.Display);title.Text=String.IsNullOrEmpty(usage.Title)?L.T("等待当前对话数据"):ContextSelection.Normalize(usage.Title);title.ToolTip=usage.ThreadId;
      detail.Text=usage.Known?String.Format("{0:N0} / {1:N0} tokens",usage.Used,usage.Capacity):L.T("暂时无法确认用量");
      status.Text=L.T(usage.Level=="critical"?"上下文即将用满，建议开启新对话。":usage.Level=="warning"?"上下文已超过 80%，可以准备切换对话。":usage.Known?"跟随当前对话，按最近一次用量更新。":"正在识别当前对话；数据不明确时显示 —。");
    }
  }
  sealed class ContextReminder:TimerWindow {
    readonly System.Windows.Threading.DispatcherTimer expiry;
    public readonly string ThreadId;
    public ContextReminder(ContextUsage usage,Action open){
      ThreadId=usage.ThreadId;
      Width=328;SizeToContent=SizeToContent.Height;ShowActivated=false;Topmost=true;Title=L.T("上下文提醒");
      var frame=new Border{Background=Theme.Background,BorderBrush=ContextFeature.Ink(usage),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(34),Padding=new Thickness(16),Margin=new Thickness(8)};Content=frame;var stack=new StackPanel();frame.Child=stack;
      var heading=Theme.Text(L.T("上下文提醒")+" · "+usage.Display,16,ContextFeature.Ink(usage));heading.FontWeight=FontWeights.SemiBold;stack.Children.Add(heading);
      var message=Theme.Text("上下文即将用满，建议开启新对话。",13,Theme.Foreground);message.TextWrapping=TextWrapping.Wrap;message.Margin=new Thickness(0,10,0,8);stack.Children.Add(message);
      var chat=Theme.Text(ContextSelection.Normalize(usage.Title),11,Theme.Muted);chat.TextTrimming=TextTrimming.CharacterEllipsis;stack.Children.Add(chat);Theme.GroupIsland(stack,0,3,new Thickness(14));
      var buttons=new System.Windows.Controls.Primitives.UniformGrid{Columns=2,Margin=new Thickness(0,14,0,0)};stack.Children.Add(buttons);
      var details=Theme.Button("查看上下文",false);details.Height=40;details.FontSize=12;details.Margin=new Thickness(0,0,5,0);details.Click+=(s,e)=>{Close();open();};buttons.Children.Add(details);
      var close=Theme.Button("知道了",false);close.Height=40;close.FontSize=12;close.Margin=new Thickness(5,0,0,0);close.Click+=(s,e)=>Close();buttons.Children.Add(close);
      expiry=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(12)};expiry.Tick+=(s,e)=>Close();Closed+=(s,e)=>expiry.Stop();expiry.Start();
    }
    public void Place(Native.RECT anchor,IntPtr host){var area=System.Windows.Forms.Screen.FromHandle(host).WorkingArea;double scale=Native.ScaleFor(Handle);ContentAsElement().Measure(new Size(Width,Double.PositiveInfinity));int height=(int)Math.Ceiling(ContentAsElement().DesiredSize.Height*scale);int width=(int)Math.Round(Width*scale);SetBoundsPixels(Math.Max(area.Left,Math.Min(area.Right-width,anchor.Right)),Math.Max(area.Top,Math.Min(area.Bottom-height,anchor.Top-height-10)),width,height);Native.SetOwner(Handle,host);}
    FrameworkElement ContentAsElement(){return (FrameworkElement)Content;}
  }
}
