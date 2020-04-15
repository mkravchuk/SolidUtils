using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Rhino;

namespace SolidUtils
{
    public enum SharedCommandsEnum
    {
        SN_RefreshTree, SN_RefreshSelected, SN_HoldThisSelection,
        SI_FindAllIssues_IfAutoSearchEnable, SI_FixAllIssues_And_RefreshIssueListIfOptionAutosearchIsSelected, IS_ClearIssues_FileGroupOperationStarted,
        ST_UpdateGeomNames
    }

    public enum NumDisplayStyle
    {
        StartsFrom0, StartsFrom1
    }

    public static class Shared
    {
        public static string AUTOFIX_NOT_IMPLEMENTED = "Auto-fix not implemented yet";

        public static bool FILEGROUPOPERATIONS_ENABLED = false;
        public static string FILEGROUPOPERATIONS_FILENAME = "";
        public static string FILEGROUPOPERATIONS_CAPTION_SHORT = "";
        public static bool FILEGROUPOPERATIONS_CONVERTINGFILES = false;
        public static string LAST_OPENED_DOC_FILENAME = "";

        public static string GetCurrentDocFilename()
        {
            var fileNameFull = "";

            var doc = RhinoDoc.ActiveDoc;
            if (doc != null) fileNameFull = doc.Path;

            if (Shared.FILEGROUPOPERATIONS_ENABLED)
            {
                fileNameFull = Shared.FILEGROUPOPERATIONS_FILENAME;
            }
            if (String.IsNullOrEmpty(fileNameFull))
            {
                fileNameFull = LAST_OPENED_DOC_FILENAME;
            }
            return fileNameFull;
        }

        public static bool DisableConduit { get; set; }
        public static bool ForceToFixManualIssues { get; set; }
        public static bool IsForeachParallelInProgress { get; set; }
        public static NumDisplayStyle NumDisplayStyle { get; set; }//SolidIssues.Options.EdgeNumbersDisplayStyle
        public static bool UseMultithreading { get; set; } //SolidIssues.Options.UseMultithreading
        public static int UseMultithreading_MaxThreadsCount { get; set; } //SolidIssues.Options.UseMultithreading_MaxThreadsCount
        public static int MultithreadingTimeout { get; set; } //in milliseconds, SolidIssues.Options.MultithreadingTimeout
        public static int Environment_ProcessorCount { get; private set; } // cache
        public static bool IsDebugMode { get; private set; }
        public static bool IsMyPC { get; private set; }        
        private static int _mainThreadId;
        public static bool IsExecutingInMainThread
        {
            get { return Thread.CurrentThread.ManagedThreadId == _mainThreadId; }
        }
        public static object RhinoLock { get; private set; } // lock object for all accesses to RhinoApp class
        public static object RhinoInvokeLock { get; private set; } // lock object for all invoke accesses to RhinoApp class

        static Shared()
        {
            Environment_ProcessorCount = Environment.ProcessorCount;
            IsDebugMode = Debugger.IsAttached;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            CatchExceptions = !IsDebugMode;
            RhinoLock = new object();
            RhinoInvokeLock = new object();

            try
            {
                if (File.Exists(@"c:\SolidFixDebugInfo.txt"))
                {
                    var debugInfo = File.ReadAllText(@"c:\SolidFixDebugInfo.txt");
                    IsMyPC = debugInfo.Contains("IsMyPC");
                }
            }
            catch
            {
                // nothing
            }
        }


        #region SharedCommands
        public class SharedCommandsClass
        {
            private Dictionary<SharedCommandsEnum, Func<RhinoDoc, object, bool>> Commands { get; set; }


            public SharedCommandsClass()
            {
                Commands = new Dictionary<SharedCommandsEnum, Func<RhinoDoc, object, bool>>();
            }

            public void Register(SharedCommandsEnum commandType, Func<RhinoDoc, object, bool> command)
            {
                Commands[commandType] = command;
            }

            public bool Execute(SharedCommandsEnum commandType, object data = null)
            {
                var doc = RhinoDoc.ActiveDoc;

                if (doc == null)
                {
                    return false;
                }

                if (!Commands.ContainsKey(commandType))
                {
                    return false;
                }

                var command = Commands[commandType];
                bool res = false;
                Shared.TryCatchAction(() => res = command(doc, data), g.SolidFix, "Failed to execute command " + commandType);
                return res;
            }
        }
        public static SharedCommandsClass SharedCommands = new SharedCommandsClass();
        #endregion


        #region TryCatch
        public static bool CatchExceptions { get; set; }

        // used in method MakeScreenShot and class ConduitShowRotationPoint

        /// <summary>
        /// Execute some action with try..catch. Logs exception to console.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="group"></param>
        /// <param name="logExceptionDescription">defined as object just for better perfromance - this will allow to send some object that will be convertable to string</param>
        [HandleProcessCorruptedStateExceptions]// - debug only
        public static void TryCatchAction(Action a, g group, object logExceptionDescription)
        {
            if (CatchExceptions)
            {
                try
                {
                    a();
                }
                catch (Exception ex)
                {
                    if (ex is ThreadAbortException) return;// do nothing on thread termination by timout
                    if (IsDebugMode) Debugger.Break();
                    log.exception(group, ex, logExceptionDescription.ToString());
                }
            }
            else
            {
                a();
            }

        }

        [HandleProcessCorruptedStateExceptions]// - debug only
        public static void TryCatchActionFast(Action a) // duplicated method without parameters for faster performance
        {
            if (CatchExceptions)
            {
                try
                {
                    a();
                }
                catch (Exception ex)
                {
                    if (IsDebugMode) Debugger.Break();
                    log.exception(g.ExceptionHandler, ex, "");
                }
            }
            else
            {
                a();
            }
        }
        #endregion

        public static int GUIComponentNum(int componentIndex)
        {
            var num = componentIndex;
            if (Shared.NumDisplayStyle == NumDisplayStyle.StartsFrom1)
            {
                num++;
            }
            return num;
        }
    }
}