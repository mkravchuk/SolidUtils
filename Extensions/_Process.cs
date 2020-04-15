using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class _Process
    {
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern bool TerminateThread(IntPtr hThread, uint dwExitCode);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static void _Suspend(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                //if (thread.Id != 6612) continue;
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                SuspendThread(pOpenThread);
            }
        }
        public static void _Resume(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                ResumeThread(pOpenThread);
            }
        }

       


        //public static bool _Suspend(this Thread thread)
        //{
        //    var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.ManagedThreadId);
        //    if (pOpenThread != IntPtr.Zero)
        //    {
        //        SuspendThread(pOpenThread);
        //        return true;
        //    }
        //    return false;
        //}
        public static bool SuspendThread(uint threadId)
        {
            var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, threadId);
            if (pOpenThread != IntPtr.Zero)
            {
                SuspendThread(pOpenThread);
                return true;
            }
            return false;
        }

        public static bool TerminateThread(uint threadId)
        {
            var pOpenThread = OpenThread(ThreadAccess.TERMINATE, false, threadId);
            if (pOpenThread != IntPtr.Zero)
            {
                TerminateThread(pOpenThread, 0);
                return true;
            }
            return false;
        }


        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            if (field != null)
            {
                return field.GetValue(instance);
            }
            else
            {
                return null;
            }
        }
        public static bool _TerminateUnmanagedThread(this Thread thread)
        {
            MethodInfo GetNativeHandle = thread.GetType().GetMethod("GetNativeHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            if (GetNativeHandle != null)
            {
                object threadHandle = GetNativeHandle.Invoke(thread, new object[] { });
                if (threadHandle != null)
                {
                    object m_ptr = GetInstanceField(threadHandle.GetType(), threadHandle, "m_ptr");
                    if (m_ptr != null)
                    {
                        Win32.SuspendThread((IntPtr)m_ptr);
                        //Win32.TerminateThread((IntPtr) m_ptr, 0);
                    }
                }
            }
            return false;
        }
    }
}
