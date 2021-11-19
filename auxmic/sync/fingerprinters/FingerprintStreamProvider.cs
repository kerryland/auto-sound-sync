using System.IO;
using auxmic.mediaUtil;
using NAudio.Wave;

namespace auxmic.sync
{
    // Provide AuxMicFingerprinter with a stream of audio data that it can extract fingerprints from.
    // This is usually a WAV file, but we have a few different ways to create the wave file.
    public interface FingerprintStreamProvider
    {
        Stream GetStream(Clip clip);
    }

    // Get WAV data via capturing stdout pipe from ffmpeg
    // This logic means there is no intermediate file, so
    // I had expected it to be fastest. It isn't.
    public class PipedWaveProvider : FingerprintStreamProvider
    {
        public Stream GetStream(Clip clip)
        {
            return new VideoWave(clip.Filename);
        }
    }

    // Create a WAVE file using ffmpeg. Even though we are spawning an
    // external process it's still extremely fast. 
    public class FFmpegWaveFile : FingerprintStreamProvider
    {
        private readonly FFmpegTool _launcher;

        public FFmpegWaveFile(FFmpegTool _launcher)
        {
            this._launcher = _launcher;
        }

        public Stream GetStream(Clip clip)
        {
            var tempFilename = FileCache.ComposeTempFilename(clip.Filename);

            // if such file already exists, do not create it again
            if (!FileCache.Exists(tempFilename))
            {
                _launcher.ExecuteFFmpeg("-i " + clip.Filename + " -f wav -ac 2 -ar 48000 " + tempFilename);
            }

            return File.OpenRead(tempFilename);
        }
    }
    
    // Create a WAVE file using the Naudio library. This is the logic traditionally used in AuxMic.
    // It also caches the WAVE file in case we need it later, but if we have cached the fingerprints
    // I don't know why we need the WAVE file too.
    public class NaudioWavefile : FingerprintStreamProvider
    {
        public Stream GetStream(Clip clip)
        {
            var tempFilename = FileCache.ComposeTempFilename(clip.Filename);

            // if such file already exists, do not create it again
            if (!FileCache.Exists(tempFilename))
            {
                ExtractAndResampleAudio(clip.MasterWaveFormat, clip.Filename, tempFilename);
            }

            return File.OpenRead(tempFilename);
        }
        
        private void ExtractAndResampleAudio(WaveFormat resampleFormat, string filename, string tempFilename)
        {
            using (var reader = new MediaFoundationReader(filename))
            {
                if (NeedResample(reader.WaveFormat, resampleFormat))
                {
                    using (var resampler = new MediaFoundationResampler(reader,
                        CreateOutputFormat(resampleFormat ?? reader.WaveFormat)))
                    {
                        WaveFileWriter.CreateWaveFile(tempFilename, resampler);
                    }
                }
                else
                {
                    WaveFileWriter.CreateWaveFile(tempFilename, reader);
                }
            }
        }

        private bool NeedResample(WaveFormat inputFormat, WaveFormat resampleFormat)
        {
            // даже если resampleFormat не задан, необходимо проверять
            // сколько каналов в файле и ресемплировать до 1 канала
            if (inputFormat.Channels > 2)
            {
                return true;
            }

            if (resampleFormat == null)
            {
                return false;
            }

            // TODO: test if BitsPerSample check needed
            return inputFormat.SampleRate != resampleFormat.SampleRate; /* || (inputFormat.BitsPerSample != resampleFormat.BitsPerSample)*/
        }

        private WaveFormat CreateOutputFormat(WaveFormat resampleFormat)
        {
            int channels = 1;

            WaveFormat waveFormat = new WaveFormat(resampleFormat.SampleRate, resampleFormat.BitsPerSample, channels);

            return waveFormat;
        }
    }
}