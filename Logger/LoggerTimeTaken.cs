using System;
using System.Diagnostics;

namespace SolidUtils
{
    public class LoggerTimeTaken : IDisposable
    {
        public Stopwatch Watch { get; set; }
        public string Text { get; set; }
        public bool WriteEnter { get; set; }

        public LoggerTimeTaken(string text = "Time taken: {0}", bool enter = true)
        {
            Text = text;

            Watch = Stopwatch.StartNew();
            Watch.Start();
            WriteEnter = enter;
        }

        public override string ToString()
        {
            Watch.Stop();
            var taken = ((long)(Watch.ElapsedTicks / 100));
            //myStopWatch.ElapsedMilliseconds

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
            Logger.log(this.ToString(), WriteEnter);
        }
    }
}
