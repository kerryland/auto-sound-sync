using NAudio.Wave;

namespace auxmic
{
    public interface ISoundFile
    {
        string TempFilename { get; }
        // Length in samples
        long DataLength { get; set; }
        WaveFormat WaveFormat { get; set; }
        // Length in bytes
        long Length { get; set;  }
        string Filename { get; }
        void Dispose();
    }
}