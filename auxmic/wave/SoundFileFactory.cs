using NAudio.Wave;

namespace auxmic
{
    public class SoundFileFactory : ISoundFileFactory
    {
        public ISoundFile CreateSoundFile(string filename, WaveFormat resampleFormat)
        {
            return new SoundFile(filename, resampleFormat);
        }
    }
}