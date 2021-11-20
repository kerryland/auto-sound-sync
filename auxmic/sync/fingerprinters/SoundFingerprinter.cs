using System.Diagnostics;
using System.IO;
using auxmic.mediaUtil;
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
        public static FFmpegTool _launcher;

        private readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library
        
        public object CreateFingerPrints(Clip clip)
        {
            // string tempFilename = FileToWaveFile.Create(clip.Filename, clip.WaveFormat);

            string tempFilename = FileCache.ComposeTempFilename(clip.Filename);

            Stopwatch stopwatch = Stopwatch.StartNew();

           
            // if wave file already exists, do not create it again
            if (!FileCache.Exists(tempFilename))
            {
                if (clip.MasterWaveFormat != null)
                {
                    // TODO: Use FileToWaveFile.cs OR FingerprintStreamProvider 
                    _launcher.ExecuteFFmpeg(
                        "-i " + clip.Filename + " -f wav -ac " + clip.MasterWaveFormat.Channels +
                        " -ar " + clip.MasterWaveFormat.SampleRate + " " + tempFilename); 
                }
                else
                {
                    _launcher.ExecuteFFmpeg(
                        "-i " + clip.Filename + " -f wav -ac 1 -ar 5512 " + tempFilename);
                }
            }
            stopwatch.Stop();
            FingerprintStreamProvider.Log.Log($"sound ffmpeg took {stopwatch.Elapsed.TotalMilliseconds}");
 
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