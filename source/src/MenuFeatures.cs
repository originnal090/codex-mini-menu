using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace RingTimer {
  interface IMenuFeature {
    string Id{get;}string Title{get;}string Summary{get;}string Icon{get;}
    FrameworkElement View{get;}void Refresh();bool Tick();bool PlaySound{get;}
    event Action ContentChanged;
  }
  sealed class TimerFeature:IMenuFeature {
    readonly TimerEngine engine;readonly Action save;TimerPanel view;
    public event Action ContentChanged;
    public TimerFeature(TimerEngine timer,Action changed){engine=timer;save=changed;}
    internal TimerEngine Engine{get{return engine;}} internal Action SaveAction{get{return save;}}
    public string Id{get{return "timer";}}public string Title{get{return L.T("计时器");}}
    public string Summary{get{return engine.State.Status=="idle"?L.T("工作与休息循环"):engine.State.Status=="finished"?L.T("时间到了"):TimerEngine.Format(engine.Remaining)+" · "+L.T(engine.State.Status=="paused"?"已暂停":"正在计时");}}
    public string Icon{get{return "M 7,1 A 6,6 0 1 1 6.99,1 M 7,3 L 7,7 L 10,9";}}
    public FrameworkElement View{get{return Panel;}}public TimerPanel Panel{get{if(view==null){view=new TimerPanel(engine,save);view.ContentChanged+=()=>{if(ContentChanged!=null)ContentChanged();};}return view;}}
    public void Refresh(){if(view!=null)view.PaintState();}
    public bool Tick(){return engine.Tick();}public bool PlaySound{get{return engine.State.Sound;}}
  }
  sealed class FeatureRegistry {
    readonly List<IMenuFeature> features=new List<IMenuFeature>();readonly List<string> issues=new List<string>();
    public IList<IMenuFeature> Features{get{return features.AsReadOnly();}}public IList<string> Issues{get{return issues.AsReadOnly();}}
    public FeatureRegistry(params IMenuFeature[] installed){foreach(var f in installed)features.Add(f);}
    public IMenuFeature Find(string id){return features.Find(f=>f.Id==id);}
    // The manifest selects a compiled feature provider. It never executes scripts or loads arbitrary DLLs.
    public static FeatureRegistry Load(string folder,params IMenuFeature[] providers){
      var result=new FeatureRegistry();if(!Directory.Exists(folder)){result.issues.Add("功能目录不存在，请重新安装。");return result;}
      foreach(string dir in Directory.GetDirectories(folder)){
        string manifest=System.IO.Path.Combine(dir,"module.json");if(!File.Exists(manifest))continue;
        try{if(new FileInfo(manifest).Length>16384)throw new InvalidDataException();var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(manifest));object id,enabled;
          if(data==null||!data.TryGetValue("id",out id)||!(id is string))throw new InvalidDataException();if(data.TryGetValue("enabled",out enabled)&&enabled is bool&&!(bool)enabled)continue;
          var feature=Array.Find(providers,p=>p.Id==(string)id);if(feature==null||result.Find(feature.Id)!=null)throw new InvalidDataException();result.features.Add(feature);
        }catch(Exception ex){if(!(ex is InvalidDataException||ex is IOException||ex is UnauthorizedAccessException||ex is ArgumentException||ex is InvalidOperationException))throw;result.issues.Add("有一个功能无法加载，请检查模块清单。");}
      }return result;
    }
  }
  sealed partial class MenuWindow:TimerWindow {
    readonly FeatureRegistry registry;readonly Action closed,restore;readonly TextBlock title;readonly Button back;readonly ContentControl body;readonly System.Windows.Controls.Primitives.UniformGrid featureNav;readonly List<Button> featureTabs=new List<Button>();readonly Style navActiveStyle=Theme.RoundedButtonStyle(true,15),navIdleStyle=Theme.RoundedButtonStyle(false,15);
    readonly Dictionary<string,TextBlock> summaries=new Dictionary<string,TextBlock>();StackPanel home;IMenuFeature active;readonly AppearanceSettings appearance;AppearancePanel settingsView;SoundSettingsPanel soundView;LanguageSettingsPanel languageView;SettingsOverview settingsOverview;string settingsPage;
    readonly Border surface,navIsland;readonly TranslateTransform reveal=new TranslateTransform(),pageShift=new TranslateTransform();
    readonly Button close;readonly StackPanel pageStack;
    Bubble launcher;
    internal Bubble Launcher{get{return launcher;}set{if(launcher!=null)launcher.SetDocked(false);launcher=value;if(launcher!=null)launcher.SetDocked(IsVisible);}}
    internal SilverCardPresenter CardPresenter{get;private set;}
    internal Border CardSurface{get{return surface;}}
    internal bool IsDurationEditing{get{var timer=active as TimerFeature;return timer!=null&&timer.Panel.IsEditing;}}
    internal bool CanTiltCard{get{return !IsClosing&&!resizing&&!morphing&&RevealProgress>.99&&!IsDurationEditing;}}
    internal void RestoreCardContour(){lastShape=Rect.Empty;UpdateRevealGeometry();}
    internal Func<double> WorkingHeight=null;readonly ScrollViewer pageScroll;
    public bool Disposing;public Action PresentationRequested;bool preparing,resizing;Size presentedSize,presentationSize;int sizeEpoch,visibilityEpoch;double navigationHeight=Double.NaN;
    public bool IsClosing{get;private set;}public bool Above{get;set;}bool animationsEnabled;
    public bool AnimationsEnabled{get{return animationsEnabled;}set{animationsEnabled=value;if(CardPresenter!=null)CardPresenter.RefreshAtmosphere();}}
    public string ActiveId{get{return settingsPage??(active==null?null:active.Id);}}
    public MenuWindow(FeatureRegistry features,Action onClose,Action restorePosition):this(features,onClose,restorePosition,new AppearanceSettings(null)){}
    public MenuWindow(FeatureRegistry features,Action onClose,Action restorePosition,AppearanceSettings preferences){
      appearance=preferences;registry=features;closed=onClose;restore=restorePosition;Title="小菜单";Width=Theme.MenuWidth;SizeToContent=SizeToContent.Manual;Above=true;AnimationsEnabled=true;
      surface=new Border{Background=Theme.GlassFill,BorderBrush=Brushes.Transparent,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(Theme.MenuRadius),Padding=new Thickness(Theme.MenuInset),Margin=new Thickness(8),VerticalAlignment=VerticalAlignment.Bottom,RenderTransform=reveal};CardPresenter=new SilverCardPresenter(this,surface);Content=CardPresenter;
      IsVisibleChanged+=(s,e)=>{if(Launcher!=null)Launcher.SetDocked(IsVisible);};Closed+=(s,e)=>{if(Launcher!=null)Launcher.SetDocked(false);};
      // The header hit targets extend 8 DIP into the card padding. Give their
      // parent that actual width so clipping keeps the entire 40 DIP buttons.
      // Body margins restore its original content width; vertical clipping still
      // contains the page during the continuous height transition.
      var layers=new Grid();surface.Child=layers;
      var stack=new StackPanel{ClipToBounds=true,Margin=new Thickness(-8,0,-8,0)};pageStack=stack;layers.Children.Add(stack);var header=new Grid{Height=40};header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(40)});header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(40)});stack.Children.Add(header);
      back=Theme.Button("",false);back.Width=112;back.Height=44;back.Padding=new Thickness(Theme.RowInset,0,Theme.RowInset,0);back.Background=Theme.IslandFill;back.HorizontalAlignment=HorizontalAlignment.Left;back.HorizontalContentAlignment=HorizontalAlignment.Stretch;
      var backContent=new Grid{VerticalAlignment=VerticalAlignment.Center};backContent.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(Theme.RowIconColumn)});backContent.ColumnDefinitions.Add(new ColumnDefinition());var backIcon=Glyph("M 9,2 L 4,7 L 9,12");backIcon.HorizontalAlignment=HorizontalAlignment.Left;backIcon.VerticalAlignment=VerticalAlignment.Center;backContent.Children.Add(backIcon);var backLabel=Theme.Text("返回",12,Theme.Muted);Grid.SetColumn(backLabel,1);backContent.Children.Add(backLabel);back.Content=backContent;
      back.Click+=(s,e)=>{if(settingsPage!=null&&settingsPage!="settings")OpenSettings();else ShowHome();};layers.Children.Add(back);
      title=Theme.Text("小菜单",14,Theme.Foreground);title.FontWeight=FontWeights.SemiBold;title.VerticalAlignment=VerticalAlignment.Center;title.TextAlignment=TextAlignment.Center;Grid.SetColumn(title,1);header.Children.Add(title);
      close=IconButton("M 3,3 L 11,11 M 11,3 L 3,11","关闭小菜单");close.Click+=(s,e)=>ClosePanel();close.HorizontalAlignment=HorizontalAlignment.Right;layers.Children.Add(close);
      featureNav=new System.Windows.Controls.Primitives.UniformGrid{Columns=2,Width=164,Height=32,HorizontalAlignment=HorizontalAlignment.Center,Visibility=Visibility.Collapsed};navIsland=Theme.Island(featureNav,new Thickness(3),20);navIsland.HorizontalAlignment=HorizontalAlignment.Center;Grid.SetColumn(navIsland,1);header.Children.Add(navIsland);
      foreach(string id in new[]{"timer","stats"}){string destination=id;var tab=Theme.Button(id=="timer"?"计时":"统计",false);tab.Style=Theme.RoundedButtonStyle(false,15);tab.Effect=null;tab.BorderBrush=Brushes.Transparent;tab.Height=32;tab.FontSize=12;tab.Margin=new Thickness(2,0,2,0);tab.Tag=id;L.Bind(tab,AutomationProperties.NameProperty,id=="timer"?"打开计时器":"打开统计");tab.Click+=(s,e)=>OpenFeature(destination);featureTabs.Add(tab);featureNav.Children.Add(tab);}
      body=new ContentControl{Width=Theme.MenuContentWidth,Margin=new Thickness(0,16,0,0),HorizontalContentAlignment=HorizontalAlignment.Stretch,RenderTransform=pageShift};pageScroll=new ScrollViewer{Content=body,HorizontalContentAlignment=HorizontalAlignment.Center,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,PanningMode=PanningMode.VerticalOnly};Theme.StyleScroll(pageScroll);stack.Children.Add(pageScroll);
      surface.SizeChanged+=(s,e)=>UpdateRevealGeometry();
      foreach(var feature in registry.Features){var f=feature;f.ContentChanged+=()=>{if(active==f)RequestPresentation();};}
      PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape){if(CardPresenter.IsPresenting){CardPresenter.Release();e.Handled=true;return;}var timer=active as TimerFeature;if(timer!=null&&timer.Panel.IsEditing)timer.Panel.CancelEditor();else ClosePanel();e.Handled=true;}};
      Closing+=(s,e)=>{if(!Disposing){e.Cancel=true;ClosePanel();}else CancelMotion();};L.Changed+=LanguageChanged;Closed+=(s,e)=>L.Changed-=LanguageChanged;ShowHome();
    }
    void LanguageChanged(){title.Text=settingsPage!=null?L.T(SettingsTitle(settingsPage)):active==null?L.T("小菜单"):active.Title;Refresh();Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>{if(!Disposing)Refresh();}));}
    static FrameworkElement Glyph(string geometry){return new System.Windows.Shapes.Path{Data=Geometry.Parse(geometry),Stroke=Theme.Muted,StrokeThickness=1.3,Width=12,Height=12,Stretch=Stretch.Uniform,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round};}
    static Button IconButton(string geometry,string name){var b=Theme.Button("",false);b.Content=Glyph(geometry);b.Width=40;b.Height=40;b.Background=Theme.IslandFill;L.Bind(b,FrameworkElement.ToolTipProperty,name);L.Bind(b,AutomationProperties.NameProperty,name);return b;}
    // The header retains its 40 DIP spacer. Return occupies the existing launcher
    // gutter independently of content measurement and page transition transforms.
    public void ShowHome(){if(active==null&&home!=null&&settingsPage==null)return;CardPresenter.Reset();navigationHeight=surface.ActualHeight;active=null;settingsPage=null;back.Visibility=Visibility.Hidden;title.Text=L.T("小菜单");summaries.Clear();home=new StackPanel();
      var label=Theme.Text("已安装",12,Theme.Muted);label.Margin=new Thickness(0,0,0,8);home.Children.Add(label);
      foreach(var feature in registry.Features){var f=feature;var button=Theme.Button("",false);button.Height=76;button.Margin=new Thickness(0,0,0,14);button.Padding=new Thickness(16,12,16,12);button.HorizontalContentAlignment=HorizontalAlignment.Stretch;L.Bind(button,AutomationProperties.NameProperty,f.Id=="timer"?"打开计时器":"打开"+f.Title);
        var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(12)});row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(12)});
        var icon=Glyph(f.Icon);icon.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(icon);
        var text=new StackPanel{Margin=new Thickness(8,0,8,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(text,1);row.Children.Add(text);text.Children.Add(Theme.Text(f.Title,14,Theme.Foreground));var summary=Theme.Text(f.Summary,12,Theme.Muted);summary.Margin=new Thickness(0,4,0,0);summary.TextTrimming=TextTrimming.CharacterEllipsis;text.Children.Add(summary);summaries.Add(f.Id,summary);
        var arrow=Glyph("M 4,2 L 9,7 L 4,12");arrow.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(arrow,2);row.Children.Add(arrow);button.Content=row;button.Click+=(s,e)=>OpenFeature(f.Id);home.Children.Add(button);
      }
      if(registry.Features.Count==0){var empty=Theme.Text("尚未安装功能",14,Theme.Muted);empty.Margin=new Thickness(0,8,0,8);home.Children.Add(empty);}

      var tools=new Grid();tools.ColumnDefinitions.Add(new ColumnDefinition());tools.ColumnDefinitions.Add(new ColumnDefinition());home.Children.Add(tools);
      var settings=Theme.Button("设置",false);settings.Margin=new Thickness(0,0,7,0);settings.Click+=(s,e)=>OpenSettings();tools.Children.Add(settings);
      var reset=Theme.Button("恢复位置",false);reset.Margin=new Thickness(7,0,0,0);reset.Click+=(s,e)=>restore();Grid.SetColumn(reset,1);tools.Children.Add(reset);
      var hint=Theme.Text(registry.Issues.Count>0?registry.Issues[0]:"拖动圆环调整位置 · 按住右键摆动卡片",12,Theme.Muted);hint.TextWrapping=TextWrapping.Wrap;hint.Margin=new Thickness(0,16,0,0);home.Children.Add(hint);body.Content=home;pageScroll.ScrollToTop();RequestPresentation(true);AnimatePage(-8);
    }
    static string SettingsTitle(string page){return page=="language"?"语言":page=="appearance"?"外观":page=="sound"?"提示音":"设置";}
    void SetBack(bool toSettings){back.Visibility=Visibility.Visible;string name=toSettings?"返回设置":"返回小菜单";L.Bind(back,FrameworkElement.ToolTipProperty,name);L.Bind(back,AutomationProperties.NameProperty,name);}
    void ShowSettingsPage(string page,FrameworkElement view,double direction){if(settingsPage==page)return;CardPresenter.Reset();navigationHeight=surface.ActualHeight;active=null;settingsPage=page;title.Text=L.T(SettingsTitle(page));SetBack(page!="settings");body.Content=view;pageScroll.ScrollToTop();body.UpdateLayout();RequestPresentation(true);AnimatePage(direction);}
    public void OpenSettings(){if(settingsOverview==null)settingsOverview=new SettingsOverview(appearance,OpenLanguageSettings,OpenAppearanceSettings,OpenSoundSettings);ShowSettingsPage("settings",settingsOverview,settingsPage==null?8:-8);}
    public void OpenLanguageSettings(){if(languageView==null)languageView=new LanguageSettingsPanel(appearance);ShowSettingsPage("language",languageView,8);}
    public void OpenAppearanceSettings(){if(settingsView==null)settingsView=new AppearancePanel(appearance);ShowSettingsPage("appearance",settingsView,8);}
    public void OpenSoundSettings(){if(soundView==null){var timer=registry.Find("timer") as TimerFeature;soundView=new SoundSettingsPanel(appearance,timer==null?null:timer.Engine,timer==null?(Action)null:timer.SaveAction);}ShowSettingsPage("sound",soundView,8);}
    public bool OpenFeature(string id){var feature=registry.Find(id);if(feature==null)return false;if(active==feature){feature.Refresh();return true;}CardPresenter.Reset();navigationHeight=surface.ActualHeight;preparing=true;try{settingsPage=null;active=feature;title.Text=feature.Title;SetBack(false);body.Content=feature.View;pageScroll.ScrollToTop();feature.Refresh();}finally{preparing=false;}RequestPresentation(true);AnimatePage(8);return true;}
    public void Refresh(){if(active!=null)active.Refresh();else foreach(var f in registry.Features){TextBlock label;if(summaries.TryGetValue(f.Id,out label)&&label.Text!=f.Summary)label.Text=f.Summary;}RequestPresentation();}
    double PageCapacity(){if(WorkingHeight!=null)return Math.Max(80,WorkingHeight()-162);var anchor=AnchorBounds==null?null:AnchorBounds();var screen=anchor.HasValue?System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(anchor.Value.Left,anchor.Value.Top)):System.Windows.Forms.Screen.FromHandle(Handle);return Math.Max(80,screen.WorkingArea.Height/Native.ScaleFor(Handle)-162);}
    Size NaturalSize(){var handle=Handle;surface.Padding=Above?new Thickness(24,24,24,64):new Thickness(24,64,24,24);back.VerticalAlignment=Above?VerticalAlignment.Bottom:VerticalAlignment.Top;
      double backInset=Theme.MenuRadius-back.Height/2;
      double backX=backInset-surface.Padding.Left-surface.BorderThickness.Left;
      double backY=backInset-(Above?surface.Padding.Bottom+surface.BorderThickness.Bottom:surface.Padding.Top+surface.BorderThickness.Top);
      back.Margin=Above?new Thickness(backX,0,0,backY):new Thickness(backX,backY,0,0);
      // Circular controls share the center of the outer corner arc. The header
      // shifts with its close button; body and return positions remain fixed.
      double cornerInset=Theme.MenuRadius-close.Width/2-25;
      close.VerticalAlignment=Above?VerticalAlignment.Top:VerticalAlignment.Bottom;close.Margin=Above?new Thickness(0,cornerInset,cornerInset,0):new Thickness(0,0,cornerInset,cornerInset);
      double shift=Above?cornerInset:0;pageStack.Margin=new Thickness(-8,shift,-8,0);body.Margin=new Thickness(0,16-shift,0,0);
      var view=(FrameworkElement)body.Content;view.Measure(new Size(Theme.MenuContentWidth,Double.PositiveInfinity));body.Height=view.DesiredSize.Height;double visible=System.Math.Min(view.DesiredSize.Height,System.Math.Floor(PageCapacity()));pageScroll.Height=visible+16-shift;return new Size(Theme.MenuWidth,Math.Ceiling(visible+162));}
    internal Size MeasurePopup(){return resizing?presentationSize:NaturalSize();}
    internal double ExpandedHeight(){double height=NaturalSize().Height;foreach(var f in registry.Features){if(f==active)continue;f.View.Measure(new Size(Theme.MenuContentWidth,Double.PositiveInfinity));height=Math.Max(height,f.View.DesiredSize.Height+162);}return Math.Min(height,Math.Floor(PageCapacity())+162);}
    // The content is clipped during transitions. Give the card the same explicit
    // height as the HWND so a previous page's clipped measure cannot constrain it.
    void CommitBounds(){if(!resizing)surface.Height=MeasurePopup().Height-16;if(PresentationRequested!=null)PresentationRequested();else{var current=PixelBounds;double scale=Native.ScaleFor(Handle);var size=MeasurePopup();int height=(int)Math.Round(size.Height*scale);SetBoundsPixels(current.Left,Above?current.Bottom-height:current.Top,(int)Math.Round(size.Width*scale),height);}UpdateLayout();}
    static DoubleAnimation Motion(double from,double to,int ms){return new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(ms)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};}
    void RequestPresentation(bool force=false){
      if(preparing)return;preparing=true;var editing=active as TimerFeature;back.IsEnabled=featureNav.IsEnabled=editing==null||!editing.Panel.IsEditing;
      bool quick=active!=null&&(active.Id=="timer"||active.Id=="stats")&&registry.Find("timer")!=null&&registry.Find("stats")!=null;featureNav.Visibility=quick?Visibility.Visible:Visibility.Collapsed;navIsland.Visibility=featureNav.Visibility;title.Visibility=quick?Visibility.Hidden:Visibility.Visible;foreach(var tab in featureTabs){bool selected=active!=null&&(string)tab.Tag==active.Id;tab.Background=selected?Theme.Primary:Brushes.Transparent;tab.Foreground=selected?Theme.PrimaryText:Theme.Muted;tab.Style=selected?navActiveStyle:navIdleStyle;}
      try{double start=Double.IsNaN(navigationHeight)?surface.ActualHeight:navigationHeight;navigationHeight=Double.NaN;var size=NaturalSize();if(size==presentedSize)return;int epoch=++sizeEpoch;CompositionTarget.Rendering-=OnSizeFrame;
        surface.BeginAnimation(HeightProperty,null);surface.VerticalAlignment=Above?VerticalAlignment.Bottom:VerticalAlignment.Top;presentedSize=size;
        if(IsVisible&&AnimationsEnabled&&start>0&&!IsClosing){
          resizing=true;presentationSize=new Size(size.Width,start+16);surface.Height=start;CommitBounds();
          var animation=Motion(start,size.Height-16,180);
          animation.Completed+=(s,e)=>{if(epoch!=sizeEpoch)return;CompositionTarget.Rendering-=OnSizeFrame;resizing=false;surface.BeginAnimation(HeightProperty,null);surface.Height=Double.NaN;CommitBounds();};
          surface.BeginAnimation(HeightProperty,animation);CompositionTarget.Rendering+=OnSizeFrame;
        }else{resizing=false;surface.Height=Double.NaN;CommitBounds();}
      }finally{preparing=false;}
    }
    // Size and placement use the same animated height. Clamping an oversized
    // envelope separately from its final size causes a jump at monitor edges.
    void OnSizeFrame(object sender,EventArgs args){if(!resizing||preparing)return;preparing=true;try{presentationSize=new Size(Theme.MenuWidth,surface.Height+16);CommitBounds();}finally{preparing=false;}}
    void AnimatePage(double offset){if(!IsVisible||!AnimationsEnabled)return;body.BeginAnimation(OpacityProperty,Motion(.25,1,150));pageShift.BeginAnimation(TranslateTransform.XProperty,Motion(offset,0,180));}
    void CancelMotion(){++visibilityEpoch;++sizeEpoch;CompositionTarget.Rendering-=OnSizeFrame;resizing=false;IsClosing=false;CancelReveal();surface.BeginAnimation(HeightProperty,null);surface.Height=Double.NaN;surface.BeginAnimation(OpacityProperty,null);surface.Opacity=1;reveal.BeginAnimation(TranslateTransform.YProperty,null);reveal.Y=0;body.BeginAnimation(OpacityProperty,null);pageShift.BeginAnimation(TranslateTransform.XProperty,null);surface.IsHitTestVisible=true;}
    public void HideImmediately(){Hide();CancelMotion();}
  }
}
