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
     * This should only be used for small files because it get steadily less accurate.
     *
     * I will eventually replace this with FFmepgFingerPrinter.
     */
    public class SoundFingerprinter : IFingerprinter
    {
        private readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library
        
        public object CreateFingerPrints(Clip clip)
        {
            SoundFile soundFile = clip.SoundFile;

            // This also builds the .wav file automatically if it doesn't exist.
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