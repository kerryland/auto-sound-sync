using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using NAudio.Wave;

namespace auxmic
{
    public class SoundFile : IDisposable
    {
        // HRESULT: 0x20 (-2147024864)
        private const int ERROR_SHARING_VIOLATION = -2147024864;

        /// <summary>
        /// Имя исходного файла - может быть аудио- или видео-контейнер
        /// </summary>
        internal string Filename { get; set; }

        /// <summary>
        /// Имя извлечённого и ресемплированного файла WAV сохранённого во временной директории
        /// </summary>
        internal string TempFilename { get; set; }

        internal WaveFormat WaveFormat { get; set; }

        // Length in samples
        internal int DataLength { get; set; }

        // Length in bytes
        public long Length { get; set; }

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
        internal void SaveMatch(string filename, double queryMatchStartsAt, double trackMatchStartsAt, long length)
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

        /// <summary>
        /// Метод полностью повторяет <see cref="internal Int32[] Read(int samplesToRead)"/>, 
        /// за исключением возвращаемого значения, не Int32[], а сразу Complex[].
        /// Различаются только одной строкой: Complex[] result = new Complex[samplesToRead];
        /// Не получилось сделать через generics
        /// </summary>
        /// <param name="samplesToRead"></param>
        /// <returns></returns>
        internal Complex[] ReadComplex(WaveFileReader fileReader, int samplesToRead)
        {
            Complex[] result = new Complex[samplesToRead];

            int blockAlign = this.WaveFormat.BlockAlign;
            int channels = this.WaveFormat.Channels;

            byte[] buffer = new byte[blockAlign * samplesToRead];

            int bytesRead = fileReader.Read(buffer, 0, blockAlign * samplesToRead);

            for (int sample = 0; sample < bytesRead / blockAlign; sample++)
            {
                switch (this.WaveFormat.BitsPerSample)
                {
                    case 8:
                        result[sample] = (Int16) buffer[sample * blockAlign];
                        break;

                    case 16:
                        result[sample] = BitConverter.ToInt16(buffer, sample * blockAlign);
                        break;

                    case 32:
                        result[sample] = BitConverter.ToInt32(buffer, sample * blockAlign);
                        break;

                    default:
                        throw new NotSupportedException(String.Format(
                            "BitDepth '{0}' not supported. Try 8, 16 or 32-bit audio instead.",
                            this.WaveFormat.BitsPerSample));
                }
            }

            return result;
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