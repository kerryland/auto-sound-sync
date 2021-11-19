using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using auxmic.logging;

namespace auxmic
{
    // A WAV stream created from any ffmpeg-compatible film, without an intermediate disk file
    public class VideoWave : Stream
    {
        private readonly StreamReader ffout;
        private static string _pathToFFmpegExe;
        private static AuxMicLog _log;
        // private long durationMs;

        // public long DurationMs => durationMs;

        public static string PathToFFmpegExe
        {
            set => _pathToFFmpegExe = value;
        }

        public static AuxMicLog Log
        {
            set => _log = value;
        }

        private readonly Process ffmpeg;

        private class ByteMe
        {
            private readonly byte[] bytes;
            private readonly int read;

            public byte[] Bytes => bytes;

            public int Read1 => read;

            public ByteMe(byte[] bytes, int read)
            {
                this.bytes = bytes;
                this.read = read;
            }
        }

        private readonly BlockingCollection<ByteMe> dataItems = new BlockingCollection<ByteMe>(1024);

        // Creates a WAV stream converted from the provided video file
        public VideoWave(string filename)
        {
            ffmpeg = new Process();
            ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = _pathToFFmpegExe,
                RedirectStandardOutput = true, // outputs raw video
                RedirectStandardError = true, // should be hidden
                UseShellExecute = false,
                CreateNoWindow = true,  
                // WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = "-i \"" + filename + "\" -f wav -ac 2 -ar 48000 pipe:1"
            };

            ffmpeg.StartInfo = ffmpegStartInfo;
            ffmpeg.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    Console.WriteLine(args.Data);
                }
            };

            ffmpeg.Start();
            ffout = ffmpeg.StandardOutput;
            ffmpeg.BeginErrorReadLine();

            Task.Run(() =>
            {
                try
                {
                    byte[] buffer = new byte[8192]; // Max seems to be 6144 in the real world
                    int read;
                    
                    do
                    {
                        read = ffout.BaseStream.Read(buffer, 0, buffer.Length);
                        byte[] clone = new byte[read];
                        Buffer.BlockCopy(buffer, 0, clone, 0, read * sizeof(byte));
                        
                        ByteMe bm = new ByteMe(clone, read);
                        dataItems.Add(bm);
                        
                    } while ((!ffout.EndOfStream && read != 0));

                    dataItems.CompleteAdding();
                    ffmpeg.WaitForExit();
                }
                catch (Exception e)
                {
                    _log.Log("ERROR", e);
                }
            });
        }

        public override void Flush()
        {
            ffout.BaseStream.Flush();
        }

        private byte[] localBuffer;
        private int localBufferPointer;
        private int localBufferLength;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;

            while (read < count)
            {
                if (localBufferPointer == 0)
                {
                    ByteMe myByte;

                    if (dataItems.TryTake(out myByte, 10000))
                    {
                        localBufferLength = myByte.Read1;
                        localBuffer = myByte.Bytes;
                    }
                    else
                    {
                        break;
                    }
                }

                int bytesToRead = Math.Min(localBufferLength - localBufferPointer, count-read);

                if (bytesToRead != 0)
                {
                    Buffer.BlockCopy(localBuffer, localBufferPointer, buffer, read * sizeof(byte),
                        bytesToRead * sizeof(byte));

                    read += bytesToRead;

                    localBufferPointer += bytesToRead;
                }

                if (localBufferPointer == localBufferLength)
                {
                    localBufferPointer = 0;
                }
            }

            return read;
        }

        public override void Close()
        {
            ffmpeg.Close();
            base.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotSupportedException();
        }

        public override bool CanRead
        {
            get { return ffout.BaseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new System.NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new System.NotSupportedException(); }

            set { throw new System.NotSupportedException(); }
        }
   }
}