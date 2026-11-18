using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace RingTimer {
  // One finite clock per entrance. Live data can change the target without
  // restarting the clock; the final paint uses the current value.
  sealed class MetricEntrance : Animatable {
    static readonly DependencyProperty FractionProperty = DependencyProperty.Register(
      "Fraction", typeof(double), typeof(MetricEntrance),
      new PropertyMetadata(1.0, (d,e) => ((MetricEntrance)d).Paint()));
    readonly Action<double> paint;
    public MetricEntrance(Action<double> paint) { this.paint = paint; }
    protected override Freezable CreateInstanceCore() { return new MetricEntrance(paint); }
    public void Paint() { paint((double)GetValue(FractionProperty)); }
    public void Finish() { BeginAnimation(FractionProperty, null); Paint(); }
    public void Start(bool enabled, int milliseconds, int delay = 0) {
      Finish();
      if (!enabled) return;
      var animation = MicroMotion.Ease(0, 1, milliseconds);
      if (delay > 0) {
        var frames = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(delay))));
        frames.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(delay+milliseconds))) {
          EasingFunction = animation.EasingFunction
        });
        BeginAnimation(FractionProperty, frames);
      } else BeginAnimation(FractionProperty, animation);
    }
    public static bool Enabled(FrameworkElement owner) {
      var menu = Window.GetWindow(owner) as MenuWindow;
      return owner.IsVisible && SystemParameters.ClientAreaAnimation && (menu == null || menu.AnimationsEnabled);
    }
  }
}
