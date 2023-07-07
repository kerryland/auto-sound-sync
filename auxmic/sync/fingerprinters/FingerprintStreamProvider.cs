using System;
using System.Diagnostics;
using System.IO;
using auxmic.logging;
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
    // it's much faster than using the nAudio library.
    public class PipedWaveProvider : FingerprintStreamProvider
    {
        public Stream GetStream(Clip clip)
        {
            return new FileToWaveStream(clip.Filename, clip.WaveFormat, clip.MasterWaveFormat);
        }
    }

    // Create a WAVE file using ffmpeg. Even though we are spawning an
    // external process it's still extremely fast. 
    public class FFmpegWaveFile : FingerprintStreamProvider, IDisposable
    {
        private string tempFilename;

        public Stream GetStream(Clip clip)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            tempFilename = FileToWaveFile.CreateFast(clip.Filename, null); 
            stopwatch.Stop();
            Log.Log($"ffmpeg took {stopwatch.Elapsed.TotalMilliseconds}");

            return File.OpenRead(tempFilename);
        }
        
        public void Dispose()
        {
            File.Delete(tempFilename);
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
            tempFilename = FileToWaveFile.CreateSlow(clip.Filename, clip.MasterWaveFormat); 
            // tempFilename = FileToWaveFile.CreateFast(clip.Filename, clip.MasterWaveFormat); 
            stopwatch.Stop();
            Log.Log($"wav create in {stopwatch.Elapsed.TotalMilliseconds}");

            return File.OpenRead(tempFilename);
        }

        public void Dispose()
        {
            File.Delete(tempFilename);
        }
    }
}