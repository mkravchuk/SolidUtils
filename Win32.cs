﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using HWND = System.IntPtr;

namespace SolidUtils
{
    public class Win32
    {
        private Win32()
        {
        }

        public const int WM_USER = 0x400;
        public const int WM_PAINT = 0xF;
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_CHAR = 0x102;

        public const int EM_GETSCROLLPOS = (WM_USER + 221);
        public const int EM_SETSCROLLPOS = (WM_USER + 222);

        public const int VK_CONTROL = 0x11;
        public const int VK_UP = 0x26;
        public const int VK_DOWN = 0x28;
        public const int VK_NUMLOCK = 0x90;

        public const short KS_ON = 0x01;
        public const short KS_KEYDOWN = 0x80;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //internal static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
        [DllImport("user32")]
        public static extern int SendMessage(HWND hwnd, int wMsg, int wParam, IntPtr lParam);
        [DllImport("user32")]
        public static extern int PostMessage(HWND hwnd, int wMsg, int wParam, int lParam);
        [DllImport("user32")]
        public static extern short GetKeyState(int nVirtKey);
        [DllImport("user32")]
        public static extern int LockWindowUpdate(HWND hwnd);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SuspendThread(IntPtr hThread);

        //[DllImport("kernel32.dll")]
        //public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern bool TerminateThread(IntPtr hThread, uint dwExitCode);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        
    }

}
