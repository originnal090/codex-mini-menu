using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RingTimer {
  // The same camera and transform drive the live visual and the native contour.
  sealed class CardProjection {
    public readonly Size Size;
    public readonly double Pitch,Yaw,Distance,Scale;
    public readonly Matrix3D Matrix;
    public const double FieldOfView=35;
    public CardProjection(Size size,double pitch,double yaw){
      Size=size;Pitch=Clamp(pitch);Yaw=Clamp(yaw);
      Distance=size.Width/(2*Math.Tan(FieldOfView*Math.PI/360));
      var rotation=Matrix3D.Identity;
      rotation.Rotate(new Quaternion(new Vector3D(1,0,0),Pitch));
      rotation.Rotate(new Quaternion(new Vector3D(0,1,0),Yaw));
      // Fit the full rectangular live visual inside the original popup. This
      // also bounds every rounded contour point and all nested island shadows.
      double fit=1;
      foreach(double x in new[]{-size.Width/2,size.Width/2})foreach(double y in new[]{-size.Height/2,size.Height/2}){
        var p=rotation.Transform(new Point3D(x,y,0));
        fit=Math.Min(fit,Distance/(Math.Abs(p.X)*Distance/(size.Width/2)+Math.Max(0,p.Z)));
        fit=Math.Min(fit,Distance/(Math.Abs(p.Y)*Distance/(size.Height/2)+Math.Max(0,p.Z)));
      }
      Scale=Math.Min(1,fit);rotation.Scale(new Vector3D(Scale,Scale,Scale));Matrix=rotation;
    }
    static double Clamp(double angle){return Math.Max(-12,Math.Min(12,angle));}
    public Point Project(Point point){var p=Matrix.Transform(new Point3D(point.X-Size.Width/2,Size.Height/2-point.Y,0));double perspective=Distance/(Distance-p.Z);return new Point(Size.Width/2+p.X*perspective,Size.Height/2-p.Y*perspective);}
    public Point[] Outline(Rect rect,double radius){
      var points=new List<Point>();double r=Math.Min(radius,Math.Min(rect.Width,rect.Height)/2);
      var centers=new[]{new Point(rect.Right-r,rect.Top+r),new Point(rect.Right-r,rect.Bottom-r),new Point(rect.Left+r,rect.Bottom-r),new Point(rect.Left+r,rect.Top+r)};
      for(int corner=0;corner<4;corner++)for(int step=0;step<=12;step++){double a=(-90+corner*90+step*7.5)*Math.PI/180;points.Add(Project(new Point(centers[corner].X+r*Math.Cos(a),centers[corner].Y+r*Math.Sin(a))));}
      return points.ToArray();
    }
  }

  // Translucent neutral reflections lie over the live glass and islands.
  sealed class SilverCardFilm:FrameworkElement {
    double pitch,yaw,lift;
    readonly bool drawBase,drawLight;
    public SilverCardFilm():this(true,true){}
    public SilverCardFilm(bool baseMaterial,bool lighting){drawBase=baseMaterial;drawLight=lighting;IsHitTestVisible=false;Focusable=false;}
    public void SetPose(double x,double y,double amount){pitch=x;yaw=y;lift=amount;InvalidateVisual();}
    static Color Silver(byte alpha,byte value){return Color.FromArgb(alpha,value,value,value);}
    protected override void OnRender(DrawingContext dc){
      double w=ActualWidth,h=ActualHeight;if(w<4||h<4)return;
      var bounds=new Rect(.5,.5,w-1,h-1);double radius=Theme.MenuRadius;
      dc.PushClip(new RectangleGeometry(bounds,radius,radius));
      if(drawBase){
      var metal=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,.85)};
      metal.GradientStops.Add(new GradientStop(Silver(Theme.IsDark?(byte)15:(byte)9,255),0));
      metal.GradientStops.Add(new GradientStop(Silver(0,255),.28));
      metal.GradientStops.Add(new GradientStop(Silver(Theme.IsDark?(byte)3:(byte)10,0),.52));
      metal.GradientStops.Add(new GradientStop(Silver(Theme.IsDark?(byte)18:(byte)12,255),.76));
      metal.GradientStops.Add(new GradientStop(Silver(2,255),1));
      dc.DrawRectangle(metal,null,bounds);
      // Fine, deterministic brushing: no random redraw noise or idle animation.
      var grain=new Pen(new SolidColorBrush(Silver(Theme.IsDark?(byte)4:(byte)5,Theme.IsDark?(byte)255:(byte)0)),.45);
      for(double y=3;y<h;y+=3)dc.DrawLine(grain,new Point(1,y),new Point(w-1,y-.65));
      }
      if(drawLight){
      double center=.36+yaw/38-pitch/70;
      var sweep=new LinearGradientBrush{StartPoint=new Point(0,.1),EndPoint=new Point(1,.8)};
      sweep.GradientStops.Add(new GradientStop(Colors.Transparent,center-.30));
      sweep.GradientStops.Add(new GradientStop(Silver((byte)(7+lift*12),255),center-.13));
      sweep.GradientStops.Add(new GradientStop(Silver((byte)(14+lift*32),255),center));
      sweep.GradientStops.Add(new GradientStop(Silver((byte)(5+lift*14),255),center+.045));
      sweep.GradientStops.Add(new GradientStop(Colors.Transparent,center+.22));
      dc.DrawRectangle(sweep,null,bounds);
      // Fresnel-like rim: more reflection at the edge, subdued over the text.
      var rim=new LinearGradientBrush{StartPoint=new Point(.05+yaw/50,0),EndPoint=new Point(.95,1)};
      rim.GradientStops.Add(new GradientStop(Silver((byte)(110+lift*100),255),0));
      rim.GradientStops.Add(new GradientStop(Silver(25,230),.28));
      rim.GradientStops.Add(new GradientStop(Silver((byte)(60+lift*80),255),.58));
      rim.GradientStops.Add(new GradientStop(Silver(Theme.IsDark?(byte)28:(byte)70,80),1));
      dc.DrawRoundedRectangle(null,new Pen(rim,1.5),new Rect(1,1,w-2,h-2),radius-1,radius-1);
      dc.DrawRoundedRectangle(null,new Pen(new SolidColorBrush(Silver((byte)(12+lift*12),255)),.7),new Rect(3,3,w-6,h-6),radius-3,radius-3);
      }
      dc.Pop();
    }
  }

  sealed class SilverCardPresenter:Grid {
    readonly MenuWindow menu;
    readonly Border surface;
    // A painted transparent full-size background fixes the visual brush bounds;
    // otherwise WPF crops its empty margins and stretches the card texture.
    readonly Grid face=new Grid{Background=Brushes.Transparent,ClipToBounds=true};
    readonly Grid cardBase=new Grid(),lightingFace=new Grid{Background=Brushes.Transparent};
    readonly SilverCardFilm brushing=new SilverCardFilm(true,false),film=new SilverCardFilm(false,true);
    readonly Viewport3D viewport=new Viewport3D{IsHitTestVisible=false};
    readonly Viewport2DVisual3D card=new Viewport2DVisual3D();
    readonly Viewport2DVisual3D lighting=new Viewport2DVisual3D();
    readonly PerspectiveCamera camera=new PerspectiveCamera();readonly MatrixTransform3D pose=new MatrixTransform3D();
    Point origin;double targetPitch,targetYaw,returnPitch,returnYaw,elapsed;
    TimeSpan? lastFrame;
    IInputElement savedFocus;
    bool restoring;
    Bubble floatingLauncher;double launcherOpacity;bool launcherHitTest;Canvas launcherLayer;
    Rect launcherRect=Rect.Empty;
    System.Windows.Shapes.Rectangle launcherCopy;
    Point[] lastContour;Rect lastHole=Rect.Empty;
    public bool IsPresenting{get;private set;}
    public bool IsHeld{get;private set;}
    public bool FrameSubscribed{get;private set;}
    public double Pitch{get;private set;}
    public double Yaw{get;private set;}
    public CardProjection Projection{get;private set;}
    internal FrameworkElement ContentVisual{get{return face;}}
    internal bool IncludesLauncher{get{return floatingLauncher!=null;}}
    public SilverCardPresenter(MenuWindow owner,Border content){
      menu=owner;surface=content;ClipToBounds=true;
      TextOptions.SetTextFormattingMode(face,TextFormattingMode.Display);TextOptions.SetTextRenderingMode(face,TextRenderingMode.Grayscale);
      cardBase.Children.Add(surface);cardBase.Children.Add(brushing);face.Children.Add(cardBase);lightingFace.Children.Add(film);Children.Add(face);Children.Add(lightingFace);lightingFace.IsHitTestVisible=false;
      face.CacheMode=new BitmapCache{EnableClearType=false,SnapsToDevicePixels=true};
      RenderOptions.SetCachingHint(card,CachingHint.Cache);RenderOptions.SetCacheInvalidationThresholdMinimum(card,.5);RenderOptions.SetCacheInvalidationThresholdMaximum(card,2);
      brushing.HorizontalAlignment=HorizontalAlignment.Left;brushing.VerticalAlignment=VerticalAlignment.Top;
      film.HorizontalAlignment=HorizontalAlignment.Left;film.VerticalAlignment=VerticalAlignment.Top;
      var material=new DiffuseMaterial(Brushes.White);Viewport2DVisual3D.SetIsVisualHostMaterial(material,true);card.Material=material;
      var reflectionMaterial=new DiffuseMaterial(Brushes.White);Viewport2DVisual3D.SetIsVisualHostMaterial(reflectionMaterial,true);lighting.Material=reflectionMaterial;
      card.Transform=pose;lighting.Transform=pose;camera.LookDirection=new Vector3D(0,0,-1);camera.UpDirection=new Vector3D(0,1,0);camera.FieldOfView=CardProjection.FieldOfView;camera.NearPlaneDistance=1;camera.FarPlaneDistance=10000;viewport.Camera=camera;
      viewport.Children.Add(new ModelVisual3D{Content=new AmbientLight(Colors.White)});viewport.Children.Add(card);viewport.Children.Add(lighting);
      surface.SizeChanged+=(s,e)=>{if(!IsPresenting)UpdateFilm();};SizeChanged+=(s,e)=>{if(IsPresenting)Reset();UpdateFilm();};
      PreviewMouseRightButtonDown+=(s,e)=>{if(!menu.CanTiltCard||IsPresenting)return;var p=e.GetPosition(surface);if(p.X<0||p.Y<0||p.X>surface.ActualWidth||p.Y>surface.ActualHeight)return;Begin(e.GetPosition(this));if(IsPresenting&&!CaptureMouse())Reset();e.Handled=true;};
      PreviewMouseMove+=(s,e)=>{if(!IsHeld)return;if(e.RightButton==MouseButtonState.Pressed)Move(e.GetPosition(this));else Release();e.Handled=true;};
      PreviewMouseRightButtonUp+=(s,e)=>{if(!IsPresenting)return;Release();e.Handled=true;};
      LostMouseCapture+=(s,e)=>{if(IsHeld&&!restoring)Release();};
      PreviewMouseLeftButtonDown+=(s,e)=>{if(IsPresenting)e.Handled=true;};PreviewMouseLeftButtonUp+=(s,e)=>{if(IsPresenting)e.Handled=true;};PreviewMouseWheel+=(s,e)=>{if(IsPresenting)e.Handled=true;};
      PreviewKeyDown+=(s,e)=>{if(IsPresenting&&e.Key!=Key.Escape)e.Handled=true;};
      menu.Deactivated+=(s,e)=>{if(IsPresenting)Release();};menu.IsVisibleChanged+=(s,e)=>{if(!menu.IsVisible)Reset();};menu.Closed+=(s,e)=>Reset();
      Loaded+=(s,e)=>{Theme.Changed+=ThemeChanged;UpdateFilm();};Unloaded+=(s,e)=>{Theme.Changed-=ThemeChanged;Reset();};
    }
    void ThemeChanged(){film.InvalidateVisual();brushing.InvalidateVisual();}
    void UpdateFilm(){if(surface.ActualWidth<=0)return;var at=surface.TranslatePoint(new Point(),face);foreach(var layer in new[]{brushing,film}){layer.Width=surface.ActualWidth;layer.Height=surface.ActualHeight;layer.Margin=new Thickness(at.X,at.Y,0,0);layer.Clip=surface.Clip;}}
    internal void Begin(Point at){
      if(IsPresenting||ActualWidth<=0||ActualHeight<=0)return;
      savedFocus=Keyboard.FocusedElement;origin=at;Pitch=Yaw=targetPitch=targetYaw=0;elapsed=0;lastFrame=null;
      UpdateFilm();double w=ActualWidth,h=ActualHeight;foreach(var layer in new[]{face,lightingFace}){layer.Width=w;layer.Height=h;layer.Measure(new Size(w,h));layer.Arrange(new Rect(0,0,w,h));}
      var mesh=new MeshGeometry3D{Positions=new Point3DCollection{new Point3D(-w/2,h/2,0),new Point3D(-w/2,-h/2,0),new Point3D(w/2,-h/2,0),new Point3D(w/2,h/2,0)},TextureCoordinates=new PointCollection{new Point(0,0),new Point(0,1),new Point(1,1),new Point(1,0)},TriangleIndices=new Int32Collection{0,1,2,0,2,3}};
      card.Geometry=mesh;lighting.Geometry=mesh;IsPresenting=IsHeld=true;
      IncludeLauncher(w,h);
      Children.Remove(face);Children.Remove(lightingFace);card.Visual=face;lighting.Visual=lightingFace;Children.Add(viewport);Cursor=Cursors.SizeAll;
      camera.Position=new Point3D(0,0,w/(2*Math.Tan(CardProjection.FieldOfView*Math.PI/360)));
      ApplyPose(1);Subscribe();
    }
    void IncludeLauncher(double width,double height){
      var launcher=menu.Launcher;if(launcher==null||!launcher.IsVisible)return;
      var window=menu.PixelBounds;var anchor=launcher.PixelBounds;double dpi=Native.ScaleFor(menu.Handle);
      var rect=new Rect((anchor.Left-window.Left)/dpi,(anchor.Top-window.Top)/dpi,anchor.Width/dpi,anchor.Height/dpi);
      launcherRect=rect;
      var visual=launcher.Content as Visual;if(visual==null)return;
      var brush=new VisualBrush(visual){Stretch=Stretch.Fill};RenderOptions.SetCachingHint(brush,CachingHint.Cache);
      var copy=new System.Windows.Shapes.Rectangle{Width=rect.Width,Height=rect.Height,Fill=brush,IsHitTestVisible=false};
      launcherCopy=copy;
      Canvas.SetLeft(copy,rect.Left);Canvas.SetTop(copy,rect.Top);launcherLayer=new Canvas{IsHitTestVisible=false};launcherLayer.Children.Add(copy);face.Children.Add(launcherLayer);
      cardBase.Clip=new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(0,0,width,height)),new EllipseGeometry(rect));
      floatingLauncher=launcher;launcherOpacity=launcher.Opacity;launcherHitTest=launcher.IsHitTestVisible;launcher.Opacity=0;launcher.IsHitTestVisible=false;
      launcher.LocationChanged+=LauncherMoved;launcher.SizeChanged+=LauncherMoved;launcher.Closed+=LauncherClosed;
    }
    void LauncherMoved(object sender,EventArgs e){if(IsPresenting&&!restoring)UpdateContour();}
    void LauncherClosed(object sender,EventArgs e){Reset();}
    bool SyncLauncher(){
      if(floatingLauncher==null)return false;var window=menu.PixelBounds;var anchor=floatingLauncher.PixelBounds;double dpi=Native.ScaleFor(menu.Handle);
      var rect=new Rect((anchor.Left-window.Left)/dpi,(anchor.Top-window.Top)/dpi,anchor.Width/dpi,anchor.Height/dpi);if(rect==launcherRect)return false;
      launcherRect=rect;launcherCopy.Width=rect.Width;launcherCopy.Height=rect.Height;Canvas.SetLeft(launcherCopy,rect.Left);Canvas.SetTop(launcherCopy,rect.Top);
      cardBase.Clip=new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight)),new EllipseGeometry(rect));return true;
    }
    void RestoreLauncher(){
      if(launcherLayer!=null){face.Children.Remove(launcherLayer);launcherLayer=null;}launcherCopy=null;cardBase.Clip=null;launcherRect=Rect.Empty;
      if(floatingLauncher!=null){floatingLauncher.LocationChanged-=LauncherMoved;floatingLauncher.SizeChanged-=LauncherMoved;floatingLauncher.Closed-=LauncherClosed;floatingLauncher.Opacity=launcherOpacity;floatingLauncher.IsHitTestVisible=launcherHitTest;floatingLauncher=null;}
    }
    internal void Move(Point at){if(!IsHeld)return;targetPitch=Math.Max(-12,Math.Min(12,-(at.Y-origin.Y)/Math.Max(1,ActualHeight/2)*12));targetYaw=Math.Max(-12,Math.Min(12,(at.X-origin.X)/Math.Max(1,ActualWidth/2)*12));Subscribe();}
    internal void Release(){
      if(!IsPresenting||!IsHeld)return;IsHeld=false;returnPitch=Pitch;returnYaw=Yaw;elapsed=0;lastFrame=null;
      if(IsMouseCaptured)ReleaseMouseCapture();
      if(!menu.AnimationsEnabled||!SystemParameters.ClientAreaAnimation){Reset();return;}Subscribe();
    }
    void Subscribe(){if(FrameSubscribed)return;FrameSubscribed=true;lastFrame=null;CompositionTarget.Rendering+=OnFrame;}
    void Unsubscribe(){if(!FrameSubscribed)return;CompositionTarget.Rendering-=OnFrame;FrameSubscribed=false;lastFrame=null;}
    void OnFrame(object sender,EventArgs e){var rendering=(RenderingEventArgs)e;if(lastFrame==rendering.RenderingTime)return;double dt=lastFrame.HasValue?Math.Min(.05,(rendering.RenderingTime-lastFrame.Value).TotalSeconds):1.0/60;lastFrame=rendering.RenderingTime;
      if(IsHeld){double follow=1-Math.Exp(-dt*22);Pitch+=(targetPitch-Pitch)*follow;Yaw+=(targetYaw-Yaw)*follow;ApplyPose(1);if(Math.Abs(targetPitch-Pitch)<.005&&Math.Abs(targetYaw-Yaw)<.005){Pitch=targetPitch;Yaw=targetYaw;ApplyPose(1);Unsubscribe();}}
      else{elapsed+=dt;double t=Math.Min(1,elapsed/.28),left=Math.Pow(1-t,3);Pitch=returnPitch*left;Yaw=returnYaw*left;ApplyPose(1-t);if(t>=1)Reset();}
    }
    void ApplyPose(double lift){
      Projection=new CardProjection(new Size(ActualWidth,ActualHeight),Pitch,Yaw);
      pose.Matrix=Projection.Matrix;film.SetPose(Pitch,Yaw,lift);UpdateContour();
    }
    internal void UpdateContour(){
      if(!IsPresenting||Projection==null)return;
      bool launcherChanged=SyncLauncher();
      var at=surface.TranslatePoint(new Point(),face);var contour=Projection.Outline(new Rect(at,new Size(surface.ActualWidth,surface.ActualHeight)),Theme.MenuRadius);
      double dpi=Native.ScaleFor(menu.Handle);for(int i=0;i<contour.Length;i++)contour[i]=new Point(contour[i].X*dpi,contour[i].Y*dpi);
      var bounds=menu.PixelBounds;var anchor=menu.AnchorBounds==null?null:menu.AnchorBounds();Rect hole=!IncludesLauncher&&anchor.HasValue?new Rect(anchor.Value.Left-bounds.Left,anchor.Value.Top-bounds.Top,anchor.Value.Width,anchor.Value.Height):Rect.Empty;
      bool same=!launcherChanged&&lastContour!=null&&lastContour.Length==contour.Length&&lastHole==hole;
      if(same)for(int i=0;i<contour.Length;i++)if(contour[i]!=lastContour[i]){same=false;break;}
      if(same)return;lastContour=contour;lastHole=hole;
      Point[] extension=IncludesLauncher?Projection.Outline(launcherRect,launcherRect.Width/2):null;
      if(extension!=null)for(int i=0;i<extension.Length;i++)extension[i]=new Point(extension[i].X*dpi,extension[i].Y*dpi);
      GlassBackdrop.ProjectedShape(menu.Handle,contour,hole,extension);
    }
    internal void Reset(){
      if(restoring)return;restoring=true;
      try{Unsubscribe();IsHeld=false;if(IsMouseCaptured)ReleaseMouseCapture();if(!IsPresenting)return;
        card.Visual=null;lighting.Visual=null;Children.Remove(viewport);Children.Add(face);Children.Add(lightingFace);face.Width=face.Height=lightingFace.Width=lightingFace.Height=Double.NaN;
        RestoreLauncher();
        IsPresenting=false;Pitch=Yaw=0;Projection=null;lastContour=null;Cursor=null;film.SetPose(0,0,0);UpdateLayout();UpdateFilm();menu.RestoreCardContour();
        // A return triggered by deactivation must not steal focus back from the
        // window the user just selected. The existing logical focus is retained.
        var focused=savedFocus as UIElement;if(menu.IsActive&&menu.IsVisible&&focused!=null&&focused.IsVisible&&focused.IsEnabled)Keyboard.Focus(savedFocus);savedFocus=null;
      }finally{restoring=false;}
    }
  }
}
