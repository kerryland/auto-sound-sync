using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
            var queue = new ConcurrentQueue<string>();

            var flushTask = new System.Timers.Timer(50);
            flushTask.Elapsed += (s, e) =>
            {
                while (!queue.IsEmpty)
                {
                    string line = null;
                    if (queue.TryDequeue(out line))
                        Log.Log(line);
                }
            };
            flushTask.Start();

            using var process = new Process();
            try
            {
                process.StartInfo.FileName = $"\"{PathToFFmpegExe}\"";
                process.StartInfo.Environment["AV_LOG_FORCE_NOCOLOR"] = "TRUE";
                process.StartInfo.Arguments = "-hide_banner -loglevel quiet -nostats " + exportArgs;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                var errorRead = Task.Run(() =>
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        var readLine = process.StandardError.ReadLine();
                        queue.Enqueue(readLine);
                    }
                });

                var timeout = new TimeSpan(hours: 1, minutes: 0, seconds: 0);

                if (Task.WaitAll(new[] {errorRead}, timeout) &&
                    process.WaitForExit((int) timeout.TotalMilliseconds))
                {
                    if (process.ExitCode != 0)
                    {
                        Log.Log("FFmpeg failed to run");
                    }
                }
                else
                {
                    Log.Log($"FFmpeg timed out after waiting {timeout}");
                }
            }
            catch (Exception e)
            {
                Log.Log($"FFmpeg failed to run: {e.Message}", e);
            }
        }
    }
}