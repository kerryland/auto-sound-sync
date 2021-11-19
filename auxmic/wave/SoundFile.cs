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

            using (var
                fileReader =
                    new MediaFoundationReader(filename)) // needs its own "using" otherwise it doesn't get closed
            {
                this.WaveFormat = fileReader.WaveFormat;

                this.DataLength = (int) (fileReader.Length / this.WaveFormat.BlockAlign);

                this.Length = fileReader.Length;
            }
        }

        /// <summary>
        ///
        /// Creates a wave file containing a subset of the master audio file.
        ///
        /// TODO: Move into 'mediaExport' package, and replace to ffmpeg so we don't need a temp file.
        /// TODO: Currently exporting audio won't work if use FFmpegFingerprinter.
        /// 
        /// http://mark-dot-net.blogspot.ru/2009/09/trimming-wav-file-using-naudio.html
        /// </summary>
        /// <param name="filename"> description="Destination file name</param>
        /// <param name="queryMatchStartsAt" description="Location in low quality file where audio matches. bytes"></param>
        /// <param name="trackMatchStartsAt" description="Location in master file where audio begins. bytes"></param>
        /// <param name="length" description="Length of the LQ file in bytes"></param>
        public void SaveMatch(string filename, double queryMatchStartsAt, double trackMatchStartsAt, long length)
        {
            using (var reader = new WaveFileReader(this.TempFilename)) // TODO: Should be using master file audio
            using (var writer = new WaveFileWriter(filename, this.WaveFormat))
            {
                byte[] buffer = new byte[1024];

                var trackEndPosition = Math.Abs((queryMatchStartsAt * WaveFormat.AverageBytesPerSecond) - length);

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