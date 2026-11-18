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
    public CardProjection(Size size,double pitch,double yaw):this(size,new Rect(new Point(),size),pitch,yaw){}
    public CardProjection(Size size,Rect activeBounds,double pitch,double yaw){
      Size=size;Pitch=Clamp(pitch);Yaw=Clamp(yaw);
      Distance=activeBounds.Width/(2*Math.Tan(FieldOfView*Math.PI/360));
      var rotation=Matrix3D.Identity;
      rotation.Rotate(new Quaternion(new Vector3D(1,0,0),Pitch));
      rotation.Rotate(new Quaternion(new Vector3D(0,1,0),Yaw));
      // Fit the visible card, independently of its retained native envelope.
      double fit=1;
      foreach(double x in new[]{-activeBounds.Width/2,activeBounds.Width/2})foreach(double y in new[]{-activeBounds.Height/2,activeBounds.Height/2}){
        var p=rotation.Transform(new Point3D(x,y,0));
        fit=Math.Min(fit,Distance/(Math.Abs(p.X)*Distance/(activeBounds.Width/2)+Math.Max(0,p.Z)));
        fit=Math.Min(fit,Distance/(Math.Abs(p.Y)*Distance/(activeBounds.Height/2)+Math.Max(0,p.Z)));
      }
      Scale=Math.Min(1,fit);rotation.Scale(new Vector3D(Scale,Scale,Scale));
      var offset=new Vector3D(activeBounds.Left+activeBounds.Width/2-size.Width/2,size.Height/2-activeBounds.Top-activeBounds.Height/2,0);
      var matrix=Matrix3D.Identity;matrix.Translate(-offset);matrix.Append(rotation);
      // Compensate perspective about the envelope's camera center. This makes
      // a point project exactly like a tightly sized card plus its local offset,
      // retaining the original tilt geometry without reallocating the window.
      var center=Matrix3D.Identity;center.M31=-offset.X/Distance;center.M32=-offset.Y/Distance;center.OffsetX=offset.X;center.OffsetY=offset.Y;
      matrix.Append(center);Matrix=matrix;
    }
    static double Clamp(double angle){return Math.Max(-12,Math.Min(12,angle));}
    public Point Project(Point point){return Project(point,0);}
    public Point Project(Point point,double elevation){var p=Matrix.Transform(new Point3D(point.X-Size.Width/2,Size.Height/2-point.Y,elevation));double perspective=Distance/(Distance-p.Z);return new Point(Size.Width/2+p.X*perspective,Size.Height/2-p.Y*perspective);}
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
    public void SetPose(double x,double y,double amount){double light=Math.Min(1,(Math.Abs(x)+Math.Abs(y))/25);if(pitch==x&&yaw==y&&lift==light)return;pitch=x;yaw=y;lift=light;InvalidateVisual();}
    internal static double LightPosition(double pitch,double yaw){return .35+yaw*.018-pitch*.004;}
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
      double center=LightPosition(pitch,yaw);
      var sweep=new LinearGradientBrush{StartPoint=new Point(0,.1),EndPoint=new Point(1,.8)};
      sweep.GradientStops.Add(new GradientStop(Colors.Transparent,center-.30));
      sweep.GradientStops.Add(new GradientStop(Silver((byte)(7+lift*4),255),center-.13));
      sweep.GradientStops.Add(new GradientStop(Silver((byte)(24+lift*7),255),center));
      sweep.GradientStops.Add(new GradientStop(Silver((byte)(6+lift*4),255),center+.045));
      sweep.GradientStops.Add(new GradientStop(Colors.Transparent,center+.22));
      dc.DrawRectangle(sweep,null,bounds);
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
    readonly System.Windows.Shapes.Path launcherBacking=new System.Windows.Shapes.Path{Fill=Theme.GlassFill,IsHitTestVisible=false};
    Rect backingRect=Rect.Empty,backingSurface=Rect.Empty;
    readonly SilverCardFilm brushing=new SilverCardFilm(true,false),film=new SilverCardFilm(false,true);
    readonly CardAtmosphere atmosphere=new CardAtmosphere();
    readonly BitmapCache contentCache=new BitmapCache{EnableClearType=false,SnapsToDevicePixels=true};
    // The moving light field has no text or controls. A smaller texture avoids
    // rasterizing a full-size translucent 3D surface on every pointer frame.
    // Live content retains its native-resolution cache; the light cache is used
    // at rest too, so beginning a gesture does not change its rasterization.
    readonly BitmapCache lightCache=new BitmapCache{RenderAtScale=.5};
    readonly ThinFoilRelief relief;

    readonly Viewport3D viewport=new Viewport3D{IsHitTestVisible=false};
    readonly Viewport2DVisual3D card=new Viewport2DVisual3D();
    readonly Viewport2DVisual3D lighting=new Viewport2DVisual3D();
    readonly PerspectiveCamera camera=new PerspectiveCamera();readonly MatrixTransform3D pose=new MatrixTransform3D();
    Point origin;double targetPitch,targetYaw,returnPitch,returnYaw,elapsed;
    int handoffFrames;
    TimeSpan? lastFrame;
    IInputElement savedFocus;
    bool restoring;
    Bubble floatingLauncher;bool launcherHitTest;Canvas launcherLayer;
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
    internal bool IncludesLauncher{get{return IsPresenting&&floatingLauncher!=null;}}
    public SilverCardPresenter(MenuWindow owner,Border content){
      menu=owner;surface=content;ClipToBounds=true;relief=new ThinFoilRelief(surface);
      face.CacheMode=contentCache;
      TextOptions.SetTextFormattingMode(face,TextFormattingMode.Display);TextOptions.SetTextRenderingMode(face,TextRenderingMode.Grayscale);
      cardBase.Children.Add(surface);face.Children.Add(launcherBacking);face.Children.Add(cardBase);face.Children.Add(brushing);lightingFace.Children.Add(atmosphere);lightingFace.Children.Add(relief);lightingFace.Children.Add(film);lightingFace.CacheMode=lightCache;Children.Add(face);Children.Add(lightingFace);lightingFace.IsHitTestVisible=false;
      RenderOptions.SetCachingHint(card,CachingHint.Cache);RenderOptions.SetCacheInvalidationThresholdMinimum(card,.5);RenderOptions.SetCacheInvalidationThresholdMaximum(card,2);
      relief.HorizontalAlignment=HorizontalAlignment.Left;relief.VerticalAlignment=VerticalAlignment.Top;
      brushing.HorizontalAlignment=HorizontalAlignment.Left;brushing.VerticalAlignment=VerticalAlignment.Top;
      film.HorizontalAlignment=HorizontalAlignment.Left;film.VerticalAlignment=VerticalAlignment.Top;
      var material=new DiffuseMaterial(Brushes.White);Viewport2DVisual3D.SetIsVisualHostMaterial(material,true);card.Material=material;
      var reflectionMaterial=new DiffuseMaterial(Brushes.White);Viewport2DVisual3D.SetIsVisualHostMaterial(reflectionMaterial,true);lighting.Material=reflectionMaterial;
      card.Transform=pose;lighting.Transform=pose;camera.LookDirection=new Vector3D(0,0,-1);camera.UpDirection=new Vector3D(0,1,0);camera.FieldOfView=CardProjection.FieldOfView;camera.NearPlaneDistance=1;camera.FarPlaneDistance=10000;viewport.Camera=camera;
      viewport.Children.Add(new ModelVisual3D{Content=new AmbientLight(Colors.White)});viewport.Children.Add(card);viewport.Children.Add(lighting);
      surface.LayoutUpdated+=(s,e)=>{relief.RefreshSurfaces();};
      surface.SizeChanged+=(s,e)=>{if(!IsPresenting)UpdateFilm();};SizeChanged+=(s,e)=>{if(IsPresenting)Reset();UpdateFilm();};
      PreviewMouseRightButtonDown+=(s,e)=>{if(!menu.CanTiltCard||IsPresenting)return;var p=e.GetPosition(surface);if(p.X<0||p.Y<0||p.X>surface.ActualWidth||p.Y>surface.ActualHeight)return;Begin(e.GetPosition(this));if(IsPresenting&&!CaptureMouse())Reset();e.Handled=true;};
      PreviewMouseMove+=(s,e)=>{if(!IsHeld)return;if(e.RightButton==MouseButtonState.Pressed)Move(e.GetPosition(this));else Release();e.Handled=true;};
      PreviewMouseRightButtonUp+=(s,e)=>{if(!IsPresenting)return;Release();e.Handled=true;};
      LostMouseCapture+=(s,e)=>{if(IsHeld&&!restoring)Release();};
      PreviewMouseLeftButtonDown+=(s,e)=>{if(IsPresenting)e.Handled=true;};PreviewMouseLeftButtonUp+=(s,e)=>{if(IsPresenting)e.Handled=true;};PreviewMouseWheel+=(s,e)=>{if(IsPresenting)e.Handled=true;};
      PreviewKeyDown+=(s,e)=>{if(IsPresenting&&e.Key!=Key.Escape)e.Handled=true;};
      menu.Deactivated+=(s,e)=>{if(IsPresenting)Release();};menu.IsVisibleChanged+=(s,e)=>{RefreshAtmosphere();if(!menu.IsVisible)Reset();};menu.Closed+=(s,e)=>{atmosphere.SetMotion(false);Reset();};
      Loaded+=(s,e)=>{Theme.Changed+=ThemeChanged;SystemParameters.StaticPropertyChanged+=SystemMotionChanged;UpdateFilm();};Unloaded+=(s,e)=>{Theme.Changed-=ThemeChanged;SystemParameters.StaticPropertyChanged-=SystemMotionChanged;atmosphere.SetMotion(false);Reset();};
    }
    void SystemMotionChanged(object sender,System.ComponentModel.PropertyChangedEventArgs e){if(e.PropertyName=="ClientAreaAnimation")RefreshAtmosphere();}
    internal void RefreshAtmosphere(){atmosphere.SetMotion(menu.IsVisible&&menu.AnimationsEnabled&&SystemParameters.ClientAreaAnimation);}
    // Keep the completed duration sheet in the same cached face as the page.
    // WPF invalidates it when wheels/theme change; moving the window can reuse
    // it instead of recompositing nested blur and island shadows every frame.
    internal void RefreshContentCache(){face.CacheMode=contentCache;relief.RefreshSurfaces();}
    internal void RefreshRelief(){relief.RefreshSurfaces();}
    internal void SetContentReveal(double opacity,double progress,bool above,double fade){relief.Opacity=opacity;atmosphere.SetReveal(progress,above,fade);}
    internal void UpdateLauncherBacking(Rect rect){
      PositionLauncherCopy(rect);
      var at=surface.TranslatePoint(new Point(),face);var cardRect=new Rect(at,new Size(surface.ActualWidth,surface.ActualHeight));if(rect==backingRect&&cardRect==backingSurface)return;
      backingRect=rect;backingSurface=cardRect;
      if(rect.IsEmpty){launcherBacking.Data=null;return;}
      // Extend the same glass only where a screen-clamped launcher protrudes
      // beyond the card, so their overlapping interior is painted exactly once.
      var outside=new CombinedGeometry(GeometryCombineMode.Exclude,new EllipseGeometry(rect),new RectangleGeometry(cardRect,Theme.MenuRadius,Theme.MenuRadius));outside.Freeze();launcherBacking.Data=outside;
    }
    void ThemeChanged(){relief.InvalidateVisual();film.InvalidateVisual();brushing.InvalidateVisual();atmosphere.RefreshTheme();RefreshAtmosphere();}
    void UpdateFilm(){if(surface.ActualWidth<=0)return;var at=surface.TranslatePoint(new Point(),face);foreach(var layer in new FrameworkElement[]{brushing,film,atmosphere,relief}){layer.Width=surface.ActualWidth;layer.Height=surface.ActualHeight;layer.Margin=new Thickness(at.X,at.Y,0,0);layer.Clip=surface.Clip;}RefreshAtmosphere();}
    internal void Begin(Point at){
      if(menu.IsDurationEditing||IsPresenting||ActualWidth<=0||ActualHeight<=0||surface.ActualWidth<=0||surface.ActualHeight<=0)return;
      SetDockedLauncher(true);savedFocus=Keyboard.FocusedElement;origin=at;Pitch=Yaw=targetPitch=targetYaw=0;elapsed=0;handoffFrames=0;lastFrame=null;
      face.CacheMode=contentCache;
      UpdateFilm();double w=ActualWidth,h=ActualHeight;foreach(var layer in new[]{face,lightingFace}){layer.Width=w;layer.Height=h;layer.Measure(new Size(w,h));layer.Arrange(new Rect(0,0,w,h));}
      var mesh=new MeshGeometry3D{Positions=new Point3DCollection{new Point3D(-w/2,h/2,0),new Point3D(-w/2,-h/2,0),new Point3D(w/2,-h/2,0),new Point3D(w/2,h/2,0)},TextureCoordinates=new PointCollection{new Point(0,0),new Point(0,1),new Point(1,1),new Point(1,0)},TriangleIndices=new Int32Collection{0,1,2,0,2,3}};
      card.Geometry=mesh;lighting.Geometry=mesh;IsPresenting=IsHeld=true;
      IncludeLauncher(w,h);
      lightingFace.CacheMode=lightCache;
      relief.RefreshSurfaces();
      Children.Remove(face);Children.Remove(lightingFace);card.Visual=face;lighting.Visual=lightingFace;Children.Add(viewport);Cursor=Cursors.SizeAll;
      camera.Position=new Point3D(0,0,w/(2*Math.Tan(CardProjection.FieldOfView*Math.PI/360)));
      ApplyPose();Subscribe();
    }
    internal void SetDockedLauncher(bool visible){
      var launcher=visible?menu.Launcher:null;
      if(launcher!=null&&!launcher.IsVisible)launcher=null;
      if(floatingLauncher==launcher){if(launcher!=null)SyncLauncher();return;}
      if(IsPresenting)Reset();
      if(floatingLauncher!=null){
        floatingLauncher.LocationChanged-=LauncherMoved;floatingLauncher.SizeChanged-=LauncherMoved;floatingLauncher.Closed-=LauncherClosed;
        floatingLauncher.SetVisualInCard(false);floatingLauncher=null;
      }
      if(launcherLayer!=null){face.Children.Remove(launcherLayer);launcherLayer=null;}launcherCopy=null;launcherRect=Rect.Empty;
      if(launcher==null)return;
      floatingLauncher=launcher;
      var brush=new VisualBrush(launcher.ProjectionVisual){Stretch=Stretch.Fill};RenderOptions.SetCachingHint(brush,CachingHint.Cache);
      launcherCopy=new System.Windows.Shapes.Rectangle{Fill=brush,IsHitTestVisible=false};
      launcherLayer=new Canvas{IsHitTestVisible=false};launcherLayer.Children.Add(launcherCopy);
      // The ink sits over the glass fill and under its brushing, color field and
      // reflection at rest and in perspective. A fully opaque fill cannot erase it.
      face.Children.Insert(face.Children.IndexOf(brushing),launcherLayer);
      launcher.LocationChanged+=LauncherMoved;launcher.SizeChanged+=LauncherMoved;launcher.Closed+=LauncherClosed;
      SyncLauncher();launcher.SetVisualInCard(true);
    }
    void IncludeLauncher(double width,double height){
      if(floatingLauncher==null)return;
      // The persistent live visual stays in the same material stack. Only the
      // native hit surface is suppressed while the whole card is projected.
      launcherHitTest=floatingLauncher.IsHitTestVisible;floatingLauncher.SuppressForProjection(true);floatingLauncher.IsHitTestVisible=false;
    }
    void LauncherMoved(object sender,EventArgs e){if(restoring)return;if(IsPresenting)UpdateContour();else SyncLauncher();}
    void LauncherClosed(object sender,EventArgs e){SetDockedLauncher(false);}
    bool SyncLauncher(){
      if(floatingLauncher==null)return false;var window=menu.PixelBounds;var anchor=floatingLauncher.PixelBounds;double dpi=Native.ScaleFor(menu.Handle);
      var rect=new Rect((anchor.Left-window.Left)/dpi,(anchor.Top-window.Top)/dpi,anchor.Width/dpi,anchor.Height/dpi);if(rect==launcherRect)return false;
      launcherRect=rect;PositionLauncherCopy(rect);UpdateLauncherBacking(rect);return true;
    }
    void PositionLauncherCopy(Rect rect){
      if(launcherCopy==null||rect.IsEmpty)return;launcherRect=rect;launcherCopy.Width=rect.Width;launcherCopy.Height=rect.Height;Canvas.SetLeft(launcherCopy,rect.Left);Canvas.SetTop(launcherCopy,rect.Top);
    }
    void RestoreLauncher(){
      if(floatingLauncher!=null){floatingLauncher.SuppressForProjection(false);floatingLauncher.IsHitTestVisible=launcherHitTest;}
    }
    internal void Move(Point at){if(!IsHeld)return;targetPitch=Math.Max(-12,Math.Min(12,-(at.Y-origin.Y)/Math.Max(1,(surface.ActualHeight+16)/2)*12));targetYaw=Math.Max(-12,Math.Min(12,(at.X-origin.X)/Math.Max(1,(surface.ActualWidth+16)/2)*12));Subscribe();}
    internal void Release(){
      if(!IsPresenting||!IsHeld)return;IsHeld=false;returnPitch=Pitch;returnYaw=Yaw;elapsed=0;lastFrame=null;
      if(IsMouseCaptured)ReleaseMouseCapture();
      if(!menu.AnimationsEnabled||!SystemParameters.ClientAreaAnimation){Reset();return;}Subscribe();
    }
    void Subscribe(){if(FrameSubscribed)return;FrameSubscribed=true;lastFrame=null;CompositionTarget.Rendering+=OnFrame;}
    void Unsubscribe(){if(!FrameSubscribed)return;CompositionTarget.Rendering-=OnFrame;FrameSubscribed=false;lastFrame=null;}
    void OnFrame(object sender,EventArgs e){var rendering=(RenderingEventArgs)e;if(lastFrame==rendering.RenderingTime)return;double dt=lastFrame.HasValue?Math.Min(.05,(rendering.RenderingTime-lastFrame.Value).TotalSeconds):1.0/60;lastFrame=rendering.RenderingTime;
      if(handoffFrames>0){if(--handoffFrames==0)Reset();return;}
      if(IsHeld){double follow=1-Math.Exp(-dt*22);Pitch+=(targetPitch-Pitch)*follow;Yaw+=(targetYaw-Yaw)*follow;ApplyPose();if(Math.Abs(targetPitch-Pitch)<.005&&Math.Abs(targetYaw-Yaw)<.005){Pitch=targetPitch;Yaw=targetYaw;ApplyPose();Unsubscribe();}}
      else{elapsed+=dt;double t=Math.Min(1,elapsed/.28),left=Math.Pow(1-t,3);Pitch=returnPitch*left;Yaw=returnYaw*left;ApplyPose();if(t>=1){if(floatingLauncher!=null)handoffFrames=2;else Reset();}}
    }
    void ApplyPose(){
      var canvas=new Size(ActualWidth,ActualHeight);var at=surface.TranslatePoint(new Point(),face);var active=new Rect(at,surface.RenderSize);active.Inflate(8,8);active.Intersect(new Rect(new Point(),canvas));
      Projection=new CardProjection(canvas,active,Pitch,Yaw);
      camera.Position=new Point3D(0,0,Projection.Distance);camera.FieldOfView=Math.Atan(ActualWidth/(2*Projection.Distance))*360/Math.PI;
      pose.Matrix=Projection.Matrix;relief.SetPose(Pitch,Yaw);film.SetPose(Pitch,Yaw,0);UpdateContour();
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
      try{Unsubscribe();handoffFrames=0;IsHeld=false;if(IsMouseCaptured)ReleaseMouseCapture();if(!IsPresenting)return;
        relief.SetPose(0,0);card.Visual=null;lighting.Visual=null;Children.Remove(viewport);Children.Add(face);Children.Add(lightingFace);face.Width=face.Height=lightingFace.Width=lightingFace.Height=Double.NaN;
        RestoreLauncher();
        IsPresenting=false;Pitch=Yaw=0;Projection=null;lastContour=null;Cursor=null;film.SetPose(0,0,0);RefreshContentCache();UpdateLayout();UpdateFilm();menu.RestoreCardContour();
        // A return triggered by deactivation must not steal focus back from the
        // window the user just selected. The existing logical focus is retained.
        var focused=savedFocus as UIElement;if(menu.IsActive&&menu.IsVisible&&focused!=null&&focused.IsVisible&&focused.IsEnabled)Keyboard.Focus(savedFocus);savedFocus=null;
      }finally{restoring=false;}
    }
  }
}
