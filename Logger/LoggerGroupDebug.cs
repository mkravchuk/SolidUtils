using System;
using System.Diagnostics;
using Rhino.UI;

namespace SolidUtils
{
    public class LoggerGroupDebug : IDisposable
    {
        private readonly bool savedEnabled;
        private LoggerGroup L;

        public LoggerGroupDebug(string text, bool useWaitCursor = true)
        {
            L = new LoggerGroup(text, useWaitCursor);
            savedEnabled = Logger.ENABLED;
            Logger.ENABLED = false;
        }

        public void Dispose()
        {
            Logger.ENABLED = savedEnabled;
            L.Dispose();
        }
    }
}
