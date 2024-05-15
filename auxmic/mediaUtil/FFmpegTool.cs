
using auxmic.logging;

namespace auxmic.mediaUtil
{
    // Launch the ffmpeg executable and capture its output nicely.
    public class FFmpegTool
    {
        private CommandLineCapturer _capturer;
        
        public FFmpegTool(AuxMicLog log, string exeName)
        {
            _capturer = new CommandLineCapturer(log, exeName, "ffmpeg", "-hide_banner -nostats");
        }

        public void ExecuteFFmpeg(string args)
        {
            _capturer.Execute(args);
        }
    }
}