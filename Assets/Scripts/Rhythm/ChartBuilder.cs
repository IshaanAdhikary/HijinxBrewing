using System;
using System.Collections.Generic;

namespace Brewing.Rhythm
{
    public static class ChartBuilder
    {
        /// <summary>
        /// Generates the expected events for a list of sections. Each section starts a fresh pass through
        /// the pattern at its start beat, so a tempo change always begins at the first action of the cycle.
        /// </summary>
        public static List<RhythmEvent> Build(IReadOnlyList<ChartSection> sections, IReadOnlyList<RhythmAction> pattern)
        {
            var events = new List<RhythmEvent>();
            if (sections == null || pattern == null || pattern.Count == 0) return events;

            for (int s = 0; s < sections.Count; s++)
            {
                var section = sections[s];
                double spacing = section.mode.EventSpacingBeats();
                int count = (int)Math.Floor(section.lengthBeats / spacing + 1e-9);
                for (int k = 0; k < count; k++)
                {
                    double beat = section.startBeat + k * spacing;
                    events.Add(new RhythmEvent(events.Count, beat, pattern[k % pattern.Count], s));
                }
            }

            return events;
        }

        /// <summary>Returns human-readable problems with a chart. Empty means it looks fine.</summary>
        public static List<string> Validate(IReadOnlyList<ChartSection> sections, IReadOnlyList<RhythmAction> pattern)
        {
            var problems = new List<string>();
            if (pattern == null || pattern.Count == 0)
            {
                problems.Add("Action pattern is empty.");
                return problems;
            }

            if (sections == null || sections.Count == 0)
            {
                problems.Add("Chart has no sections.");
                return problems;
            }

            int previousEnd = int.MinValue;
            for (int s = 0; s < sections.Count; s++)
            {
                var section = sections[s];
                if (section.lengthBeats <= 0)
                {
                    problems.Add($"Section {s} has a length of {section.lengthBeats} beats.");
                    continue;
                }

                if (section.startBeat < previousEnd)
                    problems.Add($"Section {s} starts at beat {section.startBeat}, before the previous section ends at beat {previousEnd}.");

                double spacing = section.mode.EventSpacingBeats();
                double events = section.lengthBeats / spacing;
                if (Math.Abs(events - Math.Round(events)) > 1e-9)
                    problems.Add($"Section {s} ({section.mode}) length {section.lengthBeats} is not a whole number of events.");
                else if ((int)Math.Round(events) % pattern.Count != 0)
                    problems.Add($"Section {s} ({section.mode}) ends mid-cycle: {(int)Math.Round(events)} events do not fill whole {pattern.Count}-action cycles, so the next section would start while keys may be held.");

                previousEnd = section.EndBeat;
            }

            return problems;
        }
    }
}
