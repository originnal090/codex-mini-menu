using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RingTimer {
  // All surfaces sample one light direction in card coordinates. Only light is
  // redrawn as the card turns; the controls retain their live cached hierarchy.
  sealed class ThinFoilRelief:FrameworkElement {
    struct Surface {public Rect Bounds,Clip;public double Radius,Opacity;}
    readonly FrameworkElement root;
    List<Surface> surfaces=new List<Surface>();
    double pitch,yaw;
    public ThinFoilRelief(FrameworkElement content){root=content;IsHitTestVisible=false;Focusable=false;}
    public void SetPose(double x,double y){if(pitch==x&&yaw==y)return;pitch=x;yaw=y;InvalidateVisual();}
    public void RefreshSurfaces(){
      var next=new List<Surface>();
      Collect(root,new Rect(0,0,root.ActualWidth,root.ActualHeight),1,next);
      bool changed=next.Count!=surfaces.Count;
      if(!changed)for(int i=0;i<next.Count;i++)if(next[i].Bounds!=surfaces[i].Bounds||next[i].Clip!=surfaces[i].Clip||next[i].Radius!=surfaces[i].Radius||next[i].Opacity!=surfaces[i].Opacity){changed=true;break;}
      if(changed){surfaces=next;InvalidateVisual();}
    }
    void Collect(FrameworkElement element,Rect clip,double opacity,List<Surface> result){
      // The presenter fades this overlay with the top-level content group.
      // Keep its surfaces available during that group's brief defocus and
      // apply the group opacity only once, through the overlay itself.
      var card=root as Border;bool contentLayer=card!=null&&element==card.Child;
      if(element.Visibility!=Visibility.Visible||(!contentLayer&&(element.Opacity<=0||element.Effect is System.Windows.Media.Effects.BlurEffect))||element.ActualWidth<1||element.ActualHeight<1)return;
      if(!contentLayer)opacity*=element.Opacity;
      var bounds=element==root?new Rect(element.RenderSize):element.TransformToAncestor(root).TransformBounds(new Rect(element.RenderSize));
      if(element.ClipToBounds||element is ScrollViewer){clip.Intersect(bounds);if(clip.IsEmpty)return;}
      bool component=element is ButtonBase||element is Slider||element is TextBox||element is ComboBox||element is SlidingRail;
      if(element!=root&&((double)element.GetValue(IslandDepthLayers.ElevationProperty)>0||element is ButtonBase)){
        var border=element as Border;var control=element as Control;
        if(border==null&&control!=null&&control.Template!=null)border=control.Template.FindName("surface",control) as Border??control.Template.FindName("frame",control) as Border;
        // A bare icon has no material face to catch this light.
        if(border!=null)result.Add(new Surface{Bounds=bounds,Clip=clip,Opacity=opacity,Radius=Math.Min(border.CornerRadius.TopLeft,Math.Min(bounds.Width,bounds.Height)/2)});
      }
      if(component)return;
      for(int i=0;i<VisualTreeHelper.GetChildrenCount(element);i++){var child=VisualTreeHelper.GetChild(element,i) as FrameworkElement;if(child!=null)Collect(child,clip,opacity,result);}
    }
    protected override void OnRender(DrawingContext dc){
      if(ActualWidth<1||ActualHeight<1)return;
      // This is the same view-dependent light used by the full-card film.
      double lightX=ActualWidth*SilverCardFilm.LightPosition(pitch,yaw);
      double angle=(120+yaw*2-pitch*.6)*Math.PI/180;
      var direction=new Vector(Math.Cos(angle),Math.Sin(angle));
      double intensity=Math.Min(1,(Math.Abs(pitch)+Math.Abs(yaw))/25);
      foreach(var surface in surfaces){
        var bounds=surface.Bounds;
        dc.PushOpacity(surface.Opacity);
        dc.PushClip(new RectangleGeometry(surface.Clip));
        dc.PushClip(new RectangleGeometry(bounds,surface.Radius,surface.Radius));
        double center=(lightX-bounds.Left)/Math.Max(1,bounds.Width);
        var sheen=new RadialGradientBrush{Center=new Point(center,-.30),GradientOrigin=new Point(center,-.30),RadiusX=.75,RadiusY=1.7};
        sheen.GradientStops.Add(new GradientStop(Color.FromArgb(Theme.IsDark?(byte)31:(byte)25,255,255,255),0));
        sheen.GradientStops.Add(new GradientStop(Colors.Transparent,.76));sheen.Freeze();dc.DrawRectangle(sheen,null,bounds);
        dc.Pop();
        var edge=new LinearGradientBrush{StartPoint=new Point(.5-direction.X*.5,.5-direction.Y*.5),EndPoint=new Point(.5+direction.X*.5,.5+direction.Y*.5)};
        edge.GradientStops.Add(new GradientStop(Colors.Transparent,.12));
        edge.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(28+intensity*21),255,255,255),.38));
        edge.GradientStops.Add(new GradientStop(Colors.Transparent,.63));
        edge.GradientStops.Add(new GradientStop(Color.FromArgb(Theme.IsDark?(byte)12:(byte)16,0,0,0),1));edge.Freeze();
        var inset=bounds;inset.Inflate(-.5,-.5);if(inset.Width>0&&inset.Height>0)dc.DrawRoundedRectangle(null,new Pen(edge,.7),inset,surface.Radius,surface.Radius);
        dc.Pop();dc.Pop();
      }
    }
  }
}
