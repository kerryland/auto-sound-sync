namespace auxmic.mediaUtil
{
    public interface IMediaExporter
    {
        public void Export(
            bool video,
            string videoFilePath,
            string audioFilePath,
            double queryMatchStartsAt,
            double trackMatchStartsAt,
            string targetFilePath,
            double duration);
    }
}