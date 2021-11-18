using System;
using auxmic.logging;

namespace TestProject
{
    // Logging to console is handy for unit tests
    public class ConsoleLogger : AuxMicLog
    {
        public void Log(string message, Exception e = null)
        {
            Console.Error.WriteLine(message);
            if (e != null)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
        }
    }
}