using auxmic;
using NAudio.Wave;

namespace TestProject
{
    public class SimpleSoundFile : ISoundFile
    {
        
        public SimpleSoundFile WithDurationInSeconds(double durationInSeconds)
        {
            Length = (long) (durationInSeconds * WaveFormat.AverageBytesPerSecond);
            DataLength = (int) (Length / WaveFormat.BlockAlign);

            return this;
        }
        
        public SimpleSoundFile WithFilename(string filename)
        {
            Filename = filename;
            return this;
        }
        public SimpleSoundFile WithWaveFormat(WaveFormat waveFormat)
        {
            WaveFormat = waveFormat;
            return this;
        }

        public string TempFilename
        {
            get
            {
                return Filename + ".tmp";
            }
        }

        public long DataLength { get; set; }

        public WaveFormat WaveFormat { get; set; }

        public long Length { get; set; }

        public string Filename { get; set; }
        
        
        public void Dispose()
        {
            // noop
        }

        public void SaveMatch(string filename, double matchResultQueryMatchStartsAt, double matchResultTrackMatchStartsAt,
            long soundFileLength)
        {
            // noop
        }

    }
}