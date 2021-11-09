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
        private readonly string _logfilename;
        private readonly FileInfo _logfileInfo;
        
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
            _logfilename = $"{_folder}{Path.DirectorySeparatorChar}{_logfilePrefix}.log";
            _logfileInfo = new FileInfo(_logfilename);
        }
        
        public void Log(string message, Exception e = null)
        {
            lock (logLock)
            {
                if (e != null)
                {
                    message += Environment.NewLine + e.StackTrace;
                }

                _logfileInfo.Refresh();
                RollLogs(_logfileInfo, message.Length);

                using StreamWriter logWriter = _logfileInfo.AppendText();
                logWriter.WriteLine($"{DateTime.Now:r} {message}");
                logWriter.Close();
            }
        }

        private void RollLogs(FileInfo logfileInfo, int moreBytes)
        {
            if (!logfileInfo.Exists || (logfileInfo.Length + moreBytes < _maxLogSizeBytes))
            {
                return;
            }
            logfileInfo.Refresh();

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
                return _logfilename;
            }

            return $"{_folder}{Path.DirectorySeparatorChar}{_logfilePrefix}-{fileCounter:00}.log";
        }
    }
}