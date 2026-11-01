using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace RingTimer {
  sealed partial class TimerPanel {
    static readonly DependencyProperty EditorProgressProperty=DependencyProperty.Register("EditorProgress",typeof(double),typeof(TimerPanel),new PropertyMetadata(0.0));
    BlurEffect editorBlur;Effect previousPageEffect;CacheMode previousPageCache;
    Image editorBackdrop;
    ScaleTransform editorScale;TranslateTransform editorShift;Button editorSource;
    double sourceScaleX,sourceScaleY,sourceShiftX,sourceShiftY;
    int editorEpoch;bool editorClosing,editorThemeDirty;
    double EditorProgress {get{return (double)GetValue(EditorProgressProperty);}set{SetValue(EditorProgressProperty,value);}}

    static Brush EditorGlass(){
      var glass=new SolidColorBrush{Opacity=.82};
      BindingOperations.SetBinding(glass,SolidColorBrush.ColorProperty,new Binding("Color"){Source=Theme.Background});
      return glass;
    }
    bool EditorAnimations {
      get {var menu=Window.GetWindow(this) as MenuWindow;return IsVisible&&SystemParameters.ClientAreaAnimation&&(menu==null||menu.AnimationsEnabled);}
    }
    void BeginEditorMotion(Button source){
      editorSource=source;editorClosing=false;
      var host=Window.GetWindow(this) as MenuWindow;if(host!=null)host.CardPresenter.RefreshContentCache();
      var visible=new Rect(root.RenderSize);
      for(DependencyObject parent=VisualTreeHelper.GetParent(this);parent!=null;parent=VisualTreeHelper.GetParent(parent)){
        var viewport=parent as ScrollContentPresenter;if(viewport==null)continue;
        visible.Intersect(new Rect(viewport.TranslatePoint(new Point(),root),viewport.RenderSize));break;
      }
      if(!visible.IsEmpty){editor.MaxHeight=Math.Max(1,visible.Height);editor.Margin=new Thickness(0,0,0,Math.Max(0,root.ActualHeight-visible.Bottom));root.UpdateLayout();}
      var origin=source.TransformToAncestor(root).TransformBounds(new Rect(source.RenderSize));
      var destination=editor.TransformToAncestor(root).TransformBounds(new Rect(editor.RenderSize));
      sourceScaleX=origin.Width/Math.Max(1,destination.Width);sourceScaleY=origin.Height/Math.Max(1,destination.Height);
      sourceShiftX=origin.Left+origin.Width/2-destination.Left-destination.Width/2;
      sourceShiftY=origin.Top+origin.Height/2-destination.Top-destination.Height/2;
      editorScale=new ScaleTransform();editorShift=new TranslateTransform();
      var transform=new TransformGroup();transform.Children.Add(editorScale);transform.Children.Add(editorShift);
      editor.RenderTransformOrigin=new Point(.5,.5);editor.RenderTransform=transform;
      previousPageEffect=page.Effect;previousPageCache=page.CacheMode;
      // The disabled timer page cannot change duration during editing. Capture
      // it once and crossfade a cached, fully blurred layer; varying Blur.Radius
      // was filtering and uploading the whole page again on every lift frame.
      // This layer is intentionally defocused: half-DIP resolution avoids
      // allocating a full monitor-DPI texture for details the blur removes.
      editorBlur=new BlurEffect{Radius=14,KernelType=KernelType.Gaussian,RenderingBias=RenderingBias.Performance};
      editorBackdrop=new Image{Source=CaptureEditorBackdrop(),Stretch=Stretch.Fill,Effect=editorBlur,Opacity=0,IsHitTestVisible=false,CacheMode=new BitmapCache()};
      root.Children.Insert(1,editorBackdrop);page.CacheMode=new BitmapCache();page.IsHitTestVisible=false;
      Theme.Changed+=EditorThemeChanged;
      KeyboardNavigation.SetTabNavigation(page,KeyboardNavigationMode.None);
      // Rasterize the wheel glyphs and nested island shadows once while the
      // floating shell scales. Opacity/transform then reuse that texture.
      editor.CacheMode=new BitmapCache{EnableClearType=false,SnapsToDevicePixels=true};
      EditorProgress=0;PaintEditorMotion();AnimateEditor(1,190,()=>{if(editor!=null){editor.CacheMode=null;if(editorThemeDirty)RefreshEditorBackdrop();}});
    }
    BitmapSource CaptureEditorBackdrop(){var snapshot=new RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(page.ActualWidth*.5)),Math.Max(1,(int)Math.Ceiling(page.ActualHeight*.5)),48,48,PixelFormats.Pbgra32);snapshot.Render(page);snapshot.Freeze();return snapshot;}
    void EditorThemeChanged(){editorThemeDirty=true;if(editor!=null&&!editorClosing&&EditorProgress>=.999)RefreshEditorBackdrop();}
    void RefreshEditorBackdrop(){
      if(editorBackdrop==null)return;editorThemeDirty=false;
      page.BeginAnimation(OpacityProperty,null);editorSource.BeginAnimation(OpacityProperty,null);
      double opacity=page.Opacity,sourceOpacity=editorSource.Opacity;page.Opacity=editorSource.Opacity=1;
      try{editorBackdrop.Source=CaptureEditorBackdrop();}finally{page.Opacity=opacity;editorSource.Opacity=sourceOpacity;}
    }
    void PaintEditorMotion(){
      if(editor==null||editorScale==null||editorBlur==null)return;
      double p=Math.Max(0,Math.Min(1,EditorProgress)),remaining=1-p;
      editorScale.ScaleX=1+(sourceScaleX-1)*remaining;editorScale.ScaleY=1+(sourceScaleY-1)*remaining;
      editorShift.X=sourceShiftX*remaining;editorShift.Y=sourceShiftY*remaining;
      editorBackdrop.Opacity=p;page.Opacity=1-p;editorShade.Opacity=p;
      editor.Child.Opacity=1;editor.Opacity=p;
      editor.Child.IsHitTestVisible=!editorClosing&&p>.8;
      editorSource.Opacity=remaining;
    }
    void AnimateEditor(double target,int duration,Action completed){
      double from=EditorProgress;int epoch=++editorEpoch;
      if(editor!=null)editor.CacheMode=new BitmapCache{EnableClearType=false,SnapsToDevicePixels=true};
      BeginAnimation(EditorProgressProperty,null);EditorProgress=from;
      if(!EditorAnimations||Math.Abs(target-from)<.001){EditorProgress=target;PaintEditorMotion();if(completed!=null)completed();return;}
      int milliseconds=Math.Max(80,(int)(duration*Math.Abs(target-from)));
      // Animate composition properties directly. A progress-property callback
      // setting nine visual properties on every frame forced UI-thread redraws.
      AnimateEditorValue(editorScale,ScaleTransform.ScaleXProperty,1+(sourceScaleX-1)*(1-target),milliseconds);
      AnimateEditorValue(editorScale,ScaleTransform.ScaleYProperty,1+(sourceScaleY-1)*(1-target),milliseconds);
      AnimateEditorValue(editorShift,TranslateTransform.XProperty,sourceShiftX*(1-target),milliseconds);
      AnimateEditorValue(editorShift,TranslateTransform.YProperty,sourceShiftY*(1-target),milliseconds);
      AnimateEditorValue(editorBackdrop,OpacityProperty,target,milliseconds);
      AnimateEditorValue(page,OpacityProperty,1-target,milliseconds);
      AnimateEditorValue(editorShade,OpacityProperty,target,milliseconds);
      AnimateEditorValue(editor,OpacityProperty,target,milliseconds);
      AnimateEditorValue(editorSource,OpacityProperty,1-target,milliseconds);
      editor.Child.IsHitTestVisible=false;
      var animation=MicroMotion.Ease(from,target,milliseconds);
      animation.Completed+=(s,e)=>{if(epoch!=editorEpoch||editor==null)return;BeginAnimation(EditorProgressProperty,null);EditorProgress=target;PaintEditorMotion();if(completed!=null)completed();};
      BeginAnimation(EditorProgressProperty,animation);
    }
    static void AnimateEditorValue(DependencyObject visual,DependencyProperty property,double target,int milliseconds){
      double from=(double)visual.GetValue(property);var animated=(IAnimatable)visual;
      animated.BeginAnimation(property,null);visual.SetValue(property,from);animated.BeginAnimation(property,MicroMotion.Ease(from,target,milliseconds));
    }
    void ClearEditor(bool restoreFocus){
      if(editor==null)return;
      ++editorEpoch;BeginAnimation(EditorProgressProperty,null);Theme.Changed-=EditorThemeChanged;editorThemeDirty=false;
      var source=editorSource;
      root.Children.Remove(editor);root.Children.Remove(editorShade);root.Children.Remove(editorBackdrop);
      page.BeginAnimation(OpacityProperty,null);page.Effect=previousPageEffect;page.CacheMode=previousPageCache;page.Opacity=1;page.IsHitTestVisible=true;
      KeyboardNavigation.SetTabNavigation(page,KeyboardNavigationMode.Continue);
      if(source!=null){source.BeginAnimation(OpacityProperty,null);source.Opacity=1;}
      editor=null;editorShade=null;editorSource=null;editorScale=null;editorShift=null;editorBlur=null;editorBackdrop=null;wheels=null;
      previousPageEffect=null;previousPageCache=null;editorClosing=false;EditorProgress=0;
      var host=Window.GetWindow(this) as MenuWindow;if(host!=null)host.CardPresenter.RefreshContentCache();
      if(restoreFocus){if(ContentChanged!=null)ContentChanged();if(IsVisible&&source!=null)source.Focus();}
    }
  }
}
