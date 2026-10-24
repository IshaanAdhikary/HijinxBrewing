namespace Brewing.Rhythm
{
    /// <summary>Converts between song time (seconds since the clip started) and beats.</summary>
    public readonly struct BeatClock
    {
        public readonly double Bpm;

        /// <summary>Seconds from the start of the audio clip to beat 0.</summary>
        public readonly double FirstBeatOffset;

        public BeatClock(double bpm, double firstBeatOffset)
        {
            Bpm = bpm;
            FirstBeatOffset = firstBeatOffset;
        }

        public double SecondsPerBeat => 60.0 / Bpm;

        public double BeatToSongTime(double beat) => FirstBeatOffset + beat * SecondsPerBeat;

        public double SongTimeToBeat(double songTime) => (songTime - FirstBeatOffset) / SecondsPerBeat;
    }
}
