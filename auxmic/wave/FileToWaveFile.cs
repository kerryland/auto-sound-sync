using NAudio.Wave;

namespace auxmic.wave
{
    // Convert a media file to a temporary wave file (used for fingerprinting)
    public static class FileToWaveFile
    {
        public static string Create(string filename, WaveFormat waveFormat)
        {
            string tempFilename = FileCache.ComposeTempFilename(filename);

            // if such file already exists, do not create it again
            if (!FileCache.Exists(tempFilename))
            {
                ExtractAndResampleAudio(waveFormat, filename, tempFilename);
            }

            return tempFilename;
        }
        
        //  TODO: Is ffmpeg faster than WaveFileWriter?
        // _launcher.ExecuteFFmpeg("-i " + clip.Filename + " -f wav -ac 2 -ar 48000 " + tempFilename);
        private static void ExtractAndResampleAudio(WaveFormat resampleFormat, string filename, string tempFilename)
        {
            using (var reader = new MediaFoundationReader(filename))
            {
                if (NeedResample(reader.WaveFormat, resampleFormat))
                {
                    using (var resampler = new MediaFoundationResampler(reader, CreateOutputFormat(resampleFormat ?? reader.WaveFormat)))
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
        
        private static WaveFormat CreateOutputFormat(WaveFormat resampleFormat)
        {
            const int channels = 1;

            WaveFormat waveFormat = new WaveFormat(resampleFormat.SampleRate, resampleFormat.BitsPerSample, channels);

            return waveFormat;
        }
        
        private static bool NeedResample(WaveFormat inputFormat, WaveFormat resampleFormat)
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

            // BitsPerSample check does not seem to be needed
            return inputFormat.SampleRate != resampleFormat.SampleRate; /* || (inputFormat.BitsPerSample != resampleFormat.BitsPerSample)*/
        }
    }
}