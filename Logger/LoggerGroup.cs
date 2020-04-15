using System;
using System.Diagnostics;
using Rhino.UI;

namespace SolidUtils
{
    public class LoggerGroup : IDisposable
    {
        public Stopwatch Watch { get; set; }
        public string Text { get; set; }
        public bool UseWaitCursor { get; set; }
        private WaitCursor waitCursor { get; set; }

        public LoggerGroup(string text, bool useWaitCursor = true)
        {
            UseWaitCursor = useWaitCursor;
            if (useWaitCursor)
            {
                waitCursor = new WaitCursor();
                waitCursor.Set();
            }
            Text = RemoveDots(text);
            if (Text != "")
            {
                if (Logger.IndentLevel == 0)
                {
                    Logger.log("");
                }
                Logger.log(Text + " ...");
                Logger.IndentLevel++;
            }
            Watch = Stopwatch.StartNew();
            Watch.Start();
        }

        public override string ToString()
        {
            Watch.Stop();
            var taken = ((long)(Watch.ElapsedTicks / 100));

            Watch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = Watch.Elapsed;
            
            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:0.000}", ts.TotalMilliseconds / 1000);
            return elapsedTime;
        }

        public void Dispose()
        {
            if (Text != "")
            {
                Logger.IndentLevel--;
                var message = "Finished in {0}   ({1})";
                Logger.log(message, ToString(), Text);
            }
            if (UseWaitCursor)
            {
                waitCursor.Clear();
            }
        }

        private string RemoveDots(string text)
        {
           return  text.Replace("...", "").Trim();
        }
    }
}
