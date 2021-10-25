namespace auxmic.sync
{
    /*
     * An IFingerPrinter class is able to create "fingerprints" for audio files
     * and use them to find a subset of one file in another file.
     */
    public interface IFingerprinter
    {
        object CreateFingerPrints(Clip clip);
        
        ClipMatch matchClips(Clip master, Clip lqClip);

        void Cleanup(Clip clip);

    }
}