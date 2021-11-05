using System;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;

namespace auxmic
{
    public class SoundFile : IDisposable, ISoundFile
    {
        // HRESULT: 0x20 (-2147024864)
        private const int ERROR_SHARING_VIOLATION = -2147024864;

        /// <summary>
        /// Имя исходного файла - может быть аудио- или видео-контейнер
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Имя извлечённого и ресемплированного файла WAV сохранённого во временной директории
        /// </summary>
        public string TempFilename { get;  }

        public WaveFormat WaveFormat { get; private set; }

        // Length in samples
        public int DataLength { get; private set; }

        // Length in bytes
        public long Length { get; private set; }

        internal SoundFile(string filename, WaveFormat resampleFormat)
        {
            this.Filename = filename;
            this.TempFilename = FileCache.ComposeTempFilename(filename);

            // if such file already exists, do not create it again
            if (!FileCache.Exists(this.TempFilename))
            {
                ExtractAndResampleAudio(resampleFormat);
            }

            ReadWave(this.TempFilename);
        }

        private void ReadWave(string filename)
        {
            Debug.Assert(filename != null, nameof(filename) + " != null");

            using (var fs = new FileStream(this.TempFilename, FileMode.Open, FileAccess.Read)) // needs its own "using" otherwise it doesn't get closed
            using (var fileReader = new WaveFileReader(fs))
            {
                this.WaveFormat = fileReader.WaveFormat;
                
                this.DataLength = (int) (fileReader.Length / this.WaveFormat.BlockAlign);
                
                this.Length = fileReader.Length;
            }
        }

        private void ExtractAndResampleAudio(WaveFormat resampleFormat)
        {
            using (var reader = new MediaFoundationReader(this.Filename))
            {
                if (NeedResample(reader.WaveFormat, resampleFormat))
                {
                    using (var resampler = new MediaFoundationResampler(reader,
                        CreateOutputFormat(resampleFormat ?? reader.WaveFormat)))
                    {
                        WaveFileWriter.CreateWaveFile(this.TempFilename, resampler);
                    }
                }
                else
                {
                    WaveFileWriter.CreateWaveFile(this.TempFilename, reader);
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

        /// <summary>
        /// http://mark-dot-net.blogspot.ru/2009/09/trimming-wav-file-using-naudio.html
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="queryMatchStartsAt" description="Location in low quality file where audio matches. bytes"></param>
        /// <param name="trackMatchStartsAt" description="Location in master file where audio begins. bytes"></param>
        /// <param name="length" description="Length of the LQ file in bytes"></param>
        public void SaveMatch(string filename, double queryMatchStartsAt, double trackMatchStartsAt, long length)
        {
            using (var reader = new WaveFileReader(this.TempFilename))
            using (var writer = new WaveFileWriter(filename, this.WaveFormat))
            {
                byte[] buffer = new byte[1024];

                var trackEndPosition  = Math.Abs((queryMatchStartsAt * WaveFormat.AverageBytesPerSecond) - length);

                var trackStartPosition = trackMatchStartsAt * WaveFormat.AverageBytesPerSecond;
                reader.Position = (long) trackStartPosition;

                while (writer.Position < trackEndPosition)
                {
                    int bytesRead = reader.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }


        //~SoundFile()
        //{
        //    Dispose();
        //}

        // TODO: Check releasing FileReader - test are not run well - file locked!
        // No need to release FileReader anymore. I found the bug :-)
        public void Dispose()
        {
            if (File.Exists(TempFilename))
            {
                try
                {
                    File.Delete(TempFilename);
                }
                catch (IOException ex)
                {
                    // if the file is used by another instance, do not throw exception
                    if (ex.HResult != ERROR_SHARING_VIOLATION)
                    {
                        throw;
                    }
                }
            }
        }
    }
}