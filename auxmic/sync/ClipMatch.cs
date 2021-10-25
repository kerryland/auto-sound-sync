namespace auxmic
{
    public class ClipMatch
    {
        /// eg: Given HQ file that started recording at 2pm
        ///     and a LQ file that started recording at 1:55pm
        ///     Then the "QueryMatchStartsAt" will be 5 mins
        ///          and "TrackMatchStartsAt" will be 0.
        ///
        /// eg: Given HQ file that started recording at 2pm
        ///     and a LQ file that started recording at 2:10pm
        ///     Then the "QueryMatchStartsAt" will be 0 mins
        ///          and "TrackMatchStartsAt" will be 10 mins. (or minus 10 mins?)
        ///
        /// eg: Given HQ file that started recording at 2pm
        ///     and a LQ file that started recording at 2:10pm
        ///     Then the "QueryMatchStartsAt" will be 0 mins
        ///          and "TrackMatchStartsAt" will be 10 mins.
        ///
         
        /// Gets the exact position in seconds where resulting track started to match in the query
        public double QueryMatchStartsAt { get; }
        
        ///  Gets best guess in seconds where the LQ track is found in the HQ master audio. This value may be negative.
        public double TrackMatchStartsAt { get; }

        public ClipMatch(double queryMatchStartsAt, double trackMatchStartsAt)
        {
            QueryMatchStartsAt = queryMatchStartsAt;
            TrackMatchStartsAt = trackMatchStartsAt;
        }
    }
}