namespace auxmic.mediaUtil
{
    public class MediaProperties
    {
        public string Par { get; }
        public bool IsVideo { get; }
        public int Width { get; }
        public int Height { get; }
        public float FrameRate { get; }
        public float TimeBase { get; }

        public MediaProperties(bool isVideo, int width, int height, string par, float frameRate, float timeBase)
        {
            Par = par;
            FrameRate = frameRate;
            TimeBase = timeBase;
            IsVideo = isVideo;
            Width = width;
            Height = height;
        }
    }
}