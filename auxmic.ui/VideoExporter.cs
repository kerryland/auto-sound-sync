using auxmic.mediaUtil;

namespace auxmic.ui
{
    /// <summary>
    /// Export videos with audio from the master file
    ///
    /// TODO: Move out of UI project and into 'mediaExport' package
    /// </summary>
    internal class VideoExporter
    {
        private FFmpegTool _launcher;

        /// <summary>
        /// Initialize path to FFmpeg 
        /// </summary>
        public VideoExporter(FFmpegTool _launcher)
        {
            this._launcher = _launcher;
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
            string queryStartInvariantCulture =
                queryMatchStartsAt.ToString("g", System.Globalization.CultureInfo.InvariantCulture);
            string trackStartInvariantCulture =
                trackMatchStartsAt.ToString("g", System.Globalization.CultureInfo.InvariantCulture);
            string durationInvariantCulture = duration.ToString("g", System.Globalization.CultureInfo.InvariantCulture);

            // command template to export synced video
            // check https://ffmpeg.org/ffmpeg.html for params:
            // -ss position(input / output)
            string exportArgs =
                $"-y -ss {queryStartInvariantCulture} -i \"{videoFilePath}\" -ss {trackStartInvariantCulture} -i \"{audioFilePath}\" -c copy -map 0:v:0 -map 1:a:0 -t {durationInvariantCulture} \"{targetFilePath}\"";

            _launcher.ExecuteFFmpeg(exportArgs);
        }
    }
}