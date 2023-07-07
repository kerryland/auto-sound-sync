using System;
using System.IO;

namespace auxmic.logging
{
    public class RollingLogFile : AuxMicLog
    {
        private readonly string _folder;
        private readonly string _logfilePrefix;
        private readonly int _maxLogCount;
        private readonly int _maxLogSizeBytes;
        
        private long currentLogfileSize;
        private static long LENGTH_OF_DATE_AND_EOL = 23;
        
        public string LogFilename { get; }

        private static readonly object logLock = new object();

        /// <summary>
        /// Writes logs to a disk file, ensuring that logs don't grow forever.
        /// This is not very efficient, but was entertaining to write. Maybe we should use a 'real' logging framework.
        /// </summary>
        /// <param name="folder">Where should we put the logfile</param>
        /// <param name="logfilePrefix">What is the name of the logfile, without the .log suffix</param>
        /// <param name="maxLogCount">What is the maximum number of log files we should create</param>
        /// <param name="maxLogSizeBytes">What is the maximum size of each log files</param>
        public RollingLogFile(string folder, string logfilePrefix, int maxLogCount, int maxLogSizeBytes)
        {
            _folder = folder;
            _logfilePrefix = logfilePrefix;
            _maxLogCount = maxLogCount;
            _maxLogSizeBytes = maxLogSizeBytes;
            LogFilename = $"{_folder}{Path.DirectorySeparatorChar}{_logfilePrefix}.log";
            FileInfo _logfileInfo = new FileInfo(LogFilename);
            if (_logfileInfo.Exists)
            {
                currentLogfileSize = _logfileInfo.Length;
            }
        }
       
        public void Log(string message, Exception e = null)
        { 
            while (e != null)
            {
                message += Environment.NewLine + e.Message + ": " + Environment.NewLine + e.StackTrace;
                e = e.InnerException;
                if (e != null)
                {
                    message += Environment.NewLine + "-----" + Environment.NewLine;
                }
            }
   
            lock (logLock)
            {
                RollLogs(message.Length);

                currentLogfileSize += message.Length + LENGTH_OF_DATE_AND_EOL;
                    
                using StreamWriter logWriter = new StreamWriter(LogFilename, append: true);
                logWriter.WriteLine($"{DateTime.Now:u} {message}");
                logWriter.Close();
            }
        }

        private void RollLogs(int moreBytes)
        {
            if (currentLogfileSize + moreBytes < _maxLogSizeBytes)
            {
                return;
            }

            currentLogfileSize = 0;

            for (int fileCounter = _maxLogCount - 1; fileCounter >= 0; fileCounter--)
            {
                string fromFilename = GenerateLogfileName(fileCounter);
                string toFilename = GenerateLogfileName(fileCounter + 1);
                try
                {
                    File.Move(fromFilename, toFilename, true);
                }
                catch (FileNotFoundException)
                {
                    // That's fine
                }
            }
            File.Delete(GenerateLogfileName(_maxLogCount));
        }

        private string GenerateLogfileName(int fileCounter)
        {
            if (fileCounter == 0)
            {
                return LogFilename;
            }

            return $"{_folder}{Path.DirectorySeparatorChar}{_logfilePrefix}-{fileCounter:00}.log";
        }
    }
}