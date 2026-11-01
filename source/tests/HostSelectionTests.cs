using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RingTimer;
class HostSelectionTests {
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateWindowEx(int ex,string cls,string name,uint style,int x,int y,int w,int h,IntPtr parent,IntPtr menu,IntPtr module,IntPtr data);
 [DllImport("user32.dll")]static extern bool DestroyWindow(IntPtr h);
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
 static string log;static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);File.AppendAllText(log,"PASS "+name+Environment.NewLine);count++;}
 static IntPtr Resolve(IntPtr h){return (IntPtr)typeof(Native).GetMethod("CodexHost").Invoke(null,new object[]{h});}
 [STAThread]static int Main(string[] args){log=args[0];File.WriteAllText(log,"");IntPtr dialog=IntPtr.Zero,nested=IntPtr.Zero,orphan=IntPtr.Zero;
 using(var host=new Form{Text="Host fixture",StartPosition=FormStartPosition.Manual,Left=-2500,Top=0,Width=900,Height=700})using(var second=new Form{Left=-2500,Top=0})using(var tool=new Form{FormBorderStyle=FormBorderStyle.FixedToolWindow,Left=-2500,Top=0}){
 try{host.Show();second.Show();tool.Show();ShowWindow(host.Handle,5);ShowWindow(second.Handle,5);ShowWindow(tool.Handle,5);Application.DoEvents();Check(Native.IsWindowVisible(host.Handle),"fixture main window is visible");Check(Native.IsCodex(host.Handle),"main process window is recognized");
 dialog=CreateWindowEx(0,"#32770","Error fixture",0x90C80000,-2400,50,400,180,host.Handle,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);Check(dialog!=IntPtr.Zero,"native error dialog created");
 Check(!Native.IsCodex(dialog),"native error dialog cannot be selected as host");
 File.AppendAllText(log,"host="+host.Handle+" dialog="+dialog+" resolved="+Resolve(dialog)+" owner="+typeof(Native).GetMethod("GetWindow",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{dialog,(uint)4})+" visible="+Native.IsWindowVisible(host.Handle)+" iconic="+Native.IsIconic(host.Handle)+Environment.NewLine);Check(Resolve(dialog)==host.Handle,"error dialog resolves to its main window");host.Enabled=false;Check(Resolve(dialog)==host.Handle,"modal disabled main window remains anchor");
 nested=CreateWindowEx(0,"#32770","Nested error fixture",0x90C80000,-2400,80,300,150,dialog,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);Check(Resolve(nested)==host.Handle,"nested dialog resolves original host");
 orphan=CreateWindowEx(0,"#32770","Ownerless error fixture",0x90C80000,-2400,80,300,150,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);Check(Resolve(orphan)==IntPtr.Zero,"ownerless native dialog is excluded");
 Check(!Native.IsCodex(tool.Handle)&&Resolve(tool.Handle)==IntPtr.Zero,"standalone tool window is excluded");Check(Resolve(second.Handle)==second.Handle,"second main window remains selectable");
 Check(Resolve(IntPtr.Zero)==IntPtr.Zero,"missing foreground is excluded");DestroyWindow(nested);nested=IntPtr.Zero;var old=dialog;DestroyWindow(dialog);dialog=IntPtr.Zero;Check(Resolve(old)==IntPtr.Zero,"closed dialog is excluded");Check(Resolve(host.Handle)==host.Handle,"main window remains usable after dialog closes");
 host.Hide();Check(Resolve(host.Handle)==IntPtr.Zero,"hidden main window is excluded");File.AppendAllText(log,count+" host selection checks passed");return 0;
 }catch(Exception e){File.AppendAllText(log,"FAIL "+e);return 1;}finally{foreach(var h in new[]{nested,dialog,orphan})if(h!=IntPtr.Zero)DestroyWindow(h);}}
 }
}
