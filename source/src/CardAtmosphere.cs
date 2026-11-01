using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace RingTimer {
  // Retained gradient visuals: animation changes only transforms and opacity,
  // leaving the cached card text and controls untouched.
  sealed class CardAtmosphere:Grid {
    readonly Border rim=new Border{BorderThickness=new Thickness(1.4),CornerRadius=new CornerRadius(Theme.MenuRadius-1),Margin=new Thickness(1)};
    readonly Grid glow=new Grid{Height=146,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,-42)};
    readonly Ellipse cool=new Ellipse{Width=285,Height=132,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(-26,0,0,0)},warm=new Ellipse{Width=245,Height=118,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,-18,0)};
    readonly RotateTransform rotation=new RotateTransform(0,.5,.5);
    readonly ScaleTransform breath=new ScaleTransform(1,1);
    readonly TranslateTransform drift=new TranslateTransform();
    bool moving;
    public CardAtmosphere(){
      IsHitTestVisible=false;Focusable=false;HorizontalAlignment=HorizontalAlignment.Left;VerticalAlignment=VerticalAlignment.Top;
      glow.Children.Add(cool);glow.Children.Add(warm);glow.RenderTransformOrigin=new Point(.5,.7);
      var motion=new TransformGroup();motion.Children.Add(breath);motion.Children.Add(drift);glow.RenderTransform=motion;
      Children.Add(glow);Children.Add(rim);SizeChanged+=(s,e)=>Clip=new RectangleGeometry(new Rect(RenderSize),Theme.MenuRadius,Theme.MenuRadius);RefreshTheme();
    }
    static Color Blend(Color a,Color b,double mix){return Color.FromRgb((byte)(a.R+(b.R-a.R)*mix),(byte)(a.G+(b.G-a.G)*mix),(byte)(a.B+(b.B-a.B)*mix));}
    static Color Alpha(Color c,byte a){c.A=a;return c;}
    static RadialGradientBrush Diffusion(Color c){var brush=new RadialGradientBrush{Center=new Point(.5,.55),GradientOrigin=new Point(.5,.55),RadiusX=.5,RadiusY=.5};brush.GradientStops.Add(new GradientStop(Alpha(c,170),0));brush.GradientStops.Add(new GradientStop(Alpha(c,82),.32));brush.GradientStops.Add(new GradientStop(Alpha(c,20),.68));brush.GradientStops.Add(new GradientStop(Alpha(c,0),1));brush.Freeze();return brush;}
    public void RefreshTheme(){
      Color accent=((SolidColorBrush)Theme.Focus).Color,surface=((SolidColorBrush)Theme.Background).Color;
      Color ice=Blend(accent,Color.FromRgb(117,202,233),.46),amber=Blend(accent,Color.FromRgb(241,171,115),.69);
      ice=Blend(ice,Theme.IsDark?Colors.White:surface,Theme.IsDark?.16:.12);amber=Blend(amber,Theme.IsDark?Colors.White:surface,Theme.IsDark?.12:.10);
      cool.Fill=Diffusion(ice);warm.Fill=Diffusion(amber);cool.Opacity=Theme.IsDark?.50:.29;warm.Opacity=Theme.IsDark?.42:.25;
      var edge=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,1),RelativeTransform=rotation};
      edge.GradientStops.Add(new GradientStop(Alpha(ice,Theme.IsDark?(byte)185:(byte)150),0));
      edge.GradientStops.Add(new GradientStop(Alpha(ice,24),.28));edge.GradientStops.Add(new GradientStop(Alpha(amber,Theme.IsDark?(byte)155:(byte)135),.57));
      edge.GradientStops.Add(new GradientStop(Alpha(amber,20),.80));edge.GradientStops.Add(new GradientStop(Alpha(ice,175),1));rim.BorderBrush=edge;
    }
    static DoubleAnimation Loop(double from,double to,double seconds,bool reverse){return new DoubleAnimation(from,to,TimeSpan.FromSeconds(seconds)){AutoReverse=reverse,RepeatBehavior=RepeatBehavior.Forever,EasingFunction=reverse?new SineEase{EasingMode=EasingMode.EaseInOut}:null};}
    public void SetMotion(bool enabled){
      if(moving==enabled)return;moving=enabled;
      rotation.BeginAnimation(RotateTransform.AngleProperty,enabled?Loop(0,360,18,false):null);
      glow.BeginAnimation(OpacityProperty,enabled?Loop(.64,1,3.4,true):null);
      breath.BeginAnimation(ScaleTransform.ScaleXProperty,enabled?Loop(.92,1.12,3.4,true):null);
      breath.BeginAnimation(ScaleTransform.ScaleYProperty,enabled?Loop(.88,1.16,3.4,true):null);
      drift.BeginAnimation(TranslateTransform.XProperty,enabled?Loop(-12,12,5.2,true):null);
    }
  }
}
