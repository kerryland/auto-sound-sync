using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using auxmic.logging;

namespace auxmic.mediaUtil
{
    // Launch the ffmpeg executable and capture its output nicely.
    public class FFmpegTool
    { 
        private readonly AuxMicLog Log;

        /// <summary>
        /// Full path to FFmpeg executable file `ffmpeg.exe`
        /// </summary>
        public static string PathToFFmpegExe { get; set; }

        public FFmpegTool(AuxMicLog log, string fFmpegExe)
        {
            Log = log;
            PathToFFmpegExe = fFmpegExe;
        }

        public void ExecuteFFmpeg(string exportArgs)
        {
            if (!File.Exists(PathToFFmpegExe))
            {
                throw new ApplicationException(
                    "Full path to FFmpeg executable file `ffmpeg.exe` is not set or not correct.");
            }

            StringBuilder sout = new StringBuilder();
            using var process = new Process();
            try
            {
                process.StartInfo.FileName = $"\"{PathToFFmpegExe}\"";
                process.StartInfo.Environment["AV_LOG_FORCE_NOCOLOR"] = "TRUE";
                process.StartInfo.Arguments = "-hide_banner -nostats " + exportArgs;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.ErrorDataReceived += (sender, args) => { CaptureMessage("err: ", args); };
                process.OutputDataReceived += (sender, args) => { CaptureMessage("out: ", args); };
                
                bool started = process.Start();
                if (!started)
                {
                    Log.Log($"FFmpeg did not start");
                    Log.Log($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
                    throw new ApplicationException("ffmpeg did not start");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Log.Log($"FFmpeg failed with exit code {process.ExitCode}");
                    Log.Log($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
                    if (sout.Length > 0)
                    {
                        Log.Log(sout.ToString());
                    }

                    throw new ApplicationException("ffmpeg failed to execute");
                }
            }
            catch (ApplicationException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Log($"FFmpeg failed to run: {e.Message}", e);
                throw;
            }

            void CaptureMessage(string prefix, DataReceivedEventArgs args)
            {
                if (args.Data != null && args.Data.Trim().Length > 0)
                {
                    if (sout.Length > 0)
                    {
                        sout.Append(Environment.NewLine);
                    }
                    sout.Append(prefix).Append(args.Data.Trim());
                }
            }
        }
    }
}