using System;
using System.Collections.Generic;
using Brewing.Rhythm;

namespace Brewing.Pouring
{
    /// <summary>
    /// Pouring needs both keys at once, so each press and each release is really two judgements. This waits
    /// for both and reports the worse of the two: a miss beats a good, a good beats a perfect, and between
    /// two of the same rating, the one further off the beat.
    /// </summary>
    public sealed class ChordCombiner
    {
        readonly int chordSize;
        readonly Dictionary<(int bottle, InputKind kind), (JudgeResult worst, int count)> partial =
            new Dictionary<(int, InputKind), (JudgeResult, int)>();

        public ChordCombiner(int chordSize = 2)
        {
            this.chordSize = chordSize;
        }

        /// <summary>Adds one judgement. Returns true, with the combined result, once the whole chord is in.</summary>
        public bool Add(JudgeResult result, out JudgeResult combined)
        {
            var key = (result.Event.SectionIndex, result.Event.Action.kind);
            var worst = result;
            int count = 1;
            if (partial.TryGetValue(key, out var existing))
            {
                worst = Worse(existing.worst, result);
                count = existing.count + 1;
            }

            if (count >= chordSize)
            {
                partial.Remove(key);
                combined = worst;
                return true;
            }

            partial[key] = (worst, count);
            combined = default;
            return false;
        }

        public void Reset() => partial.Clear();

        public static JudgeResult Worse(JudgeResult a, JudgeResult b)
        {
            if (a.Judgement != b.Judgement) return a.Judgement > b.Judgement ? a : b;
            return Math.Abs(a.ErrorSeconds) >= Math.Abs(b.ErrorSeconds) ? a : b;
        }
    }
}
