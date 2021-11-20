using System;
using System.Diagnostics;
using System.IO;
using auxmic.logging;
using auxmic.mediaUtil;
using auxmic.wave;
using static auxmic.sync.FingerprintStreamProvider;

namespace auxmic.sync
{
    // Provide AuxMicFingerprinter with a stream of audio data that it can extract fingerprints from.
    // This is usually a WAV file, but we have a few different ways to create the wave file.
    public interface FingerprintStreamProvider
    {
        Stream GetStream(Clip clip);

        public static AuxMicLog Log;
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
                // TODO: Don't hardcode 2 and 48000
                Stopwatch stopwatch = Stopwatch.StartNew();
                _launcher.ExecuteFFmpeg("-i " + clip.Filename + " -f wav -ac 2 -ar 48000 " + tempFilename);
                stopwatch.Stop();
                Log.Log($"ffmpeg took {stopwatch.Elapsed.TotalMilliseconds}");
            }

            return File.OpenRead(tempFilename);
        }
    }
    
    // Create a WAVE file using the Naudio library. This is the logic traditionally used in AuxMic.
    // It also caches the WAVE file in case we need it later, but if we have cached the fingerprints
    // I don't know why we need the WAVE file too.
    public class NaudioWavefile : FingerprintStreamProvider, IDisposable
    {
        private string tempFilename;
        
        public Stream GetStream(Clip clip)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var tempFilename = FileToWaveFile.Create(clip.Filename, clip.MasterWaveFormat); 
            stopwatch.Stop();
            Log.Log($"naudio took {stopwatch.Elapsed.TotalMilliseconds}");

            return File.OpenRead(tempFilename);
        }

        public void Dispose()
        {
            // File.Delete(tempFilename);
        }
    }
}