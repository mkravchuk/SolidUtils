namespace SolidUtils
{
    public class LoggerBase
    {
        public static void log(string message, params object[] args)
        {
            Logger.log(message, args);
        }

        public void msg(string text, string title = "Message")
        {
            Logger.msg(text, title);
        }
    }
}
