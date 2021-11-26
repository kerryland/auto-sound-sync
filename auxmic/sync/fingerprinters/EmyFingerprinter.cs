using System;
using System.Diagnostics;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;

namespace auxmic.sync
{
    /*
     * Create FingerPrints using "Emy" FFmpegAudioService https://github.com/AddictedCS/soundfingerprinting
     */
    public class EmyFingerprinter : IFingerprinter
    {
        private readonly IAudioService audioService = new FFmpegAudioService(); // fast and accurate audio library
        
        public object CreateFingerPrints(Clip clip)
        {
            // TODO: Handle better
            Debug.WriteLine("Put ffmpeg into " + Environment.CurrentDirectory + "/bin/" + (Environment.Is64BitProcess ? "x64" : "x86"));
            // or set ffmpeg.RootPath = path using FFmpeg.AutoGen; ;

            // ISoundFile soundFile = clip.SoundFile;

            Stopwatch stopwatch = Stopwatch.StartNew();
            
            // This processes the source file without using an intermediate .wav file
            var result = FingerprintCommandBuilder.Instance
                .BuildFingerprintCommand()
                .From(clip.Filename)
                .UsingServices(audioService)
                .Hash()
                .Result;
            
            stopwatch.Stop();
            FingerprintStreamProvider.Log.Log($"emy fingerprint took {stopwatch.Elapsed.TotalMilliseconds}");

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
            // nothing to do;
        }
    }
}