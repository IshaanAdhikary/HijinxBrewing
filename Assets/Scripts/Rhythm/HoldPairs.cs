using System.Collections.Generic;

namespace Brewing.Rhythm
{
    public static class HoldPairs
    {
        /// <summary>
        /// For each press event, the index of the release of the same button that ends its hold, or -1 if it
        /// has none (or the same button is pressed again first). Always -1 for releases.
        /// </summary>
        public static int[] Compute(IReadOnlyList<RhythmEvent> events)
        {
            var releases = new int[events.Count];
            for (int i = 0; i < events.Count; i++)
            {
                releases[i] = -1;
                var action = events[i].Action;
                if (action.kind != InputKind.Press) continue;

                for (int j = i + 1; j < events.Count; j++)
                {
                    var other = events[j].Action;
                    if (other.button != action.button) continue;
                    if (other.kind == InputKind.Release) releases[i] = j;
                    break;
                }
            }
            return releases;
        }
    }
}
