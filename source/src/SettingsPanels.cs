using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace RingTimer {
  sealed class SettingsOverview:StackPanel {
    readonly AppearanceSettings settings;readonly TextBlock language,appearance,sound;
    public SettingsOverview(AppearanceSettings preferences,Action openLanguage,Action openAppearance,Action openSound,Action restorePosition=null){
      settings=preferences;Margin=new Thickness(0,8,0,0);
      language=AddRow("语言","M 7,1 A 6,6 0 1 1 6.99,1 M 1,7 L 13,7 M 7,1 C 3,4 3,10 7,13 C 11,10 11,4 7,1",openLanguage);
      appearance=AddRow("外观","M 7,1 A 6,6 0 1 1 6.99,1 M 7,1 L 7,13 M 9,3 L 9,11 M 11,5 L 11,9",openAppearance);
      sound=AddRow("提示音","M 1,5 L 4,5 L 8,2 L 8,12 L 4,9 L 1,9 Z M 10,4 C 13,5 13,9 10,10",openSound);
      if(restorePosition!=null){var reset=Theme.Button("恢复位置",false);L.Bind(reset,FrameworkElement.ToolTipProperty,"将圆环移回默认位置");L.Bind(reset,AutomationProperties.NameProperty,"恢复位置");reset.Click+=(s,e)=>restorePosition();Children.Add(reset);}
      Loaded+=(s,e)=>{settings.Changed+=Update;L.Changed+=Update;Update();};Unloaded+=(s,e)=>{settings.Changed-=Update;L.Changed-=Update;};Update();
    }
    TextBlock AddRow(string title,string geometry,Action action){
      var button=Theme.Button("",false);button.Height=76;button.Padding=new Thickness(Theme.RowInset,12,Theme.RowInset,12);button.Margin=new Thickness(0,0,0,14);button.HorizontalContentAlignment=HorizontalAlignment.Stretch;L.Bind(button,AutomationProperties.NameProperty,title);
      var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(Theme.RowIconColumn)});row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(20)});
      row.Children.Add(Icon(geometry));var text=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,12,0)};Grid.SetColumn(text,1);row.Children.Add(text);
      text.Children.Add(Theme.Text(title,14,Theme.Foreground));var summary=Theme.Text("",12,Theme.Muted);summary.Margin=new Thickness(0,6,0,0);summary.TextTrimming=TextTrimming.CharacterEllipsis;text.Children.Add(summary);
      var arrow=Icon("M 4,2 L 9,7 L 4,12");arrow.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetColumn(arrow,2);row.Children.Add(arrow);button.Content=row;button.Click+=(s,e)=>action();Children.Add(button);return summary;
    }
    static FrameworkElement Icon(string geometry){return new System.Windows.Shapes.Path{Data=Geometry.Parse(geometry),Stroke=Theme.Muted,StrokeThickness=1.3,Width=12,Height=12,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round};}
    void Update(){language.Text=settings.Language=="en"?"English":"简体中文";appearance.Text=L.T(settings.AlwaysFloating?"始终悬浮":"跟随 Codex")+" · "+L.T("透明度")+" "+settings.Transparency+"%";sound.Text=String.IsNullOrEmpty(settings.SoundFile)?L.T("系统默认提示音"):Path.GetFileName(settings.SoundFile);sound.ToolTip=sound.Text;}
  }
  sealed class FloatingSettingsPanel:StackPanel {
    readonly AppearanceSettings settings;readonly GlassSoundSwitch toggle;
    public FloatingSettingsPanel(AppearanceSettings preferences){settings=preferences;
      toggle=new GlassSoundSwitch{IsChecked=settings.AlwaysFloating,Foreground=Theme.Foreground,FontFamily=Theme.Font,FontSize=13};L.Bind(toggle,ContentControl.ContentProperty,"始终悬浮");L.Bind(toggle,AutomationProperties.NameProperty,"始终悬浮");Children.Add(toggle);
      var hint=Theme.Text("切换应用、最小化或关闭 Codex 后仍显示。上下文仅在确认当前 Codex 聊天时显示。",12,Theme.Muted);hint.TextWrapping=TextWrapping.Wrap;hint.Margin=new Thickness(0,4,0,0);Children.Add(hint);
      Theme.GroupIsland(this,0,2,new Thickness(16,8,16,12));
      toggle.Checked+=(s,e)=>settings.AlwaysFloating=true;toggle.Unchecked+=(s,e)=>settings.AlwaysFloating=false;
      Loaded+=(s,e)=>{settings.Changed+=Update;Update();};Unloaded+=(s,e)=>settings.Changed-=Update;
    }
    void Update(){toggle.IsChecked=settings.AlwaysFloating;}
  }
  sealed class LanguageSettingsPanel:StackPanel {
    public readonly Button Chinese,English;readonly AppearanceSettings settings;
    public LanguageSettingsPanel(AppearanceSettings preferences){settings=preferences;
      var intro=Theme.Text("界面语言",18,Theme.Foreground);intro.FontWeight=FontWeights.SemiBold;intro.Margin=new Thickness(0,8,0,8);Children.Add(intro);
      var hint=Theme.Text("切换后立即生效",12,Theme.Muted);hint.Margin=new Thickness(0,0,0,0);Children.Add(hint);
      Theme.GroupIsland(this,0,2,new Thickness(16)).Margin=new Thickness(0,0,0,14);
      Chinese=Theme.Button("简体中文",false);English=Theme.Button("English",false);
      foreach(var button in new[]{Chinese,English}){button.Height=52;button.Margin=new Thickness(0,0,0,14);Children.Add(button);}
      Chinese.Click+=(s,e)=>settings.Language="zh-CN";English.Click+=(s,e)=>settings.Language="en";
      Loaded+=(s,e)=>{settings.Changed+=Update;Update();};Unloaded+=(s,e)=>settings.Changed-=Update;Update();
    }
    void Update(){Chinese.Background=settings.Language=="zh-CN"?Theme.Selected:Theme.IslandFill;English.Background=settings.Language=="en"?Theme.Selected:Theme.IslandFill;Chinese.BorderBrush=settings.Language=="zh-CN"?Theme.RimHover:Theme.IslandEdge;English.BorderBrush=settings.Language=="en"?Theme.RimHover:Theme.IslandEdge;}
  }
}
