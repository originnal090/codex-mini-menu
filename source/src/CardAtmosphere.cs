using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RingTimer {
  // An angular color field rotates behind a fixed rounded stroke mask.
  sealed class CardAtmosphere:Grid {
    readonly Border rim=new Border{BorderThickness=new Thickness(1.4),CornerRadius=new CornerRadius(Theme.MenuRadius-1),Margin=new Thickness(1),IsHitTestVisible=false};
    ImageBrush angular;
    readonly Grid glow=new Grid{Height=190,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(-20,0,-20,-44),Opacity=.4,CacheMode=new BitmapCache()};
    readonly Ellipse cool=new Ellipse{Width=310,Height=166,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(-20,0,0,0)},warm=new Ellipse{Width=275,Height=150,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,14,-12,0)};
    readonly RotateTransform rotation=new RotateTransform();
    readonly ScaleTransform breath=new ScaleTransform(1,1);
    readonly TranslateTransform drift=new TranslateTransform();
    bool moving;
    public CardAtmosphere(){
      IsHitTestVisible=false;Focusable=false;HorizontalAlignment=HorizontalAlignment.Left;VerticalAlignment=VerticalAlignment.Top;
      glow.Children.Add(cool);glow.Children.Add(warm);glow.RenderTransformOrigin=new Point(.5,.78);
      var motion=new TransformGroup();motion.Children.Add(breath);motion.Children.Add(drift);glow.RenderTransform=motion;
      Children.Add(glow);Children.Add(rim);SizeChanged+=(s,e)=>ResizeBorder();RefreshTheme();
    }
    void ResizeBorder(){
      double w=ActualWidth,h=ActualHeight;if(w<8||h<8)return;
      double side=Math.Ceiling(Math.Sqrt(w*w+h*h))+8;rotation.CenterX=(w-2)/2;rotation.CenterY=(h-2)/2;
      if(angular!=null)angular.Viewport=new Rect((w-2-side)/2,(h-2-side)/2,side,side);
    }
    static Color Blend(Color a,Color b,double mix){return Color.FromArgb((byte)(a.A+(b.A-a.A)*mix),(byte)(a.R+(b.R-a.R)*mix),(byte)(a.G+(b.G-a.G)*mix),(byte)(a.B+(b.B-a.B)*mix));}
    static Color Alpha(Color c,byte a){c.A=a;return c;}
    static RadialGradientBrush Diffusion(Color c){var brush=new RadialGradientBrush{Center=new Point(.5,.55),GradientOrigin=new Point(.5,.55),RadiusX=.5,RadiusY=.5};brush.GradientStops.Add(new GradientStop(Alpha(c,200),0));brush.GradientStops.Add(new GradientStop(Alpha(c,110),.32));brush.GradientStops.Add(new GradientStop(Alpha(c,24),.68));brush.GradientStops.Add(new GradientStop(Alpha(c,0),1));brush.Freeze();return brush;}
    static BitmapSource AngularTexture(Color ice,Color amber,Color baseColor){
      const int size=384;var colors=new[]{Alpha(ice,210),Alpha(Blend(ice,baseColor,.55),75),Alpha(amber,195),Alpha(Blend(amber,baseColor,.65),60),Alpha(ice,210)};var pixels=new byte[size*size*4];
      for(int y=0;y<size;y++)for(int x=0;x<size;x++){
        double angle=(Math.Atan2(y-(size-1)*.5,x-(size-1)*.5)+Math.PI)/(2*Math.PI)*4;int sector=Math.Min(3,(int)angle);double t=angle-sector;t=t*t*(3-2*t);Color c=Blend(colors[sector],colors[sector+1],t);int p=(y*size+x)*4;
        pixels[p]=(byte)(c.B*c.A/255);pixels[p+1]=(byte)(c.G*c.A/255);pixels[p+2]=(byte)(c.R*c.A/255);pixels[p+3]=c.A;
      }
      var bitmap=BitmapSource.Create(size,size,96,96,PixelFormats.Pbgra32,null,pixels,size*4);bitmap.Freeze();return bitmap;
    }
    public void RefreshTheme(){
      Color accent=((SolidColorBrush)Theme.Focus).Color,surface=((SolidColorBrush)Theme.Background).Color;
      Color ice=Blend(accent,Color.FromRgb(110,205,232),.42),amber=Blend(accent,Color.FromRgb(238,162,107),.74);
      ice=Blend(ice,Theme.IsDark?Colors.White:surface,Theme.IsDark?.10:.12);amber=Blend(amber,Theme.IsDark?Colors.White:surface,Theme.IsDark?.08:.10);
      cool.Fill=Diffusion(ice);warm.Fill=Diffusion(amber);cool.Opacity=Theme.IsDark?.82:.50;warm.Opacity=Theme.IsDark?.72:.44;
      angular=new ImageBrush(AngularTexture(ice,amber,surface)){ViewportUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill,Transform=rotation};rim.BorderBrush=angular;ResizeBorder();
    }
    static DoubleAnimation Loop(double from,double to,double seconds,bool reverse){return new DoubleAnimation(from,to,TimeSpan.FromSeconds(seconds)){AutoReverse=reverse,RepeatBehavior=RepeatBehavior.Forever,EasingFunction=reverse?new SineEase{EasingMode=EasingMode.EaseInOut}:null};}
    public void SetMotion(bool enabled){
      if(moving==enabled)return;moving=enabled;
      rotation.BeginAnimation(RotateTransform.AngleProperty,enabled?Loop(0,360,12,false):null);
      glow.BeginAnimation(OpacityProperty,enabled?Loop(.18,.95,1.8,true):null);
      breath.BeginAnimation(ScaleTransform.ScaleXProperty,enabled?Loop(.86,1.16,1.8,true):null);
      breath.BeginAnimation(ScaleTransform.ScaleYProperty,enabled?Loop(.76,1.26,1.8,true):null);
      drift.BeginAnimation(TranslateTransform.XProperty,enabled?Loop(-16,16,3.6,true):null);
    }
  }
}
