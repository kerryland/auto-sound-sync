using System.Diagnostics;
using System.IO;
using auxmic.wave;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;

namespace auxmic.sync
{
    /*
     * Create FingerPrints using https://github.com/AddictedCS/soundfingerprinting
     *
     * This should only be used for small files because it get steadily less accurate. Allegedly.
     *
     * See also FFmpegFingerPrinter which uses code from the same developer, but is faster and closed source.
     */
    public class SoundFingerprinter : IFingerprinter
    {
        private readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library
        
        public object CreateFingerPrints(Clip clip)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // The "SoundFingerPrinter" only supports 5512, so define it here to avoid an extra resample.
            var waveFormat = new NAudio.Wave.WaveFormat((int)  5512, 1); 
            string tempFilename = FileToWaveFile.CreateFast(clip.Filename, waveFormat);
            
            stopwatch.Stop();
            FingerprintStreamProvider.Log.Log($"sound ffmpeg took {stopwatch.Elapsed.TotalMilliseconds}"); // TODO: Fix awful logging
 
            stopwatch = Stopwatch.StartNew();
            
            var result = FingerprintCommandBuilder.Instance
                .BuildFingerprintCommand()
                .From(tempFilename)
                .UsingServices(audioService)
                .Hash()
                .Result;
            
            stopwatch.Stop();
            FingerprintStreamProvider.Log.Log($"sound fingerprint took {stopwatch.Elapsed.TotalMilliseconds}");

            return result;
        }
       
        public ClipMatch matchClips(Clip master, Clip lqClip)
        {
            lqClip.SetProgressMax(100);
            lqClip.ProgressValue = 25;
            
            var track = new TrackInfo(master.Filename, "Master", "Master");

            lqClip.ProgressValue = 50;

            IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
            modelService.Insert(track, (Hashes) master.Hashes);

            lqClip.ProgressValue = 75;

            var result = QueryFingerprintService.Instance
                .Query((Hashes) lqClip.Hashes, new DefaultQueryConfiguration(), modelService);

            modelService.DeleteTrack(master.Filename);
                
            lqClip.ProgressValue = 100;
            
            if (result.BestMatch == null)
            {
                return null;
            }
            return new ClipMatch(result.BestMatch.QueryMatchStartsAt, result.BestMatch.TrackMatchStartsAt, 
                result.BestMatch.TrackMatchStartsAt - result.BestMatch.QueryMatchStartsAt);
        }

        public void Cleanup(Clip clip)
        {
            File.Delete(FileCache.ComposeTempFilename(clip.Filename));
        }
    }
}