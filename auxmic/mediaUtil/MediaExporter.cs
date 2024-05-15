namespace auxmic.mediaUtil
{
    /// <summary>
    /// Export media with audio from the master file
    /// </summary>
    public class MediaExporter : IMediaExporter
    {
        private FFmpegTool _launcher;

        /// <summary>
        /// Initialize path to FFmpeg 
        /// </summary>
        public MediaExporter(FFmpegTool _launcher)
        {
            this._launcher = _launcher;
        }

        /// <summary>
        /// Exports a media file containing high-quality audio.
        /// </summary>
        /// <param name="video">Should the target file contain the video?</param>
        /// <param name="videoFilePath">Source video file path</param>
        /// <param name="audioFilePath">High quality audio file path</param>
        /// <param name="queryMatchStartsAt">Where in the video file does the export video file begin</param>
        /// <param name="trackMatchStartsAt">Where in the audio file does the export media begin</param>
        /// <param name="targetFilePath">Target synced file path</param>
        /// <param name="duration">How long is the audio to export</param>
        public void Export(
            bool video,
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
            string exportArgs;
            if (video)
            {
                exportArgs =
                    // TODO: Optionally just copy the original audio
                    // $"-y -ss {trackStartInvariantCulture} -i \"{audioFilePath}\" -t {durationInvariantCulture} \"{targetFilePath}\"";

                exportArgs = 
                    $"-y " +
                    $"-ss {queryStartInvariantCulture} -i \"{videoFilePath}\" " +
                    $"-ss {trackStartInvariantCulture} -i \"{audioFilePath}\" " +
                    $"-c copy -map 0:v:0 -map 1:a:0 -t {durationInvariantCulture} \"{targetFilePath}\"";
            }
            else
            {
                exportArgs =
                    $"-y -ss {trackStartInvariantCulture} -i \"{audioFilePath}\" -t {durationInvariantCulture} \"{targetFilePath}\"";
            }

            _launcher.ExecuteFFmpeg(exportArgs);
        }
    }
}
