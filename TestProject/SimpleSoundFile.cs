using auxmic;
using NAudio.Wave;

namespace TestProject
{
    public class SimpleSoundFile : ISoundFile
    {
        public SimpleSoundFile WithDataLength(int dataLength)
        {
            DataLength = dataLength;
            return this;
        }
        
        public SimpleSoundFile WithLength(long length)
        {
            Length = length;
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

        public int DataLength { get; private set; }

        public WaveFormat WaveFormat { get; set; }

        public long Length { get; private set; }

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