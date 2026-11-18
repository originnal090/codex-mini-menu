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
    readonly FeatureRegistry registry;readonly Action closed,restore;readonly TextBlock title;readonly Button back;readonly ContentControl body;readonly SlidingRail featureNav;
    readonly Dictionary<string,TextBlock> summaries=new Dictionary<string,TextBlock>();StackPanel home;IMenuFeature active;readonly AppearanceSettings appearance;AppearancePanel settingsView;SoundSettingsPanel soundView;LanguageSettingsPanel languageView;SettingsOverview settingsOverview;string settingsPage;
    readonly Border surface;readonly TranslateTransform reveal=new TranslateTransform(),pageShift=new TranslateTransform();
    readonly Button close;readonly StackPanel pageStack;
    Bubble launcher;
    internal Bubble Launcher{get{return launcher;}set{if(CardPresenter!=null)CardPresenter.SetDockedLauncher(false);if(launcher!=null){launcher.IsVisibleChanged-=LauncherVisibilityChanged;launcher.SetDocked(false);}launcher=value;if(launcher!=null){launcher.IsVisibleChanged+=LauncherVisibilityChanged;launcher.SetDocked(IsVisible);}if(CardPresenter!=null)CardPresenter.SetDockedLauncher(IsVisible);}}
    void LauncherVisibilityChanged(object sender,DependencyPropertyChangedEventArgs e){if(CardPresenter!=null){CardPresenter.SetDockedLauncher(IsVisible);RestoreCardContour();}}
    internal SilverCardPresenter CardPresenter{get;private set;}
    internal Border CardSurface{get{return surface;}}
    internal bool IsDurationEditing{get{var timer=active as TimerFeature;return timer!=null&&timer.Panel.IsEditing;}}
    internal bool CanTiltCard{get{return !IsClosing&&!resizing&&!morphing&&RevealProgress>.99&&ContentRevealProgress>=1&&!IsDurationEditing;}}
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
      IsVisibleChanged+=(s,e)=>{if(Launcher!=null)Launcher.SetDocked(IsVisible);CardPresenter.SetDockedLauncher(IsVisible);};Closed+=(s,e)=>{CardPresenter.SetDockedLauncher(false);if(Launcher!=null){Launcher.IsVisibleChanged-=LauncherVisibilityChanged;Launcher.SetDocked(false);}};
      SourceInitialized+=(s,e)=>System.Windows.Interop.HwndSource.FromHwnd(Handle).AddHook(LauncherHitTest);
      // The header hit targets extend 8 DIP into the card padding. Give their
      // parent that actual width so clipping keeps the entire 40 DIP buttons.
      // Body margins restore its original content width; vertical clipping still
      // contains the page during the continuous height transition.
      var layers=new Grid();surface.Child=layers;
      var stack=new StackPanel{ClipToBounds=true,Margin=new Thickness(-8,0,-8,0)};pageStack=stack;layers.Children.Add(stack);var header=new Grid{Height=40};header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(40)});header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(40)});stack.Children.Add(header);
      back=Theme.Button("",false);back.Width=112;back.Height=44;back.Padding=new Thickness(Theme.RowInset,0,Theme.RowInset,0);back.Background=Theme.IslandFill;back.HorizontalAlignment=HorizontalAlignment.Left;back.HorizontalContentAlignment=HorizontalAlignment.Stretch;
      var backContent=new Grid{VerticalAlignment=VerticalAlignment.Center};backContent.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(Theme.RowIconColumn)});backContent.ColumnDefinitions.Add(new ColumnDefinition());var backIcon=Glyph("M 9,2 L 4,7 L 9,12");backIcon.HorizontalAlignment=HorizontalAlignment.Left;backIcon.VerticalAlignment=VerticalAlignment.Center;backContent.Children.Add(backIcon);var backLabel=Theme.Text("返回",12,Theme.Muted);Grid.SetColumn(backLabel,1);backContent.Children.Add(backLabel);back.Content=backContent;
      back.Click+=(s,e)=>{if(settingsPage!=null&&settingsPage!="settings")OpenSettings();else ShowHome();};layers.Children.Add(back);
      title=Theme.Text("小菜单",14,Theme.Foreground);title.FontWeight=FontWeights.SemiBold;title.VerticalAlignment=VerticalAlignment.Center;title.TextAlignment=TextAlignment.Center;L.Bind(title,FrameworkElement.ToolTipProperty,"按住右键摆动卡片");Grid.SetColumn(title,1);header.Children.Add(title);
      close=IconButton("M 3,3 L 11,11 M 11,3 L 3,11","关闭小菜单");close.Click+=(s,e)=>ClosePanel();close.HorizontalAlignment=HorizontalAlignment.Right;layers.Children.Add(close);
      featureNav=new SlidingRail("计时","统计"){Width=170,HorizontalAlignment=HorizontalAlignment.Center,Visibility=Visibility.Collapsed};
      L.Bind(featureNav,AutomationProperties.NameProperty,"计时与统计视图");featureNav.SetActionNames("打开计时器","打开统计");
      featureNav.Changed+=i=>OpenFeature(i==0?"timer":"stats");Grid.SetColumn(featureNav,1);header.Children.Add(featureNav);
      // Retain the page cache through resize, interruption and reopen. The outer
      // card cache changes with its contour; this inner cache reuses page pixels.
      body=new ContentControl{Width=Theme.MenuContentWidth,Margin=new Thickness(0,16,0,0),HorizontalContentAlignment=HorizontalAlignment.Stretch,RenderTransform=pageShift,CacheMode=new BitmapCache{EnableClearType=false,SnapsToDevicePixels=true}};pageScroll=new ScrollViewer{Content=body,HorizontalContentAlignment=HorizontalAlignment.Center,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,PanningMode=PanningMode.VerticalOnly};Theme.StyleScroll(pageScroll);stack.Children.Add(pageScroll);
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
      foreach(var feature in registry.Features){if(feature.Id!="timer"&&feature.Id!="context")continue;var f=feature;var button=Theme.Button("",false);button.Height=Double.NaN;button.MinHeight=76;button.Margin=new Thickness(0,0,0,14);button.Padding=new Thickness(16,12,16,12);button.HorizontalContentAlignment=HorizontalAlignment.Stretch;L.Bind(button,AutomationProperties.NameProperty,f.Id=="timer"?"打开计时器":"打开"+f.Title);
        var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(12)});row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(12)});
        var icon=Glyph(f.Icon);icon.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(icon);
        var text=new StackPanel{Margin=new Thickness(8,0,8,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(text,1);row.Children.Add(text);text.Children.Add(Theme.Text(f.Title,14,Theme.Foreground));var summary=Theme.Text("",12,Theme.Muted);summary.TextWrapping=TextWrapping.Wrap;var status=new Border{Child=summary,CornerRadius=new CornerRadius(6),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,4,0,0)};text.Children.Add(status);summaries.Add(f.Id,summary);PaintSummary(f,summary);
        var arrow=Glyph("M 4,2 L 9,7 L 4,12");arrow.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(arrow,2);row.Children.Add(arrow);button.Content=row;button.Click+=(s,e)=>OpenFeature(f.Id);home.Children.Add(button);
      }
      if(summaries.Count==0){var empty=Theme.Text("尚未安装功能",14,Theme.Muted);empty.Margin=new Thickness(0,8,0,8);home.Children.Add(empty);}

      var settings=Theme.Button("设置",false);settings.Click+=(s,e)=>OpenSettings();home.Children.Add(settings);
      if(registry.Issues.Count>0){var hint=Theme.Text(registry.Issues[0],12,Theme.Paused);hint.TextWrapping=TextWrapping.Wrap;hint.Margin=new Thickness(0,12,0,0);home.Children.Add(hint);}body.Content=home;pageScroll.ScrollToTop();RequestPresentation(true);AnimatePage();
    }
    static string SettingsTitle(string page){return page=="language"?"语言":page=="appearance"?"外观":page=="sound"?"提示音":"设置";}
    void SetBack(bool toSettings){back.Visibility=Visibility.Visible;string name=toSettings?"返回设置":"返回小菜单";L.Bind(back,FrameworkElement.ToolTipProperty,name);L.Bind(back,AutomationProperties.NameProperty,name);}
    void ShowSettingsPage(string page,FrameworkElement view){if(settingsPage==page)return;CardPresenter.Reset();navigationHeight=surface.ActualHeight;active=null;settingsPage=page;title.Text=L.T(SettingsTitle(page));SetBack(page!="settings");body.Content=view;pageScroll.ScrollToTop();geometryUpdating=true;try{body.UpdateLayout();}finally{geometryUpdating=false;}RequestPresentation(true);AnimatePage();}
    public void OpenSettings(){if(settingsOverview==null)settingsOverview=new SettingsOverview(appearance,OpenLanguageSettings,OpenAppearanceSettings,OpenSoundSettings,restore);ShowSettingsPage("settings",settingsOverview);}
    public void OpenLanguageSettings(){if(languageView==null)languageView=new LanguageSettingsPanel(appearance);ShowSettingsPage("language",languageView);}
    public void OpenAppearanceSettings(){if(settingsView==null)settingsView=new AppearancePanel(appearance);ShowSettingsPage("appearance",settingsView);}
    public void OpenSoundSettings(){if(soundView==null){var timer=registry.Find("timer") as TimerFeature;soundView=new SoundSettingsPanel(appearance,timer==null?null:timer.Engine,timer==null?(Action)null:timer.SaveAction);}ShowSettingsPage("sound",soundView);}
    public bool OpenFeature(string id){var feature=registry.Find(id);if(feature==null)return false;if(active==feature){feature.Refresh();return true;}CardPresenter.Reset();navigationHeight=surface.ActualHeight;preparing=true;try{settingsPage=null;active=feature;title.Text=feature.Title;SetBack(false);body.Content=feature.View;pageScroll.ScrollToTop();feature.Refresh();}finally{preparing=false;}RequestPresentation(true);AnimatePage();return true;}
    static void PaintSummary(IMenuFeature feature,TextBlock label){
      var timer=feature as TimerFeature;var context=feature as ContextFeature;bool finished=timer!=null&&timer.Engine.State.Status=="finished";
      label.Text=(finished?"✓ ":"")+feature.Summary;label.Foreground=finished?Theme.Done:context!=null?ContextFeature.Ink(context.Usage):Theme.Muted;
      label.FontWeight=finished?FontWeights.SemiBold:FontWeights.Normal;label.ToolTip=context!=null&&!context.Usage.Known?context.UnavailableHelp:null;
      var status=(Border)label.Parent;status.Padding=finished?new Thickness(7,2,7,2):new Thickness(0);status.Background=finished?Theme.IslandFill:Brushes.Transparent;status.BorderBrush=finished?Theme.Done:Brushes.Transparent;status.BorderThickness=finished?new Thickness(1):new Thickness(0);
      AutomationProperties.SetName(label,label.Text);
    }
    public void Refresh(){if(active!=null)active.Refresh();else foreach(var f in registry.Features){TextBlock label;if(summaries.TryGetValue(f.Id,out label))PaintSummary(f,label);}RequestPresentation();}
    double PageCapacity(){
      if(WorkingHeight!=null)return Math.Max(80,WorkingHeight()-162);
      var anchor=AnchorBounds==null?null:AnchorBounds();var screen=anchor.HasValue?System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(anchor.Value.Left,anchor.Value.Top)):System.Windows.Forms.Screen.FromHandle(Handle);
      double scale=Native.ScaleFor(Handle),available=screen.WorkingArea.Height;
      // Fit long pages on the selected side instead of growing past the screen
      // and then translating the anchor. Extra content remains scrollable.
      if(anchor.HasValue){double inset=Theme.LauncherDockInset*scale;available=Math.Min(available,Above?anchor.Value.Bottom+inset-screen.WorkingArea.Top:screen.WorkingArea.Bottom-anchor.Value.Top+inset);}
      return Math.Max(80,available/scale-162);
    }
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
    // Reserve one compositor surface for the visible session. Page navigation
    // changes the clipped card inside it, never reallocates/rebases the HWND.
    double backingHeight;
    internal Size MeasurePopup(){return backingHeight>0||resizing||geometryUpdating?presentationSize:NaturalSize();}
    internal int PlacementHeightPixels{get{return (int)Math.Round((resizing?Math.Max(resizeFrom,resizeTo)+16:presentedSize.Height)*Native.ScaleFor(Handle));}}
    internal int PlacementOffsetPixels{get{return Above?Math.Max(0,(int)Math.Round(presentationSize.Height*Native.ScaleFor(Handle))-PlacementHeightPixels):0;}}
    void ReserveBacking(){if(backingHeight<=0&&AnchorBounds!=null){var size=NaturalSize();var anchor=AnchorBounds();var screen=anchor.HasValue?System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(anchor.Value.Left,anchor.Value.Top)):System.Windows.Forms.Screen.FromHandle(Handle);double capacity=WorkingHeight==null?screen.WorkingArea.Height/Native.ScaleFor(Handle):WorkingHeight();backingHeight=Math.Max(size.Height,Math.Floor(capacity));}}
    internal double ExpandedHeight(){double height=NaturalSize().Height;foreach(var f in registry.Features){if(f==active)continue;f.View.Measure(new Size(Theme.MenuContentWidth,Double.PositiveInfinity));height=Math.Max(height,f.View.DesiredSize.Height+162);}return Math.Min(height,Math.Floor(PageCapacity())+162);}
    // The content is clipped during transitions. Give the card the same explicit
    // height as the HWND so a previous page's clipped measure cannot constrain it.
    bool geometryUpdating;
    readonly System.Diagnostics.Stopwatch resizeClock=new System.Diagnostics.Stopwatch();
    double resizeFrom,resizeTo;
    void CommitBounds(){
      if(!resizing){presentedSize=NaturalSize();presentationSize=new Size(presentedSize.Width,Math.Max(backingHeight,presentedSize.Height));surface.Height=presentedSize.Height-16;}
      geometryUpdating=true;
      try{if(PresentationRequested!=null)PresentationRequested();else{var current=PixelBounds;double scale=Native.ScaleFor(Handle);var size=MeasurePopup();int height=(int)Math.Round(size.Height*scale);SetBoundsPixels(current.Left,Above?current.Bottom-height:current.Top,(int)Math.Round(size.Width*scale),height);}UpdateLayout();}
      finally{geometryUpdating=false;}
      UpdateRevealGeometry();
    }
    static DoubleAnimation Motion(double from,double to,int ms){return new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(ms)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};}
    void RequestPresentation(bool force=false){
      if(preparing)return;preparing=true;var editing=active as TimerFeature;back.IsEnabled=featureNav.IsEnabled=editing==null||!editing.Panel.IsEditing;
      bool quick=active!=null&&(active.Id=="timer"||active.Id=="stats")&&registry.Find("timer")!=null&&registry.Find("stats")!=null;featureNav.Visibility=quick?Visibility.Visible:Visibility.Collapsed;title.Visibility=quick?Visibility.Hidden:Visibility.Visible;if(quick&&featureNav.Selected!=(active.Id=="timer"?0:1))featureNav.Select(active.Id=="timer"?0:1,false);
      try{double start=Double.IsNaN(navigationHeight)?surface.ActualHeight:navigationHeight;navigationHeight=Double.NaN;var size=NaturalSize();if(size==presentedSize)return;++sizeEpoch;CompositionTarget.Rendering-=OnSizeFrame;
        surface.BeginAnimation(HeightProperty,null);surface.VerticalAlignment=Above?VerticalAlignment.Bottom:VerticalAlignment.Top;presentedSize=size;
        if(IsVisible&&AnimationsEnabled&&start>0&&!IsClosing){
          // Reserve a fixed backing rectangle. Only the anchored visible shell
          // changes per frame, avoiding HWND resize/layout/region races.
          resizing=true;resizeFrom=start;resizeTo=size.Height-16;
          backingHeight=Math.Max(backingHeight,Math.Max(start,resizeTo)+16);
          presentationSize=new Size(size.Width,backingHeight);surface.Height=start;CommitBounds();
          resizeClock.Restart();CompositionTarget.Rendering+=OnSizeFrame;
        }else{resizing=false;surface.Height=Double.NaN;CommitBounds();}
      }finally{preparing=false;}
    }
    void OnSizeFrame(object sender,EventArgs args){
      if(!resizing||preparing)return;preparing=true;
      try{double t=Math.Min(1,resizeClock.Elapsed.TotalMilliseconds/220),ease=1-Math.Pow(1-t,3);
        geometryUpdating=true;try{surface.Height=resizeFrom+(resizeTo-resizeFrom)*ease;UpdateLayout();}finally{geometryUpdating=false;}
        UpdateRevealGeometry();
        if(t>=1){CompositionTarget.Rendering-=OnSizeFrame;resizeClock.Stop();resizing=false;CommitBounds();}
      }finally{preparing=false;}
    }
    void AnimatePage(){
      if(!IsVisible||IsClosing)return;
      body.BeginAnimation(OpacityProperty,null);body.Opacity=1;
      pageShift.BeginAnimation(TranslateTransform.XProperty,null);pageShift.X=0;
      // The timer and its statistics tab share the compact slide transition.
      if(active!=null&&(active.Id=="timer"||active.Id=="stats")){
        SetContentReveal(1);
        if(RevealAnimationsEnabled){body.BeginAnimation(OpacityProperty,Motion(.25,1,150));pageShift.BeginAnimation(TranslateTransform.XProperty,Motion(8,0,180));}
        return;
      }
      // Other destinations share the opening reveal with anchored resizing.
      SetContentReveal(RevealAnimationsEnabled?0:1);
      if(RevealAnimationsEnabled)BeginContentReveal();
    }
    void CancelMotion(){++visibilityEpoch;++sizeEpoch;CompositionTarget.Rendering-=OnSizeFrame;resizeClock.Stop();resizing=false;IsClosing=false;CancelReveal();surface.BeginAnimation(HeightProperty,null);surface.Height=Double.NaN;surface.BeginAnimation(OpacityProperty,null);surface.Opacity=1;reveal.BeginAnimation(TranslateTransform.YProperty,null);reveal.Y=0;body.BeginAnimation(OpacityProperty,null);body.Opacity=1;pageShift.BeginAnimation(TranslateTransform.XProperty,null);pageShift.X=0;surface.IsHitTestVisible=true;}
    public void HideImmediately(){Hide();CancelMotion();backingHeight=0;}
  }
}
