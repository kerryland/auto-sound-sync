using auxmic.mediaUtil;
using NAudio.Wave;

namespace auxmic.wave
{
    // Convert a media file to a temporary wave file (used for fingerprinting)
    public static class FileToWaveFile
    {
        public static FFmpegTool FFmpegTool;
        
        // Create a temporary WAV file
        public static string CreateFast(string filename, WaveFormat waveFormat)
        {
            var tempFilename = FileCache.ComposeTempFilename(filename);

            // if such file already exists, do not create it again
            if (!FileCache.Exists(tempFilename))
            {
                using (var reader = new MediaFoundationReader(filename))
                {
                    var exportFormat = NeedResample(reader.WaveFormat, waveFormat) 
                        ? waveFormat : reader.WaveFormat;

                    FFmpegTool.ExecuteFFmpeg(
                        "-i \"" + filename + "\" -f wav -ac " + exportFormat.Channels +
                        " -ar " + exportFormat.SampleRate + " \"" + tempFilename + "\"");
                }
            }

            return tempFilename;
        }
        
        public static string CreateSlow(string filename, WaveFormat waveFormat)
        {
            string tempFilename = FileCache.ComposeTempFilename(filename);

            // if such file already exists, do not create it again
            if (!FileCache.Exists(tempFilename))
            {
                ExtractAndResampleAudio(waveFormat, filename, tempFilename);
            }

            return tempFilename;
        }
        
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
        
        public static bool NeedResample(WaveFormat inputFormat, WaveFormat resampleFormat)
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