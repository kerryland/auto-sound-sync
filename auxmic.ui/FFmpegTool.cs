using System;
using System.Collections.Concurrent;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using auxmic.logging;

namespace auxmic.ui
{
    /// <summary>
    /// FFmpeg wrapper
    /// </summary>
    internal class FFmpegTool
    {
        private readonly AuxMicLog Log;

        /// <summary>
        /// Full path to FFmpeg executable file `ffmpeg.exe`
        /// </summary>
        private string FFmpegExe { get; }

        /// <summary>
        /// Initialize path to FFmpeg executable
        /// </summary>
        /// <param name="pathToFFmpegExe">Full path to FFmpeg executable file `ffmpeg.exe`</param>
        public FFmpegTool(string pathToFFmpegExe, AuxMicLog log)
        {
            Log = log;
            FFmpegExe = pathToFFmpegExe;
        }

        /// <summary>
        /// Exports shortest synced file.
        /// </summary>
        /// <param name="videoFilePath">Source video file path</param>
        /// <param name="audioFilePath">Audio file path</param>
        /// <param name="targetFilePath">Target synced file path</param>
        /// <param name="duration"></param>
        internal void Export(
            string videoFilePath,
            string audioFilePath,
            double queryMatchStartsAt,
            double trackMatchStartsAt,
            string targetFilePath,
            double duration)
        {
            if (!File.Exists(this.FFmpegExe))
            {
                throw new ApplicationException(
                    "Full path to FFmpeg executable file `ffmpeg.exe` is not set or not correct.");
            }

            string queryStartInvariantCulture =
                queryMatchStartsAt.ToString("g", System.Globalization.CultureInfo.InvariantCulture);
            string trackStartInvariantCulture =
                trackMatchStartsAt.ToString("g", System.Globalization.CultureInfo.InvariantCulture);
            string durationInvariantCulture = duration.ToString("g", System.Globalization.CultureInfo.InvariantCulture);

            // command template to export shortest synced video
            // check https://ffmpeg.org/ffmpeg.html for params:
            // -ss position(input / output)
            string exportArgs =
                $"-y -loglevel levelwarning -ss {queryStartInvariantCulture} -i \"{videoFilePath}\" -ss {trackStartInvariantCulture} -i \"{audioFilePath}\" -c copy -map 0:v:0 -map 1:a:0 -t {durationInvariantCulture} \"{targetFilePath}\"";

            ExecuteFFmpeg(exportArgs);
        }

        private void ExecuteFFmpeg(string exportArgs)
        {
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
                process.StartInfo.FileName = $"\"{FFmpegExe}\"";
                process.StartInfo.Environment["AV_LOG_FORCE_NOCOLOR"] = "TRUE";
                
                process.StartInfo.Arguments = exportArgs;
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
                    else
                    {
                        Log.Log("FFmpeg processing completed successfully");
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