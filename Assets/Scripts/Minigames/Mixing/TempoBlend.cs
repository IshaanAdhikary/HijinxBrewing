using System;
using System.Collections.Generic;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    public readonly struct BlendState
    {
        public readonly TempoMode From;
        public readonly TempoMode To;

        /// <summary>0 shows <see cref="From"/>, 1 shows <see cref="To"/>.</summary>
        public readonly float T;

        public BlendState(TempoMode from, TempoMode to, float t)
        {
            From = from;
            To = to;
            T = t;
        }
    }

    /// <summary>
    /// Decides how far the brew has blended toward the next section's color. The blend finishes exactly
    /// when the next section starts, so the new color is the player's warning of the coming tempo.
    /// </summary>
    public static class TempoBlend
    {
        public static BlendState At(double beat, IReadOnlyList<ChartSection> sections, double telegraphBeats)
        {
            if (sections == null || sections.Count == 0)
                return new BlendState(TempoMode.Normal, TempoMode.Normal, 0f);

            int current = 0;
            for (int i = 0; i < sections.Count; i++)
                if (sections[i].startBeat <= beat) current = i;

            var from = sections[current].mode;
            if (current + 1 >= sections.Count) return new BlendState(from, from, 0f);

            double boundary = sections[current + 1].startBeat;
            double windowStart = Math.Max(boundary - telegraphBeats, sections[current].startBeat);
            if (beat < windowStart || boundary <= windowStart) return new BlendState(from, from, 0f);

            float t = (float)Math.Min(1.0, (beat - windowStart) / (boundary - windowStart));
            return new BlendState(from, sections[current + 1].mode, t);
        }
    }
}
