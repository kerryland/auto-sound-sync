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
        public string TempFilename { get; }

        public WaveFormat WaveFormat { get; set; }

        // Length in samples
        public long DataLength { get; set; }

        // Length in bytes
        public long Length { get; set; }

        internal SoundFile(string filename)
        {
            this.Filename = filename;
            this.TempFilename = FileCache.ComposeTempFilename(filename);

            ReadWave(this.Filename);
        }

        private void ReadWave(string filename)
        {
            Debug.Assert(filename != null, nameof(filename) + " != null");

            using (var fileReader = new MediaFoundationReader(filename))
            {
                this.WaveFormat = fileReader.WaveFormat;
                this.DataLength = (int) (fileReader.Length / this.WaveFormat.BlockAlign);
                this.Length = fileReader.Length;
            }
        }

        public void Dispose()
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