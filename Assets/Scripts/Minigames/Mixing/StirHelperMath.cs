using System;
using System.Collections.Generic;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>The logic behind the stir helper overlay, kept separate so it can be tested.</summary>
    public static class StirHelperMath
    {
        /// <summary>
        /// Which compass slot each event belongs to: its position within its section, wrapped to the pattern
        /// length. With the stir pattern that is 0 = south, 1 = west, 2 = north, 3 = east.
        /// </summary>
        public static int[] SlotOfEachEvent(IReadOnlyList<RhythmEvent> events, int patternLength)
        {
            var slots = new int[events.Count];
            int indexInSection = 0;
            for (int i = 0; i < events.Count; i++)
            {
                bool continues = i > 0 && events[i - 1].SectionIndex == events[i].SectionIndex;
                indexInSection = continues ? indexInSection + 1 : 0;
                slots[i] = patternLength > 0 ? indexInSection % patternLength : 0;
            }
            return slots;
        }

        /// <summary>
        /// For each slot, whether a hold arc starts there: a press followed (in the cycle) by a release of the
        /// same button, so the arc runs between the two compass points the spoon travels while the key is held.
        /// </summary>
        public static bool[] HoldArcStartsAt(IReadOnlyList<RhythmAction> pattern)
        {
            var starts = new bool[pattern.Count];
            for (int k = 0; k < pattern.Count; k++)
            {
                var press = pattern[k];
                var next = pattern[(k + 1) % pattern.Count];
                starts[k] = press.kind == InputKind.Press && next.kind == InputKind.Release && next.button == press.button;
            }
            return starts;
        }

        /// <summary>
        /// How far away the next event is, from 0 (due now) to 1 (a full event spacing or more away). The
        /// approach ring shrinks over exactly the time the spoon takes to reach the stop.
        /// </summary>
        public static float ApproachFraction(double eventBeat, double beatNow, double spacingBeats)
        {
            if (spacingBeats <= 0.0) return 0f;
            return (float)Math.Max(0.0, Math.Min(1.0, (eventBeat - beatNow) / spacingBeats));
        }
    }
}
