using NAudio.Wave;

namespace auxmic
{
    public interface ISoundFile
    {
        string TempFilename { get; }
        int DataLength { get; }
        WaveFormat WaveFormat { get; }
        long Length { get; }
        string Filename { get; }
        void Dispose();
        
        // I don't really think this should live here, but it will do for now. I'm just trying to 
        // get to the point where it's easier to test Clip
        void SaveMatch(string filename, double matchResultQueryMatchStartsAt, double matchResultTrackMatchStartsAt, long soundFileLength);
    }
}