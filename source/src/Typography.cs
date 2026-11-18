using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace RingTimer {
 // Use the faces actually installed for Chinese; avoid synthetic intermediate weights.
 // Body and auxiliary text stay regular, while headings and pale action surfaces
 // receive emphasis. Surface brightness matters independently of the menu theme.
 static class ThemeTypography {
  static readonly FontWeight regular=Available(Theme.ChineseFont,FontWeights.Normal),emphasis=Available(Theme.ChineseFont,FontWeights.Bold);
  static readonly SurfaceWeight converter=new SurfaceWeight();
  static FontWeight Available(FontFamily family,FontWeight requested){
   FontWeight best=FontWeights.Normal;int distance=int.MaxValue;
   foreach(var face in family.GetTypefaces()){
    GlyphTypeface glyph;if(face.Style!=FontStyles.Normal||!face.TryGetGlyphTypeface(out glyph))continue;
    int delta=Math.Abs(face.Weight.ToOpenTypeWeight()-requested.ToOpenTypeWeight());if(delta<distance){best=face.Weight;distance=delta;}
   }
   return best;
  }
  internal static void Button(Button button){
   var binding=new MultiBinding{Converter=converter,Mode=BindingMode.OneWay};
   binding.Bindings.Add(new Binding("Background.Color"){Source=button});
   binding.Bindings.Add(new Binding("Color"){Source=Theme.Background});
   BindingOperations.SetBinding(button,Control.FontWeightProperty,binding);
  }
  internal static void Text(TextBlock text){
   // A style leaves explicit role weights intact and allows body labels inside
   // action buttons to inherit the button's surface-aware weight.
   if(text.FontSize>=17&&text.FontSize<=24&&!String.IsNullOrEmpty(text.Text)){var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.FontWeightProperty,new Binding("FontFamily"){RelativeSource=new RelativeSource(RelativeSourceMode.Self),Converter=new HeadingWeight()}));text.Style=style;}
  }
  sealed class HeadingWeight:IValueConverter {
   public object Convert(object value,Type target,object parameter,CultureInfo culture){return Object.Equals(value,Theme.NumberFont)?Available(Theme.NumberFont,FontWeights.Normal):emphasis;}
   public object ConvertBack(object value,Type target,object parameter,CultureInfo culture){throw new NotSupportedException();}
  }
  sealed class SurfaceWeight:IMultiValueConverter {
   public object Convert(object[] values,Type target,object parameter,CultureInfo culture){
    Color under=values.Length>1&&values[1] is Color?(Color)values[1]:Colors.Black;
    Color surface=values.Length>0&&values[0] is Color?(Color)values[0]:under;
    double alpha=surface.A/255.0;
    double brightness=(.2126*(surface.R*alpha+under.R*(1-alpha))+.7152*(surface.G*alpha+under.G*(1-alpha))+.0722*(surface.B*alpha+under.B*(1-alpha)))/255.0;
    return brightness>.6?emphasis:regular;
   }
   public object[] ConvertBack(object value,Type[] targets,object parameter,CultureInfo culture){throw new NotSupportedException();}
  }
 }
}
