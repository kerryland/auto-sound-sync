namespace auxmic
{
    public class ClipMatch
    {
        /// eg: Given HQ file that started recording at 2pm
        ///     and a LQ file that started recording at 1:55pm
        ///     Then the "QueryMatchStartsAt" will be 5 mins
        ///          and "TrackMatchStartsAt" will be 0.
        ///          and "Offset" will be -5 minutes
        ///
        /// eg: Given HQ file that started recording at 2pm
        ///     and a LQ file that started recording at 2:10pm
        ///     Then the "QueryMatchStartsAt" will be 0 mins
        ///          and "TrackMatchStartsAt" will be 10 mins.
        ///          and "Offset" will be 10 minutes
        ///
         
        /// Gets the exact position in seconds where resulting track started to match in the query
        public double QueryMatchStartsAt { get; }
        
        ///  Gets best guess in seconds where the LQ track is found in the HQ master audio. This value may be negative.
        public double TrackMatchStartsAt { get; }
        
        // How many seconds between the start of the Query track and the start of the Matching track. Can be negative.
        public double Offset { get; }
        
        public ClipMatch(double queryMatchStartsAt, double trackMatchStartsAt, double offset)
        {
            QueryMatchStartsAt = queryMatchStartsAt;
            TrackMatchStartsAt = trackMatchStartsAt;
            Offset = offset;
        }
    }
}