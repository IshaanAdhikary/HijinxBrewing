namespace Brewing.Rhythm
{
    /// <summary>
    /// Tracks song time against an <see cref="IAudioClock"/>, including pausing.
    /// Song time is negative during a lead-in, before the music starts.
    /// </summary>
    public sealed class SongTimer
    {
        readonly IAudioClock clock;
        double startTime;
        double pausedAt;

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        /// <summary>Clock time at which song time is zero.</summary>
        public double StartTime => startTime;

        public SongTimer(IAudioClock clock)
        {
            this.clock = clock;
        }

        public void Start(double startClockTime)
        {
            startTime = startClockTime;
            IsRunning = true;
            IsPaused = false;
        }

        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
        }

        public void Pause()
        {
            if (!IsRunning || IsPaused) return;
            IsPaused = true;
            pausedAt = clock.Now;
        }

        public void Resume()
        {
            if (!IsRunning || !IsPaused) return;
            startTime += clock.Now - pausedAt;
            IsPaused = false;
        }

        public double SongTime
        {
            get
            {
                if (!IsRunning) return 0.0;
                return (IsPaused ? pausedAt : clock.Now) - startTime;
            }
        }

        public double SongTimeAtRealtime(double realtimeSinceStartup)
        {
            if (!IsRunning) return 0.0;
            if (IsPaused) return pausedAt - startTime;
            return clock.RealtimeToClock(realtimeSinceStartup) - startTime;
        }
    }
}
