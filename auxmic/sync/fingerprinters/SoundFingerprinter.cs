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
            ISoundFile soundFile = clip.SoundFile;

            // if wave file already exists, do not create it again
            if (!FileCache.Exists(soundFile.TempFilename))
            {
                // TODO: Use clip.MasterWaveFormat not hardcoded 2 and 48000
                _launcher.ExecuteFFmpeg("-i " + clip.Filename + " -f wav -ac 2 -ar 48000 " + soundFile.TempFilename);
            }
            
            return FingerprintCommandBuilder.Instance
                .BuildFingerprintCommand()
                .From(soundFile.TempFilename)
                .UsingServices(audioService)
                .Hash()
                .Result;
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
            // nothing to do;
        }
    }
}