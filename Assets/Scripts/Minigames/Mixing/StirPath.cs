using System;
using System.Collections.Generic;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>
    /// Where the spoon is at any beat. Each action sends the spoon on to the next stop of the stir: it leaves
    /// on the beat of the action and arrives on the beat of the next one. So pressing left sets off for the
    /// left edge, which it reaches just as you release, and so on round the cauldron (bottom, left, top,
    /// right, bottom). Angles are degrees, 0 = right, 90 = top, -90 = bottom, and keep decreasing as the
    /// spoon goes round (so -450 is the bottom again after one full stir).
    /// </summary>
    public sealed class StirPath
    {
        const double BottomDegrees = -90.0;
        const double StepDegrees = -90.0;

        readonly double[] beats;
        readonly int[] indexInSection;
        readonly double[] travelEnd;

        public StirPath(IReadOnlyList<RhythmEvent> events, IReadOnlyList<ChartSection> sections)
        {
            int n = events.Count;
            beats = new double[n];
            indexInSection = new int[n];
            travelEnd = new double[n];

            for (int i = 0; i < n; i++)
            {
                beats[i] = events[i].Beat;
                bool continuesSection = i > 0 && events[i - 1].Group == events[i].Group;
                indexInSection[i] = continuesSection ? indexInSection[i - 1] + 1 : 0;
            }

            for (int i = 0; i < n; i++)
            {
                bool nextInSameSection = i + 1 < n && events[i + 1].Group == events[i].Group;
                travelEnd[i] = nextInSameSection
                    ? beats[i + 1]
                    : beats[i] + sections[events[i].Group].mode.EventSpacingBeats();
            }
        }

        public double AngleAt(double beat)
        {
            int n = beats.Length;
            if (n == 0) return BottomDegrees;

            int i = LastEventAtOrBefore(beat);
            if (i < 0) return Stop(0);

            double from = Stop(indexInSection[i]);
            double to = Stop(indexInSection[i] + 1);
            double span = travelEnd[i] - beats[i];
            if (span <= 1e-9 || beat >= travelEnd[i]) return to;
            return from + (to - from) * ((beat - beats[i]) / span);
        }

        public static Vector2 PointOnRing(double angleDegrees, float radius)
        {
            double radians = angleDegrees * Math.PI / 180.0;
            return new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians)) * radius;
        }

        static double Stop(int count) => BottomDegrees + StepDegrees * count;

        // Index of the last event whose beat is at or before the given beat, or -1 if there is none.
        int LastEventAtOrBefore(double beat)
        {
            int low = 0, high = beats.Length;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (beats[mid] <= beat) low = mid + 1;
                else high = mid;
            }
            return low - 1;
        }
    }
}
