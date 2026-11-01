using System;
namespace RingTimer {
  sealed partial class MenuWindow {
    // Keep the glass pixels beneath the launcher, but route native input to its
    // independent window on this same UI thread, irrespective of activation order.
    IntPtr LauncherHitTest(IntPtr window,int message,IntPtr w,IntPtr l,ref bool handled){
      if(message!=0x84||Launcher==null||!Launcher.IsVisible||!Launcher.IsHitTestVisible||CardPresenter.IsPresenting||IsClosing)return IntPtr.Zero;
      long coordinates=l.ToInt64();int x=(short)(coordinates&0xffff),y=(short)((coordinates>>16)&0xffff);
      var bounds=Launcher.PixelBounds;if(bounds.Width<=0||bounds.Height<=0)return IntPtr.Zero;
      double dx=(x-bounds.Left-bounds.Width/2.0)/(bounds.Width/2.0),dy=(y-bounds.Top-bounds.Height/2.0)/(bounds.Height/2.0);
      if(dx*dx+dy*dy<1){handled=true;return new IntPtr(-1);} // HTTRANSPARENT
      return IntPtr.Zero;
    }
  }
}
