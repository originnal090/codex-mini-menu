using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace RingTimer {
  sealed partial class TimerPanel {
    static readonly DependencyProperty EditorProgressProperty=DependencyProperty.Register("EditorProgress",typeof(double),typeof(TimerPanel),new PropertyMetadata(0.0,(d,e)=>((TimerPanel)d).PaintEditorMotion()));
    BlurEffect editorBlur;Effect previousPageEffect;CacheMode previousPageCache;
    ScaleTransform editorScale;TranslateTransform editorShift;Button editorSource;
    double sourceScaleX,sourceScaleY,sourceShiftX,sourceShiftY;
    int editorEpoch;bool editorClosing;
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
      editorBlur=new BlurEffect{Radius=0,KernelType=KernelType.Gaussian,RenderingBias=RenderingBias.Performance};
      page.CacheMode=new BitmapCache();page.Effect=editorBlur;page.IsHitTestVisible=false;
      KeyboardNavigation.SetTabNavigation(page,KeyboardNavigationMode.None);
      EditorProgress=0;PaintEditorMotion();AnimateEditor(1,380,null);
    }
    void PaintEditorMotion(){
      if(editor==null||editorScale==null||editorBlur==null)return;
      double p=Math.Max(0,Math.Min(1,EditorProgress)),remaining=1-p;
      editorScale.ScaleX=1+(sourceScaleX-1)*remaining;editorScale.ScaleY=1+(sourceScaleY-1)*remaining;
      editorShift.X=sourceShiftX*remaining;editorShift.Y=sourceShiftY*remaining;
      editorBlur.Radius=14*p;editorShade.Opacity=p;
      editor.Child.Opacity=Math.Max(0,Math.Min(1,(p-.18)/.64));
      editor.Child.IsHitTestVisible=!editorClosing&&p>.8;
      editorSource.Opacity=remaining;
      var shadow=editor.Effect as DropShadowEffect;
      if(shadow!=null){shadow.BlurRadius=12+12*p;shadow.ShadowDepth=3+5*p;shadow.Opacity=.12+.18*p;}
    }
    void AnimateEditor(double target,int duration,Action completed){
      double from=EditorProgress;int epoch=++editorEpoch;
      BeginAnimation(EditorProgressProperty,null);EditorProgress=from;PaintEditorMotion();
      if(!EditorAnimations||Math.Abs(target-from)<.001){EditorProgress=target;PaintEditorMotion();if(completed!=null)completed();return;}
      var animation=MicroMotion.Ease(from,target,Math.Max(80,(int)(duration*Math.Abs(target-from))));
      animation.Completed+=(s,e)=>{if(epoch!=editorEpoch||editor==null)return;BeginAnimation(EditorProgressProperty,null);EditorProgress=target;PaintEditorMotion();if(completed!=null)completed();};
      BeginAnimation(EditorProgressProperty,animation);
    }
    void ClearEditor(bool restoreFocus){
      if(editor==null)return;
      ++editorEpoch;BeginAnimation(EditorProgressProperty,null);
      var source=editorSource;
      root.Children.Remove(editor);root.Children.Remove(editorShade);
      page.Effect=previousPageEffect;page.CacheMode=previousPageCache;page.IsHitTestVisible=true;
      KeyboardNavigation.SetTabNavigation(page,KeyboardNavigationMode.Continue);
      if(source!=null)source.Opacity=1;
      editor=null;editorShade=null;editorSource=null;editorScale=null;editorShift=null;editorBlur=null;wheels=null;
      previousPageEffect=null;previousPageCache=null;editorClosing=false;EditorProgress=0;
      if(restoreFocus){if(ContentChanged!=null)ContentChanged();if(IsVisible&&source!=null)source.Focus();}
    }
  }
}
