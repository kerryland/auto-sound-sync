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
        
        // I don't really think this should live here, but it will do for now. I'm just trying to 
        // get to the point where it's easier to test Clip.cs
        void SaveMatch(string filename, double matchResultQueryMatchStartsAt, double matchResultTrackMatchStartsAt, long soundFileLength);
    }
}