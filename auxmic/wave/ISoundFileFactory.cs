using NAudio.Wave;

namespace auxmic
{
    public interface ISoundFileFactory
    {
        ISoundFile CreateSoundFile(string filename, WaveFormat resampleFormat);
    }
}