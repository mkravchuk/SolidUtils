using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SolidUtils.GUI
{
    internal class NativeConstants
    {
        internal const uint WM_MOUSEACTIVATE = 0x21;
        internal const uint MA_ACTIVATE = 1;
        internal const uint MA_ACTIVATEANDEAT = 2;
        internal const uint MA_NOACTIVATE = 3;
        internal const uint MA_NOACTIVATEANDEAT = 4;
    }


    // http://blogs.msdn.com/b/rickbrew/archive/2006/01/09/511003.aspx
    //
    // ...No-click-through is implemented is via the WM_MOUSEACTIVATE notification, 
    // and is handled in an overridden WndProc() method. 
    // It supports 4 return values which are a 2x2 matrix of "activate" and "eat mouse click." 
    // Turns out that the ToolStrip is returning the value corresponding to "activate and eat" (MA_ACTIVATEANDEAT) 
    // whereas we want "activate but do NOT eat" (MA_ACTIVATE)...

    /// <summary>
    /// Allow click on button when window is deactivated.
    /// Default beheviour when window is deactivated: active window by first click, and pefrom click button after second click.
    /// </summary>
    public class ToolStripEx : ToolStrip
    {
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == NativeConstants.WM_MOUSEACTIVATE &&
                m.Result == (IntPtr)NativeConstants.MA_ACTIVATEANDEAT)
            {
                m.Result = (IntPtr)NativeConstants.MA_ACTIVATE;
            }
            
        }
    }
}
