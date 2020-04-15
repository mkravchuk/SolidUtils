using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Rhino;
using Rhino.UI;
using SolidUtils.GUI;

namespace SolidUtils
{
    public enum logType
    {
        info, warn, error, debug, temp, wrong
    }


    public static class log
    {
        private static object logfile_lockobject = new object();
        private static readonly bool IsDebugMode = false;
        public static uint LogNumer;

        public static string LOG_FILE_NAME = @"f:\Rhino_SolidFix_Log.txt";
        public static bool ENABLED = true;
        public static bool ENABLED_DEBUG_LOGS = true;
        public static bool ENABLED_FILE_LOGS = false;
        public static bool ENABLED_LogAllMessagesToLogFile = ENABLED_DEBUG_LOGS;

        public static string PREFIX_ALL_SUB_INDENTS = "                    ";
        public static string PREFIX_INFO = "";
        public static string PREFIX_DEBUG = "";
        public static string PREFIX_WARN = "!  ";
        public static string PREFIX_ERROR = "!!! ERROR: ";
        public static string PREFIX_EXCEPTION = "!!! EXCEPTION: ";
        public static string PREFIX_TEMP = "";
        public static string PREFIX_WRONG = "!!!  WRONG:";

        static log()
        {
            IsDebugMode = Debugger.IsAttached;
            if (!ENABLED) return;
            if (!ENABLED_FILE_LOGS) return;

            #region InitLogs

            if (IsDebugMode && ENABLED_FILE_LOGS)
            {
                var isLogFileAvailable = true;
                try
                {
                    File.WriteAllText(LOG_FILE_NAME, "");
                }
                catch
                {
                    isLogFileAvailable = false;
                }

                if (!isLogFileAvailable)
                {
                    try
                    {
                        LOG_FILE_NAME = @"c:\Rhino_SolidFix_Log.txt";
                        File.WriteAllText(LOG_FILE_NAME, "");
                        isLogFileAvailable = true;
                    }
                    catch
                    {
                        isLogFileAvailable = false;
                    }
                }

                if (!isLogFileAvailable)
                {
                    ENABLED_FILE_LOGS = false;
                }
            }
            else
            {
                ENABLED_FILE_LOGS = false; // if debugger is not attached - disable logs to file
            }

            #endregion


        }

        #region Indent

        private static string INDENT_PREFIX = "     ";
        private static int indentLevel;
        public static int IndentLevel
        {
            get { return indentLevel; }
            set
            {
                if (value == indentLevel + 1)
                {
                    IndentPrefix += INDENT_PREFIX;
                }
                else
                {
                    string prefix = "";
                    for (int i = 0; i < value; i++)
                    {
                        prefix += INDENT_PREFIX;
                    }
                    IndentPrefix = prefix;
                }
                indentLevel = value;
            }
        }

        private static string IndentPrefix;

        #endregion

        #region Public Methods



        public static void rawText(g group, string s)
        {
            if (!ENABLED) return;
            if (!Group.IsEnabled(group)) return;
            lock (Shared.RhinoLock)
            {
                RhinoApp.Write(s);
            }
            LogNumer++;
        }

        public static void custom(logType type, g group, string message, params object[] args)
        {
            switch (type)
            {
                case logType.info:
                    info(group, message, args);
                    break;
                case logType.warn:
                    warn(group, message, args);
                    break;
                case logType.error:
                    error(group, message, args);
                    break;
                case logType.debug:
                    debug(group, message, args);
                    break;
                case logType.temp:
                    temp(message, args);
                    break;
                case logType.wrong:
                    wrong(message, args);
                    break;
                default:
                    info(group, message, args);
                    break;
            }
        }

        public static void info(g group, string message, params object[] args)
        {
            if (!ENABLED) return;
            WriteLineFmt(PREFIX_INFO, message, group, args);
        }

        /// <summary>
        /// Warn user about some important information that he can take in account.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void warn(g group, string message, params object[] args)
        {
            if (!ENABLED) return;
            WriteLineFmt(PREFIX_WARN + "[" + group + "] ", message, group, args);
        }

        /// <summary>
        /// Additionl information for developer. 
        /// Messages will be visible only when user activates them.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void debug(g group, string message, params object[] args)
        {
            if (!ENABLED) return;
            if (!ENABLED_DEBUG_LOGS) return;
            WriteLineFmt(PREFIX_DEBUG, message, group, args);
        }

        /// <summary>
        /// Temporal information for making improvements. 
        /// Messages will be visible only in Debug mode (when running from Visual Studio)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void temp(string message, params object[] args)
        {
            if (!ENABLED) return;
            if (!ENABLED_DEBUG_LOGS) return;
            if (!IsDebugMode) return;
            WriteLineFmt(PREFIX_TEMP, message, g.Temp, args);
        }

        /// <summary>
        /// Something in code maybe dont work as expected. We dont know yet.
        /// In debug mode execution will be breaked - Visual Studio will be activated - this will give a chanche to developer to analize new unknown case and decide what indead should be handling of situation.
        /// This is not an error - algorithm still can work.
        /// Developer should take that into consideration to make results better.
        /// Developer should use this method if we want to catch some unknown case of execution.
        /// Messages will be visible only in Debug mode (when running from Visual Studio)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void wrong(string message, params object[] args)
        {
            //if (!ENABLED) return; - wrong messages should be always visible to developer - we need them to increase quality of this app!!!
            if (!ENABLED_DEBUG_LOGS) return;
            if (!IsDebugMode) return;
            WriteLineFmt(PREFIX_WRONG, message, g.Temp, args);
            //Debugger.Break();
        }


        /// <summary>
        /// Algorithm dont work as expected and user should be notified.
        /// Since we have implemented try..catch globaly - it is possible to throw expcetion.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void error(g group, string message, params object[] args)
        {
            if (!ENABLED) return;
            WriteLineFmt(PREFIX_ERROR, message, group, args);
        }

        /// <summary>
        /// Algorithm dont work as expected and user should be notified.
        /// Since we have implemented try..catch globaly - it is possible to throw expcetion.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private static void exception(g group, string message, params object[] args)
        {
            if (!ENABLED && !ENABLED_DEBUG_LOGS) return;// force to show exceptions if app running on my PC
            WriteLineFmt(PREFIX_EXCEPTION, message, group, args);
        }

        /// <summary>
        /// Exception happend in a code.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="ex"></param>
        /// <param name="description"></param>
        public static void exception(g group, Exception ex, string description)
        {
            //HostUtils.ExceptionReport(ex);
            if (ex.InnerException != null)
            {
                exception(group, ex.InnerException, description);
               // return;
            }
            var stackTraceLines = ex.StackTrace.Split('\n', '\r').Where(o => !String.IsNullOrEmpty(o)).ToList();

            // LOG in console
            exception(group, "Exception in {0}: {1}:  {2}\nStackTrace: \n{3}\n", group, description, ex.Message, ex.StackTrace);
            //error(group, "Source:  {0}", ex.Source);
            //error(group, "StackTrace:");
            //for (var i =0; i < Math.Min(2, stackTraceLines.Count); i++) // only 2 lines of stachtrace - other see in log file
            //for (var i =0; i < stackTraceLines.Count; i++)
            //{
            //    exception(group, stackTraceLines[i]);
            //}

            // LOG in file
            file("Exception in {0}: {1}:  {2}", group, description, ex.Message);
            file("Source:  {0}", ex.Source);
            file("StackTrace:");
            foreach (var stLine in stackTraceLines)
            {
                file(stLine);
            }
        }


        public static void msg(string text, string title)
        {
            if (!ENABLED) return;

            WriteLine("");
            WriteLine("--------------------------------------");
            WriteLine("---" + title + "---");
            WriteLine("");
            WriteLine(text);
            WriteLine("");
            WriteLine("--------------------------------------");
            WriteLine("");


            //
            // #1
            //
            //MessageBox.Show(text);

            //
            // #2
            //
            //doesnt work with big test - edit box appears empty!!! - i dont know why.

            //text = "ON_Brep:\nsurfaces:  1\n3d curve:  3\n2d curves: 6\nvertices:  5\nedges:     5\ntrims:     6\nloops:     1\nfaces:     1\ncurve2d[ 0]: TL_NurbsCurve domain(0,0.699715) start(0.0128737,0) end(0.954987,0)\ncurve2d[ 1]: TL_NurbsCurve domain(0,1) start(1,0.5) end(1,0.5)\ncurve2d[ 2]: TL_NurbsCurve domain(0.0872745,0.154707) start(1,0.5) end(0.954545,1)\ncurve2d[ 3]: TL_NurbsCurve domain(-0.780843,0) start(0,1) end(0.0128737,0)\ncurve2d[ 4]: TL_NurbsCurve domain(0.699715,0.733034) start(0.954987,0) end(1,0.5)\ncurve2d[ 5]: TL_NurbsCurve domain(0.154707,1.5708) start(0.954545,1) end(0,1)\ncurve3d[ 0]: TL_NurbsCurve domain(0,0.733034) start(435.325,-160.161,325.005) end(435.822,-160.161,325.453)\ncurve3d[ 1]: TL_NurbsCurve domain(0.0872745,1.5708) start(435.822,-160.161,325.453) end(435.822,-160.659,324.997)\ncurve3d[ 2]: TL_NurbsCurve domain(-0.780843,0) start(435.822,-160.659,324.997) end(435.325,-160.161,325.005)\nsurface[ 0]: TL_NurbsSurface u(0,1) v(0,1)\nvertex[ 0]: (435.324917 -160.161210 325.005109) tolerance(8.64613e-08)\n\tedges (0,2)\nvertex[ 1]: (435.791433 -160.161033 325.451895) tolerance(1.17002e-08)\n\tedges (0,3)\nvertex[ 2]: (435.822178 -160.161032 325.452841) tolerance(0)\n\tedges (1,3)\nvertex[ 3]: (435.822178 -160.192082 325.451888) tolerance(7.53934e-08)\n\tedges (1,4)\nvertex[ 4]: (435.822178 -160.659311 324.996618) tolerance(2.51321e-06)\n\tedges (2,4)\nedge[ 0]: v0( 0) v1( 1) 3d_curve(0) tolerance(8.63749e-08)\n\tdomain(0,0.699715) start(435.325,-160.161,325.005) end(435.791,-160.161,325.452)\n\ttrims (+0)\nedge[ 1]: v0( 2) v1( 3) 3d_curve(1) tolerance(0.00249911)\n\tdomain(0.0872745,0.154707) start(435.822,-160.161,325.453) end(435.822,-160.192,325.452)\n\ttrims (+3)\nedge[ 2]: v0( 4) v1( 0) 3d_curve(2) tolerance(2.5107e-06)\n\tdomain(-0.780843,0) start(435.822,-160.659,324.997) end(435.325,-160.161,325.005)\n\ttrims (+5)\nedge[ 3]: v0( 1) v1( 2) 3d_curve(0) tolerance(0.00247504)\n\tdomain(0.699715,0.733034) start(435.791,-160.161,325.452) end(435.822,-160.161,325.453)\n\ttrims (+1)\nedge[ 4]: v0( 3) v1( 4) 3d_curve(1) tolerance(2.5107e-06)\n\tdomain(0.154707,1.5708) start(435.822,-160.192,325.452) end(435.822,-160.659,324.997)\n\ttrims (+4)\nface[ 0]: surface(0) reverse(0) loops(0)\n\tFast render mesh: 0 polygons\n\tloop[ 0]: type(outer) 6 trims(0,1,2,3,4,5)\n\t\ttrim[ 0]: edge( 0) v0( 0) v1( 1) tolerance(0,1.54767e-07)\n\t\t\ttype(boundary-south side iso) rev3d(0) 2d_curve(0)\n\t\t\tdomain(0,0.699715) start(0.0128737,0) end(0.954987,0)\n\t\t\tsurface points start(435.325,-160.161,325.005) end(435.791,-160.161,325.452)\n\t\ttrim[ 1]: edge( 3) v0( 1) v1( 2) tolerance(0,0.334961)\n\t\t\ttype(boundary) rev3d(0) 2d_curve(4)\n\t\t\tdomain(0.699715,0.733034) start(0.954987,0) end(1,0.5)\n\t\t\tsurface points start(435.791,-160.161,325.452) end(435.822,-160.161,325.453)\n\t\ttrim[ 2]: edge(-1) v0( 2) v1( 2) tolerance(0,0)\n\t\t\ttype(singular) rev3d(0) 2d_curve(1)\n\t\t\tdomain(0,1) start(1,0.5) end(1,0.5)\n\t\t\tsurface points start(435.822,-160.161,325.453) end(435.822,-160.161,325.453)\n\t\ttrim[ 3]: edge( 1) v0( 2) v1( 3) tolerance(0,0.334961)\n\t\t\ttype(boundary) rev3d(0) 2d_curve(2)\n\t\t\tdomain(0.0872745,0.154707) start(1,0.5) end(0.954545,1)\n\t\t\tsurface points start(435.822,-160.161,325.453) end(435.822,-160.192,325.452)\n\t\ttrim[ 4]: edge( 4) v0( 3) v1( 4) tolerance(0,0)\n\t\t\ttype(boundary-north side iso) rev3d(0) 2d_curve(5)\n\t\t\tdomain(0.154707,1.5708) start(0.954545,1) end(0,1)\n\t\t\tsurface points start(435.822,-160.192,325.452) end(435.822,-160.659,324.997)\n\t\ttrim[ 5]: edge( 2) v0( 4) v1( 0) tolerance(6.83277e-08,3.10794e-09)\n\t\t\ttype(boundary) rev3d(0) 2d_curve(3)\n\t\t\tdomain(-0.780843,0) start(0,1) end(0.0128737,0)\n\t\t\tsurface points start(435.822,-160.659,324.997) end(435.325,-160.161,325.005)";
            //Rhino.UI.Dialogs.ShowTextDialog(text, title);


            //
            // #3
            //
            //string outtext;
            //Rhino.UI.Dialogs.ShowEditBox(title, text, "ee", true, out outtext);
        }

        /// <summary>
        /// Log debug information to file.
        /// Messages will be written to the file only in Debug mode (when running from Visual Studio)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void file(string message, params object[] args)
        {
            if (!ENABLED) return;
            //if (!ENABLED_DEBUG_LOGS) return; - we will write to file even if debug logs are disabled - because we want to handle all exceptions. anyway log.file will work only when debugger is attached
            if (!ENABLED_FILE_LOGS) return;

            if (args != null && args.Length != 0)
            {
                message = String.Format(message, args);
            }
            //message = Thread.CurrentThread.ManagedThreadId + ": " + message;
            lock (logfile_lockobject)
            {
                File.AppendAllText(LOG_FILE_NAME, message + "\n");

                //var len = new FileInfo(LOG_FILE_NAME).Length;
                //if (len == 112)
                //{
                //    var temp = 0;
                //}
            }
        }

        public static void clearlogfile()
        {
            if (!ENABLED) return;
            if (!ENABLED_FILE_LOGS) return;
            lock (logfile_lockobject)
            {
                File.WriteAllText(LOG_FILE_NAME, "");
            }
        }

        #endregion

        #region Private Methods

        private static void WriteLine(string s)
        {
            if (Shared.IsExecutingInMainThread)
            {
                lock (Shared.RhinoLock)
                {
                    RhinoApp.WriteLine(s);
                }
                if (ENABLED_LogAllMessagesToLogFile)
                {
                    File.AppendAllText(LOG_FILE_NAME, s + "\n");
                }
            }
            else
            {
                //Debugger.Break();
                lock (Shared.RhinoInvokeLock)
                {
                    if (RhinoApp.MainApplicationWindow.InvokeRequired)
                    {
                        RhinoApp.MainApplicationWindow.Invoke((Action)(() => WriteLine(s)));
                    }
                }
            }
            LogNumer++;
        }
        private static void WriteLineFmt(string prefix, string message, g group, params object[] args)
        {
            if (!Group.IsEnabled(group)) return;
            if (args != null && args.Length != 0)
            {
                message = String.Format(message, args);
            }
            string prefixSub = "";
            if (indentLevel != 0)
            {
                prefixSub = PREFIX_ALL_SUB_INDENTS;
            }
            WriteLine(prefix + IndentPrefix + prefixSub + message);
        }

        #endregion

        #region Groups

        public class Disabler : IDisposable
        {
            private bool savedENABLED;
            public Disabler()
            {
                savedENABLED = log.ENABLED;
                log.ENABLED = false;
            }

            public void Dispose()
            {
                log.ENABLED = savedENABLED;
            }
        }

        public class GroupDEBUG : Group
        {
            public GroupDEBUG(g _group, string text, bool useWaitCursor = true)
                : base(_group, text, useWaitCursor, logType.debug)
            {

            }
        }

        public class Group : IDisposable
        {
            protected static List<g> groupsStack = new List<g>(1000);
            protected static List<string> captionsStack = new List<string>(1000);
            protected static int groupsLevel = 0;
            private static object lockObject = new object();

            public Stopwatch Watch { get; set; }
            public string Text { get; set; }
            public bool UseWaitCursor { get; set; }
            private WaitCursor waitCursor { get; set; }
            private g group { get; set; }
            private logType LogType { get; set; }

            public static List<g> StackOfGroups
            {
                get { return groupsStack; }
            }
            public static List<string> StackOfCaptions
            {
                get { return captionsStack; }
            }

            /// <summary>
            /// Starts from 1
            /// </summary>
            public static int StackCount
            {
                get { return groupsLevel; }
            }

            protected Group(g group, string text, bool useWaitCursor, logType type)
            {
                this.group = group;
                Text = RemoveDots(text);
                UseWaitCursor = useWaitCursor;
                LogType = type;
                Init();
            }

            public Group(g group, string text, bool useWaitCursor = true)
                : this(group, text, useWaitCursor, logType.info)
            {
            }

            private void Init()
            {
                if (Text == "") return;
                lock (lockObject)
                {
                    while (groupsStack.Count <= groupsLevel + 1)
                    {
                        groupsStack.Add(g.None);
                    }
                    while (captionsStack.Count <= groupsLevel + 1)
                    {
                        captionsStack.Add("");
                    }
                    groupsLevel++;
                    groupsStack[groupsLevel] = group;
                    captionsStack[groupsLevel] = Text;

                    if (UseWaitCursor && groupsLevel <= 1)
                    {
                        waitCursor = new WaitCursor();
                        waitCursor.Set();
                    }
                    if (Text != "")
                    {
                        if (IndentLevel == 0)
                        {
                            logmessage(group, "");
                        }
                        logmessage(group, Text + " ...");
                        IndentLevel++;
                    }
                    Watch = Stopwatch.StartNew();
                    Watch.Start();
                }
            }

            public override string ToString()
            {
                Watch.Stop();
                TimeSpan ts = Watch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:0.000}", ts.TotalMilliseconds / 1000);
                return elapsedTime;
            }

            public void Dispose()
            {
                if (Text == "") return;
                lock (lockObject)
                {
                    if (Text != "")
                    {
                        IndentLevel--;
                        var message = "Finished in {0}   ({1})";
                        logmessage(group, message, ToString(), Text);
                    }
                    if (waitCursor != null)
                    {
                        waitCursor.Clear();
                    }
                    groupsStack[groupsLevel] = g.None;
                    captionsStack[groupsLevel] = "";
                    groupsLevel--;
                    if (groupsLevel == 0)
                    {
                        ColoredProgress.CloseWindow();
                        //log.temp("Thread.Sleep(3000);");
                        //Thread.Sleep(3000);
                    }
                }
            }

            private void logmessage(g group, string message, params object[] args)
            {
                log.custom(LogType, group, message, args);
            }

            private string RemoveDots(string text)
            {
                return text.Replace("...", "").Trim();
            }

            public static bool IsEnabled(g group)
            {
                return g_enabled.IsEnabled(group);
            }
        }

        //public static log.Group newGroup(string text, g group, bool useWaitCursor = true)
        //{
        //    return new log.Group(text, group, useWaitCursor);
        //}

        #endregion

        #region TimeTaken

        public class TimeTaken : IDisposable
        {
            public Stopwatch Watch { get; set; }
            public string Text { get; set; }

            public TimeTaken(string text = "Time taken: {0}")
            {
                Text = text;

                Watch = Stopwatch.StartNew();
                Watch.Start();
            }

            public override string ToString()
            {
                Watch.Stop();
                // Get the elapsed time as a TimeSpan value.
                TimeSpan ts = Watch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds);
                var message = String.Format(Text, elapsedTime);

                return message;
            }

            public void Dispose()
            {
                log.debug(g.None, this.ToString());
            }
        }

        #endregion
    }
}
