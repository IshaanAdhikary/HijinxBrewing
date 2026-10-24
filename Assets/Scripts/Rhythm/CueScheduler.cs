using System.Collections.Generic;
using System.Linq;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Hands out chart cues as the song reaches their beats. Each cue is returned exactly once, in beat order,
    /// and cues passed in the same call (a slow frame) are all returned together. Pure C#, so it can be tested.
    /// </summary>
    public sealed class CueScheduler
    {
        readonly ChartCue[] cues;
        int next;

        public CueScheduler(IEnumerable<ChartCue> cues)
        {
            this.cues = cues == null ? new ChartCue[0] : cues.OrderBy(c => c.beat).ToArray();
        }

        public int PendingCount => cues.Length - next;

        /// <summary>Appends every not-yet-returned cue at or before the given beat to <paramref name="due"/>.</summary>
        public void Advance(double beat, List<ChartCue> due)
        {
            while (next < cues.Length && cues[next].beat <= beat)
            {
                due.Add(cues[next]);
                next++;
            }
        }
    }
}
