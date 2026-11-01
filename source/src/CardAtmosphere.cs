using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;


namespace RingTimer {
  // An angular color field rotates behind a fixed rounded stroke mask.
  sealed class CardAtmosphere:Grid {
    readonly Border rim=new Border{BorderThickness=new Thickness(1.4),CornerRadius=new CornerRadius(Theme.MenuRadius-1),Margin=new Thickness(1),IsHitTestVisible=false};
    ImageBrush angular;
    readonly Border field=new Border{CornerRadius=new CornerRadius(Theme.MenuRadius-1),Margin=new Thickness(1),IsHitTestVisible=false};
    readonly RotateTransform rotation=new RotateTransform(45);
    bool moving;
    public CardAtmosphere(){
      IsHitTestVisible=false;Focusable=false;HorizontalAlignment=HorizontalAlignment.Left;VerticalAlignment=VerticalAlignment.Top;
      // The radial mask softens the center; both rounded layers sample exactly
      // the same angular texture in the same coordinate system.
      var diffusion=new RadialGradientBrush{Center=new Point(.5,.5),GradientOrigin=new Point(.5,.5),RadiusX=.7,RadiusY=.7};
      diffusion.GradientStops.Add(new GradientStop(Colors.Transparent,0));
      diffusion.GradientStops.Add(new GradientStop(Color.FromArgb(24,255,255,255),.16));
      diffusion.GradientStops.Add(new GradientStop(Color.FromArgb(155,255,255,255),.50));
      diffusion.GradientStops.Add(new GradientStop(Colors.White,1));diffusion.Freeze();field.OpacityMask=diffusion;
      Children.Add(field);Children.Add(rim);SizeChanged+=(s,e)=>ResizeBorder();RefreshTheme();
    }
    void ResizeBorder(){
      double w=ActualWidth,h=ActualHeight;if(w<8||h<8)return;
      double side=Math.Ceiling(Math.Sqrt(w*w+h*h))+8;rotation.CenterX=(w-2)/2;rotation.CenterY=(h-2)/2;
      if(angular!=null)angular.Viewport=new Rect((w-2-side)/2,(h-2-side)/2,side,side);
    }
    static Color Blend(Color a,Color b,double mix){return Color.FromArgb((byte)(a.A+(b.A-a.A)*mix),(byte)(a.R+(b.R-a.R)*mix),(byte)(a.G+(b.G-a.G)*mix),(byte)(a.B+(b.B-a.B)*mix));}
    static Color Alpha(Color c,byte a){c.A=a;return c;}
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
      field.Opacity=Theme.IsDark?.40:.28;
      angular=new ImageBrush(AngularTexture(ice,amber,surface)){ViewportUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill,Transform=rotation};rim.BorderBrush=angular;field.Background=angular;ResizeBorder();
    }
    public void SetMotion(bool enabled){
      if(moving==enabled)return;moving=enabled;
      rotation.BeginAnimation(RotateTransform.AngleProperty,enabled?new DoubleAnimation(45,405,TimeSpan.FromSeconds(12)){RepeatBehavior=RepeatBehavior.Forever}:null);
    }
  }
}
