using System.Windows;
namespace RingTimer {
  // Surface roles are shared by existing controls. Relief is painted in place by
  // ThinFoilRelief; controls never leave their layout or inherit a separate plane.
  static class IslandDepthLayers {
    public static readonly DependencyProperty ElevationProperty=DependencyProperty.RegisterAttached("Elevation",typeof(double),typeof(IslandDepthLayers),new PropertyMetadata(0.0));
    public static void SetElevation(DependencyObject item,double value){item.SetValue(ElevationProperty,value);}
  }
}
