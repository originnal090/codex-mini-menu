using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace RingTimer {
  sealed class StatsFeature:IMenuFeature {
    readonly StatsStore store;StatsPanel panel;
    public StatsFeature(StatsStore value){store=value;}
    public string Id{get{return "stats";}}public string Title{get{return L.T("统计");}}
    public string Summary{get{return L.T("今日")+" · "+StatsPanel.Duration(store.Day(DateTime.Today).Seconds);}}
    public string Icon{get{return "M 2,12 L 2,8 M 7,12 L 7,2 M 12,12 L 12,5";}}
    public FrameworkElement View{get{if(panel==null)panel=new StatsPanel(store);return panel;}}
    public bool Tick(){return false;}public bool PlaySound{get{return false;}}
    public event Action ContentChanged {add{}remove{}}
    public void Refresh(){if(panel!=null)panel.Refresh();}
  }
  sealed class StatsPanel:UserControl {
    readonly StatsStore store;readonly Func<DateTime> clock;readonly TextBlock summary,restSummary,notice;readonly SlidingRail rail;string category="work";readonly ContentControl body;readonly List<Button> tabs=new List<Button>();
    string view="calendar",period="day",paintedLanguage;DateTime selected,paintedToday;DateTime selectedBucket;int page,paintedRevision=-1;bool refreshPending;const double PlotWidth=278;
    readonly Style activeStyle=Theme.RoundedButtonStyle(true,8),inactiveStyle=Theme.RoundedButtonStyle(false,8);
    public string SelectedView{get{return view;}}
    public StatsPanel(StatsStore data):this(data,()=>DateTime.Today){}
    public StatsPanel(StatsStore data,Func<DateTime> date){store=data;clock=date;selected=clock().Date;Height=Theme.FeatureHeight;FontFamily=Theme.Font;
      var stack=new StackPanel();Content=stack;var summaries=new Grid{Height=84,Tag="wave"};summaries.ColumnDefinitions.Add(new ColumnDefinition());summaries.ColumnDefinitions.Add(new ColumnDefinition());stack.Children.Add(summaries);
      var a=new StackPanel();var workIsland=Theme.Island(a,new Thickness(14));workIsland.Margin=new Thickness(0,0,7,0);summaries.Children.Add(workIsland);a.Children.Add(Theme.Text("● 今日工作",11,Theme.Done));summary=Theme.Text("",26,Theme.Foreground);a.Children.Add(summary);
      var b=new StackPanel();var restIsland=Theme.Island(b,new Thickness(14));restIsland.Margin=new Thickness(7,0,0,0);Grid.SetColumn(restIsland,1);summaries.Children.Add(restIsland);b.Children.Add(Theme.Text("● 今日休息",11,Theme.Focus));restSummary=Theme.Text("",26,Theme.Foreground);b.Children.Add(restSummary);
      rail=new SlidingRail("日历","总量","分段"){Margin=new Thickness(0,14,0,14),Tag="wave"};rail.Changed+=i=>SelectView(new[]{"calendar","total","sessions"}[i]);stack.Children.Add(rail);rail.GestureEnded+=FlushPending;
      body=new ContentControl{Height=268,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Top};stack.Children.Add(Theme.Island(body,new Thickness(11,12,11,12)));notice=Theme.Text("",11,Theme.Error);notice.Height=16;notice.TextTrimming=TextTrimming.CharacterEllipsis;stack.Children.Add(notice);
      body.LostMouseCapture+=(s,e)=>FlushPending();body.PreviewKeyUp+=(s,e)=>FlushPending();
      Loaded+=(s,e)=>{store.Changed+=OnChanged;Theme.Changed+=OnChanged;L.Changed+=OnChanged;Refresh(true);};Unloaded+=(s,e)=>{store.Changed-=OnChanged;Theme.Changed-=OnChanged;L.Changed-=OnChanged;};Refresh(true);
    }
    void OnChanged(){Refresh(true);}
    void FlushPending(){if(refreshPending)Dispatcher.BeginInvoke(new Action(()=>Refresh(false)));}
    void SelectButton(Button button,bool active){button.Background=active?Theme.Primary:Theme.GlassControl;button.Foreground=active?Theme.PrimaryText:Theme.Muted;button.Style=active?activeStyle:inactiveStyle;}
    public static string Duration(double seconds){if(seconds>0&&seconds<60)return Math.Ceiling(seconds).ToString(CultureInfo.InvariantCulture)+" "+L.T("秒");return Math.Round(seconds/60,1).ToString("0.#",CultureInfo.InvariantCulture)+" "+(L.Language=="en"?"min":"分钟");}
    internal static void SetDurationText(TextBlock text,double seconds,double size){string label=Duration(seconds);int split=label.IndexOf(' ');text.Inlines.Clear();text.Inlines.Add(new Run(split<0?label:label.Substring(0,split)){FontFamily=Theme.NumberFont,FontSize=size});if(split>=0)text.Inlines.Add(new Run(label.Substring(split)){FontFamily=Theme.ChineseFont,FontSize=11,Foreground=Theme.Muted});}
    static string DateLabel(DateTime date){return date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
    static string TimeLabel(double minute){int n=(int)Math.Floor(minute);return (n/60).ToString("00")+":"+(n%60).ToString("00");}
    static Button SmallButton(string text){var b=Theme.Button(text,false);b.Style=Theme.RoundedButtonStyle(false,8);b.Height=28;b.FontSize=12;L.Bind(b,AutomationProperties.NameProperty,text);return b;}
    static void At(Canvas canvas,UIElement item,double x,double y){Canvas.SetLeft(item,x);Canvas.SetTop(item,y);canvas.Children.Add(item);}
    static TextBlock Label(string text,double width=Double.NaN){var t=Theme.Text(text,11,Theme.Muted);if(!Double.IsNaN(width))t.Width=width;return t;}
    public void SelectView(string id){if(id!="calendar"&&id!="total"&&id!="sessions")throw new ArgumentOutOfRangeException("id");view=id;int index=id=="calendar"?0:id=="total"?1:2;if(rail.Selected!=index)rail.Select(index,false);Refresh(true);}
    public void Refresh(){Refresh(false);}
    void Refresh(bool force){DateTime today=clock().Date;if(!force&&!refreshPending&&paintedRevision==store.Revision&&paintedToday==today&&paintedLanguage==L.Language)return;
      // Keep the pressed control or scroll thumb alive until the gesture finishes.
      if(rail.IsInteracting||body.IsMouseCaptureWithin||(body.IsKeyboardFocusWithin&&(Keyboard.IsKeyDown(Key.Space)||Keyboard.IsKeyDown(Key.Return)))){refreshPending=true;return;}
      refreshPending=false;paintedRevision=store.Revision;paintedToday=today;paintedLanguage=L.Language;
      var current=store.Day(today);SetDurationText(summary,current.WorkSeconds,26);SetDurationText(restSummary,current.RestSeconds,26);
      string[] ids={"calendar","total","sessions"};for(int i=0;i<tabs.Count;i++){bool active=ids[i]==view;SelectButton(tabs[i],active);AutomationProperties.SetItemStatus(tabs[i],active?L.T("已选择"):"");}
      var oldBody=body.Content as DependencyObject;var buttons=Descendants<Button>(oldBody).ToList();int focused=buttons.FindIndex(b=>b.IsKeyboardFocusWithin);var oldScroll=Descendants<ScrollViewer>(oldBody).FirstOrDefault();double offset=oldScroll==null?0:oldScroll.VerticalOffset;
      body.Content=view=="calendar"?Calendar():view=="total"?Totals():Sessions();body.UpdateLayout();var newScroll=Descendants<ScrollViewer>(body).FirstOrDefault();if(newScroll!=null)newScroll.ScrollToVerticalOffset(offset);var newButtons=Descendants<Button>(body).ToList();if(focused>=0&&focused<newButtons.Count)newButtons[focused].Focus();notice.Text=store.SaveFailed?L.T("统计暂未保存，将自动重试"):store.Recovered?L.T("已恢复统计，原文件已备份"):"";notice.ToolTip=notice.Text;
    }
    static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject{if(root==null)yield break;if(root is T)yield return (T)root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Descendants<T>(VisualTreeHelper.GetChild(root,i)))yield return child;}
    Grid Pager(string title,Action previous,Action next,bool canNext){var grid=new Grid{Height=28};grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(28)});grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(28)});
      var left=SmallButton("‹");L.Bind(left,AutomationProperties.NameProperty,"上一页");left.Click+=(s,e)=>previous();grid.Children.Add(left);var label=Theme.Text(title,12,Theme.Foreground);label.TextAlignment=TextAlignment.Center;label.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(label,1);grid.Children.Add(label);var right=SmallButton("›");L.Bind(right,AutomationProperties.NameProperty,"下一页");right.IsEnabled=canNext;right.Click+=(s,e)=>next();Grid.SetColumn(right,2);grid.Children.Add(right);return grid;
    }
    double CategorySeconds(StatsBucket b){return category=="work"?b.WorkSeconds:b.RestSeconds;}
    Brush CategoryBrush{get{return KindBrush(category);}}
    static string KindLabel(string kind){return L.T(kind=="rest"?"休息":"工作");}
    static Brush KindBrush(string kind){return kind=="work"?Theme.Done:kind=="rest"?Theme.Focus:Theme.Muted;}
    FrameworkElement Calendar(){var stack=new StackPanel();var filter=new UniformGrid{Columns=2,Height=26};stack.Children.Add(filter);foreach(string kind in new[]{"work","rest"}){string k=kind;var b=SmallButton(KindLabel(k));b.Height=24;b.Margin=new Thickness(2,0,2,0);SelectButton(b,category==k);b.Click+=(se,ev)=>{category=k;Refresh(true);};filter.Children.Add(b);}DateTime today=clock().Date,monday=today.AddDays(-((int)today.DayOfWeek+6)%7),end=monday.AddDays(7-page*91),start=end.AddDays(-91);
      stack.Children.Add(Pager(start.ToString("MM.dd")+" – "+end.AddDays(-1).ToString("yyyy.MM.dd"),()=>{page++;selected=start.AddDays(-1);Refresh(true);},()=>{page=Math.Max(0,page-1);selected=page==0?today:end.AddDays(90);Refresh(true);},page>0));
      var months=new Canvas{Height=18};stack.Children.Add(months);int lastMonth=-1;for(int w=0;w<13;w++){var day=start.AddDays(w*7);if(day.Month!=lastMonth){At(months,Label(L.Language=="en"?day.ToString("MMM",CultureInfo.InvariantCulture):day.ToString("MM")+"月"),24+w*19,5);lastMonth=day.Month;}}
      var heat=new Canvas{Height=105};stack.Children.Add(heat);string[] dayNames=L.Language=="en"?new[]{"M","T","W","T","F","S","S"}:new[]{"一","二","三","四","五","六","日"};for(int r=0;r<7;r++)At(heat,Label(dayNames[r]),0,r*15);
      for(int w=0;w<13;w++)for(int r=0;r<7;r++){DateTime day=start.AddDays(w*7+r);var daily=store.Day(day);double seconds=CategorySeconds(daily);int level=seconds<=0?0:seconds<900?1:seconds<1800?2:seconds<3600?3:4;var color=(SolidColorBrush)(level==0?Theme.GlassControl:CategoryBrush);var fill=color.Clone();fill.Opacity=level==0?1:new[]{0,.25,.45,.7,1}[level];
        var tile=new Border{Width=13,Height=13,CornerRadius=new CornerRadius(3),Background=fill,BorderBrush=day==selected?Theme.Foreground:Brushes.Transparent,BorderThickness=new Thickness(day==selected?1:0)};
        var button=new Button{Width=19,Height=15,Style=Theme.BareStyle,Content=tile,IsEnabled=day<=today,FocusVisualStyle=Theme.FocusRing,ToolTip=DateLabel(day)+" · "+KindLabel(category)+" "+Duration(CategorySeconds(daily))+" · "+(category=="work"?daily.WorkSessions:daily.RestSessions)+" "+L.T("次")};AutomationProperties.SetName(button,(string)button.ToolTip);if(day>today)button.Opacity=.2;button.Click+=(s,e)=>{selected=day;Refresh(true);};At(heat,button,22+w*19,r*15);
      }
      var legend=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Height=18,Margin=new Thickness(0,4,0,0)};legend.Children.Add(Label(L.T("少")));for(int i=0;i<5;i++){var color=((SolidColorBrush)(i==0?Theme.GlassControl:CategoryBrush)).Clone();color.Opacity=i==0?1:new[]{0,.25,.45,.7,1}[i];legend.Children.Add(new Border{Width=8,Height=8,Margin=new Thickness(3,0,0,0),CornerRadius=new CornerRadius(2),Background=color,VerticalAlignment=VerticalAlignment.Center});}legend.Children.Add(Label(" "+L.T("多")));stack.Children.Add(legend);
      var chosen=store.Day(selected);var detail=SmallButton(DateLabel(selected)+" · "+KindLabel(category)+" "+Duration(CategorySeconds(chosen))+" · "+(category=="work"?chosen.WorkSessions:chosen.RestSessions)+" "+L.T("次")+"  ›");detail.Height=30;detail.FontSize=11;detail.Click+=(s,e)=>SelectView("sessions");AutomationProperties.SetName(detail,L.T("查看当天分段")+" "+DateLabel(selected));stack.Children.Add(detail);return stack;
    }
    FrameworkElement Totals(){var stack=new StackPanel();var periods=new UniformGrid{Columns=3,Height=28};stack.Children.Add(periods);string[] names={"日","周","月"},ids={"day","week","month"};for(int i=0;i<3;i++){string id=ids[i];var b=SmallButton(names[i]);SelectButton(b,period==id);b.Margin=new Thickness(i==0?0:3,0,i==2?0:3,0);b.Click+=(s,e)=>{period=id;selectedBucket=DateTime.MinValue;Refresh(true);};periods.Children.Add(b);}
      DateTime today=clock().Date;var buckets=new List<StatsBucket>();int count=period=="day"?7:6;for(int i=count-1;i>=0;i--){DateTime first=period=="day"?today.AddDays(-i):period=="week"?today.AddDays(-((int)today.DayOfWeek+6)%7-i*7):new DateTime(today.Year,today.Month,1).AddMonths(-i);DateTime last=period=="day"?first.AddDays(1):period=="week"?first.AddDays(7):first.AddMonths(1);buckets.Add(store.Aggregate(first,last));}
      var detail=Label("",PlotWidth);detail.Height=30;detail.TextWrapping=TextWrapping.Wrap;
      var chart=new Canvas{Height=180,Margin=new Thickness(0,6,0,0)};stack.Children.Add(chart);double max=Math.Max(60,buckets.Max(b=>b.Seconds))*1.15,baseY=148,plotH=120,left=42,step=(PlotWidth-left-4)/count;
      At(chart,Label(L.T("分钟")),0,0);At(chart,Label(Math.Ceiling(max/60).ToString("0")),0,20);At(chart,Label("0"),4,136);var axis=new Line{X1=left,Y1=baseY,X2=PlotWidth,Y2=baseY,Stroke=Theme.Track,StrokeThickness=1};chart.Children.Add(axis);
      for(int i=0;i<count;i++){var bucket=buckets[i];double height=bucket.Seconds<=0?0:Math.Max(2,bucket.Seconds/max*plotH),x=left+i*step;double bottom=baseY;foreach(string kind in new[]{"work","rest"}){double seconds=kind=="work"?bucket.WorkSeconds:bucket.RestSeconds;double part=seconds/max*plotH;if(part<=0)continue;bottom-=part;At(chart,new Border{Width=Math.Max(10,step-12),Height=part,Background=KindBrush(kind),CornerRadius=new CornerRadius(2)},x+6,bottom);}string name=bucket.Start.ToString(period=="month"?"yyyy.MM":"MM.dd")+" · "+KindLabel("work")+" "+Duration(bucket.WorkSeconds)+" / "+KindLabel("rest")+" "+Duration(bucket.RestSeconds);var hit=new Button{Style=Theme.BareStyle,Width=step-2,Height=130,Background=Brushes.Transparent,Content=new Border{Background=Brushes.Transparent},ToolTip=name,FocusVisualStyle=Theme.FocusRing};AutomationProperties.SetName(hit,name);hit.Click+=(s,e)=>{selectedBucket=bucket.Start;detail.Text=name;};At(chart,hit,x,20);At(chart,Label(bucket.Start.ToString(period=="month"?(L.Language=="en"?"MMM":"MM月"):"MM.dd",CultureInfo.InvariantCulture),step),x,154);}
      var picked=buckets.FirstOrDefault(b=>b.Start==selectedBucket);detail.Text=picked==null?KindLabel("work")+" "+Duration(buckets.Sum(b=>b.WorkSeconds))+"   "+KindLabel("rest")+" "+Duration(buckets.Sum(b=>b.RestSeconds)):picked.Start.ToString(period=="month"?"yyyy.MM":"MM.dd")+" · "+KindLabel("work")+" "+Duration(picked.WorkSeconds)+" / "+KindLabel("rest")+" "+Duration(picked.RestSeconds);stack.Children.Add(detail);
      return stack;
    }
    FrameworkElement Sessions(){var stack=new StackPanel();stack.Children.Add(Pager(DateLabel(selected),()=>{selected=selected.AddDays(-1);Refresh(true);},()=>{selected=selected.AddDays(1);Refresh(true);},selected<clock().Date));var slices=store.Slices(selected);var daily=store.Day(selected);var heading=Label(Duration(daily.Seconds)+" · "+daily.Sessions+" "+L.T("次"));heading.Margin=new Thickness(0,6,0,0);heading.Height=22;stack.Children.Add(heading);
      if(slices.Count==0){var empty=Theme.Text("暂无记录",13,Theme.Muted);empty.TextAlignment=TextAlignment.Center;empty.Margin=new Thickness(0,52,0,0);stack.Children.Add(empty);return stack;}
      var timeline=new Canvas{Height=62};stack.Children.Add(timeline);timeline.Children.Add(new Line{X1=0,Y1=22,X2=PlotWidth,Y2=22,Stroke=Theme.Track,StrokeThickness=1});foreach(var slice in slices){double x=slice.StartMinute/1440*(PlotWidth-8),width=Math.Max(2,(Math.Max(slice.StartMinute,slice.EndMinute)-slice.StartMinute)/1440*(PlotWidth-8));At(timeline,new Border{Width=width,Height=12,Background=KindBrush(slice.Kind),CornerRadius=new CornerRadius(2),ToolTip=KindLabel(slice.Kind)+" · "+TimeLabel(slice.StartMinute)+" – "+TimeLabel(slice.EndMinute)+" · "+Duration(slice.Seconds)},x,16);}for(int i=0;i<=4;i++)At(timeline,Label((i*6).ToString("00")),i*(PlotWidth-18)/4,36);
      var list=new StackPanel();foreach(var slice in slices){var row=new Grid{Height=34,Margin=new Thickness(0,0,0,3)};row.Children.Add(Theme.Text("● "+KindLabel(slice.Kind)+"   "+TimeLabel(slice.StartMinute)+" – "+TimeLabel(slice.EndMinute),11,KindBrush(slice.Kind)));var length=Theme.Text(Duration(slice.Seconds),12,Theme.Muted);length.HorizontalAlignment=HorizontalAlignment.Right;row.Children.Add(length);AutomationProperties.SetName(row,TimeLabel(slice.StartMinute)+" – "+TimeLabel(slice.EndMinute)+" "+Duration(slice.Seconds));list.Children.Add(row);}var scroll=new ScrollViewer{Height=126,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=list};
      var style=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'><Setter Property='Width' Value='6'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'><Grid Background='Transparent'><Track Name='PART_Track' IsDirectionReversed='True' Orientation='Vertical'><Track.DecreaseRepeatButton><RepeatButton Command='{x:Static ScrollBar.PageUpCommand}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton><Track.Thumb><Thumb><Thumb.Template><ControlTemplate><Border Background='{DynamicResource ScrollInk}' CornerRadius='3' Margin='1'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton Command='{x:Static ScrollBar.PageDownCommand}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton></Track></Grid></ControlTemplate></Setter.Value></Setter></Style>");
      scroll.Resources["ScrollInk"]=Theme.Line;scroll.Resources[typeof(ScrollBar)]=style;stack.Children.Add(scroll);return stack;
    }
  }
}