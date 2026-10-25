using System;
using System.Collections.Generic;

namespace Brewing.Pouring
{
    public enum PourLight { Red, Green }

    public readonly struct LightReading
    {
        public readonly PourLight State;

        /// <summary>Beats until the light next changes, or infinity if it never will.</summary>
        public readonly double BeatsUntilChange;

        public LightReading(PourLight state, double beatsUntilChange)
        {
            State = state;
            BeatsUntilChange = beatsUntilChange;
        }
    }

    /// <summary>The numbers behind the pouring visuals, kept separate so they can be tested.</summary>
    public static class PourMath
    {
        /// <summary>Green while a pour should be held, red otherwise. Windows must be in beat order.</summary>
        public static LightReading Light(double beat, IReadOnlyList<PourWindow> windows)
        {
            for (int i = 0; i < windows.Count; i++)
            {
                if (beat < windows[i].Start) return new LightReading(PourLight.Red, windows[i].Start - beat);
                if (beat < windows[i].End) return new LightReading(PourLight.Green, windows[i].End - beat);
            }
            return new LightReading(PourLight.Red, double.PositiveInfinity);
        }

        /// <summary>
        /// A bottle's horizontal position. It glides in from <paramref name="enterX"/> to the spout over
        /// <paramref name="approachBeats"/>, arriving on its start beat, waits there through the pour, then
        /// glides off to <paramref name="exitX"/> over <paramref name="exitBeats"/>. Returns false when it is
        /// off stage (before it starts approaching or after it has left).
        /// </summary>
        public static bool BottleX(double beat, PourWindow window, double approachBeats, double exitBeats,
            float spoutX, float enterX, float exitX, out float x)
        {
            double arrive = window.Start;
            double leave = window.End;

            if (beat < arrive - approachBeats || beat > leave + exitBeats)
            {
                x = beat < arrive ? enterX : exitX;
                return false;
            }

            if (beat < arrive)
            {
                float t = Smooth((float)((beat - (arrive - approachBeats)) / approachBeats));
                x = enterX + (spoutX - enterX) * t;
            }
            else if (beat <= leave)
            {
                x = spoutX;
            }
            else
            {
                float t = Smooth((float)((beat - leave) / exitBeats));
                x = spoutX + (exitX - spoutX) * t;
            }
            return true;
        }

        /// <summary>
        /// How many countdown pips to show before the light changes: one per beat left (rounded up), or none
        /// when the change is further off than <paramref name="warnBeats"/>.
        /// </summary>
        public static int CountdownPips(double beatsUntilChange, int warnBeats)
        {
            if (beatsUntilChange <= 0 || beatsUntilChange > warnBeats) return 0;
            return (int)Math.Ceiling(beatsUntilChange - 1e-9);
        }

        /// <summary>
        /// Brightness of the warning flash, from 1 exactly on each beat of the countdown fading to 0 over
        /// <paramref name="flashBeats"/>. The beats are counted back from the change, so the flashes land on
        /// the beats the player should count along to.
        /// </summary>
        public static float CountdownFlash(double beatsUntilChange, double flashBeats)
        {
            if (flashBeats <= 0) return 0f;
            double sinceBeat = Math.Ceiling(beatsUntilChange - 1e-9) - beatsUntilChange;
            return (float)Math.Max(0.0, Math.Min(1.0, 1.0 - sinceBeat / flashBeats));
        }

        /// <summary>How much a full pour of this window fills per beat, so holding exactly start to end reaches 1.</summary>
        public static double FillPerBeat(PourWindow window) => window.Length > 0 ? 1.0 / window.Length : 0.0;

        static float Smooth(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return t * t * (3f - 2f * t);
        }
    }
}
