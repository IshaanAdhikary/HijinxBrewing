using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>A monotonic clock in seconds, abstracted so the timing code can be tested with fake time.</summary>
    public interface IAudioClock
    {
        double Now { get; }

        /// <summary>Converts a Time.realtimeSinceStartup timestamp (such as an input event time) onto this clock.</summary>
        double RealtimeToClock(double realtimeSinceStartup);
    }

    /// <summary>
    /// The audio system clock. AudioSettings.dspTime only advances once per audio buffer, so reading it
    /// raw gives jittery timing. This extrapolates with realtime between buffer ticks and never goes backwards.
    /// </summary>
    public sealed class DspClock : IAudioClock
    {
        double lastDsp = double.NaN;
        double lastDspRealtime;
        double lastResult;

        public double Now
        {
            get
            {
                double dsp = AudioSettings.dspTime;
                double real = Time.realtimeSinceStartupAsDouble;
                if (dsp != lastDsp)
                {
                    lastDsp = dsp;
                    lastDspRealtime = real;
                }

                double t = lastDsp + (real - lastDspRealtime);
                if (t < lastResult) t = lastResult;
                lastResult = t;
                return t;
            }
        }

        public double RealtimeToClock(double realtimeSinceStartup)
        {
            return Now - (Time.realtimeSinceStartupAsDouble - realtimeSinceStartup);
        }
    }

    /// <summary>A clock you advance by hand, for tests.</summary>
    public sealed class ManualClock : IAudioClock
    {
        public double Time { get; set; }
        public double Now => Time;
        public double RealtimeToClock(double realtimeSinceStartup) => realtimeSinceStartup;
    }
}
