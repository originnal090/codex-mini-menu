using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RingTimer {
  static partial class Theme {
    public const int IslandRadius=24,IslandGap=14,FeatureHeight=464;
    public static readonly Brush IslandFill=Live("IslandFill","#ED363636"),IslandEdge=Live("IslandEdge","#25FFFFFF"),IslandHover=Live("IslandHover","#ED424242"),IslandPressed=Live("IslandPressed","#ED303030"),IslandShadow=Live("IslandShadow","#000000");
    static void RefreshIslands(){
      Color card=((SolidColorBrush)Background).Color,text=((SolidColorBrush)Foreground).Color;
      // Islands remain legible at the full transparency range of the outer glass.
      byte alpha=(byte)(255-System.Math.Round(Transparency*.55));
      Color fill=ReadableSurface(Luminance(card)<.4?Mix(card,text,.07):Mix(card,Colors.White,.72),text);
      tokens["IslandFill"].Set(Alpha(fill,alpha));
      tokens["IslandHover"].Set(Alpha(ReadableSurface(Mix(fill,text,.045),text),alpha));
      tokens["IslandPressed"].Set(Alpha(ReadableSurface(Mix(fill,card,.45),text),alpha));
      tokens["IslandEdge"].Set(Alpha(IsDark?Colors.White:Mix(card,text,.3),(byte)(IsDark?24:35)));
      tokens["IslandShadow"].Set(IsDark?Colors.Black:Mix(card,Colors.Black,.65));
    }
    static Color ReadableSurface(Color fill,Color ink){Color limit=Contrast(Colors.Black,ink)>=Contrast(Colors.White,ink)?Colors.Black:Colors.White;for(int i=0;i<24&&Contrast(fill,ink)<4.5;i++)fill=Mix(fill,limit,.12);return fill;}
    public static DropShadowEffect IslandElevation(){
      var shadow=new DropShadowEffect{BlurRadius=12,ShadowDepth=3,Direction=270,Opacity=.24,RenderingBias=RenderingBias.Quality};
      BindingOperations.SetBinding(shadow,DropShadowEffect.ColorProperty,new Binding("Color"){Source=IslandShadow});return shadow;
    }
    public static Border Island(UIElement content,Thickness padding,int radius=IslandRadius){return new Border{Child=content,Padding=padding,CornerRadius=new CornerRadius(radius),Background=IslandFill,BorderBrush=IslandEdge,BorderThickness=new Thickness(1),Effect=IslandElevation()};}
    public static void Raise(Button button){button.Background=IslandFill;button.BorderBrush=IslandEdge;button.Effect=IslandElevation();button.Style=RoundedButtonStyle(false,IslandRadius);}
    public static void StyleScroll(ScrollViewer scroll){
      var style=(Style)System.Windows.Markup.XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'><Setter Property='Width' Value='8'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'><Grid Background='Transparent'><Track Name='PART_Track' IsDirectionReversed='True' Orientation='Vertical'><Track.DecreaseRepeatButton><RepeatButton Command='{x:Static ScrollBar.PageUpCommand}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton><Track.Thumb><Thumb><Thumb.Template><ControlTemplate><Border Background='{DynamicResource IslandScrollInk}' CornerRadius='3' Margin='2'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton Command='{x:Static ScrollBar.PageDownCommand}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton></Track></Grid></ControlTemplate></Setter.Value></Setter></Style>");
      scroll.Resources["IslandScrollInk"]=Line;scroll.Resources[SystemParameters.VerticalScrollBarWidthKey]=8.0;scroll.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)]=style;
    }
    // Groups related controls without replacing them, their bindings or input focus.
    public static Border GroupIsland(Panel panel,int start,int count,Thickness padding){
      var content=new StackPanel();for(int i=0;i<count;i++){var child=panel.Children[start];panel.Children.RemoveAt(start);content.Children.Add(child);}
      var island=Island(content,padding);panel.Children.Insert(start,island);return island;
    }
  }
}
