using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using auxmic.logging;

namespace auxmic.mediaUtil
{
    public class CommandLineCapturer
    {
        private readonly AuxMicLog _log;

        /// <summary>
        /// Full path to executable file
        /// </summary>
        private readonly string _pathToExeName;

        private readonly string _niceName;

        private readonly string _initArgs;

        public CommandLineCapturer(AuxMicLog log, string exeName, string niceName = "?.exe", string initArgs = "")
        {
            _log = log;
            _pathToExeName = exeName;
            _niceName = niceName;
            _initArgs = initArgs;
        }
        
        public List<string> Execute(string exeArgs)
        {
            if (!File.Exists(_pathToExeName))
            {
                throw new ApplicationException(
                    $"Full path to {_niceName} executable file is not set or not correct.");
            }

            StringBuilder soutLog = new StringBuilder();
            List<string>  sout = new List<string>();
            
            using var process = new Process();
            try
            {
                process.StartInfo.FileName = $"\"{_pathToExeName}\"";
                process.StartInfo.Environment["AV_LOG_FORCE_NOCOLOR"] = "TRUE"; // should't be here. Shrug.
                process.StartInfo.Arguments = _initArgs + " " + exeArgs;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.ErrorDataReceived += (sender, args) => { RecordForLog("err: ", args); };
                process.OutputDataReceived += (sender, args) => { CaptureStdout(args); };
                
                bool started = process.Start();
                if (!started)
                {
                    _log.Log($"{_niceName} did not start");
                    _log.Log($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
                    throw new ApplicationException($"{_niceName} did not start");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _log.Log($"{_niceName} failed with exit code {process.ExitCode}");
                    _log.Log($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
                    if (soutLog.Length > 0)
                    {
                        _log.Log(soutLog.ToString());
                    }

                    throw new ApplicationException($"{_niceName} failed to execute");
                }

                return sout;
            }
            catch (ApplicationException)
            {
                throw;
            }
            catch (Exception e)
            {
                _log.Log($"{_niceName} failed to run: {e.Message}", e);
                throw;
            }

            void CaptureStdout(DataReceivedEventArgs args)
            {
                RecordForLog("out:", args);
                if (args.Data != null && args.Data.Trim().Length > 0) {
                    sout.Add(args.Data.Trim());
                }
            }

            void RecordForLog(string prefix, DataReceivedEventArgs args)
            {
                if (args.Data != null && args.Data.Trim().Length > 0)
                {
                    if (soutLog.Length > 0)
                    {
                        soutLog.Append(Environment.NewLine);
                    }
                    soutLog.Append(prefix).Append(args.Data.Trim());
                }
            }
        }
    }
}