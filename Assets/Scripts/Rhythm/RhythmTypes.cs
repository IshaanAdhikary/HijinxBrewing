using System;

namespace Brewing.Rhythm
{
    public enum InputKind { Press, Release }

    public enum TempoMode { Half, Normal, Double }

    public enum Judgement { Perfect, Good, Miss }

    public static class TempoModeExtensions
    {
        /// <summary>Beats between consecutive events: half time = 2, normal = 1, double = 0.5.</summary>
        public static double EventSpacingBeats(this TempoMode mode)
        {
            switch (mode)
            {
                case TempoMode.Half: return 2.0;
                case TempoMode.Double: return 0.5;
                default: return 1.0;
            }
        }
    }

    /// <summary>One player action: a press or release of a numbered button (0 = left, 1 = right by default).</summary>
    [Serializable]
    public struct RhythmAction : IEquatable<RhythmAction>
    {
        public int button;
        public InputKind kind;

        public RhythmAction(int button, InputKind kind)
        {
            this.button = button;
            this.kind = kind;
        }

        public bool Equals(RhythmAction other) => button == other.button && kind == other.kind;
        public override bool Equals(object obj) => obj is RhythmAction other && Equals(other);
        public override int GetHashCode() => button * 31 + (int)kind;
        public override string ToString() => $"{kind} {button}";
    }

    /// <summary>An action the player is expected to perform at a given beat.</summary>
    public readonly struct RhythmEvent
    {
        public readonly int Index;
        public readonly double Beat;
        public readonly RhythmAction Action;
        public readonly int SectionIndex;

        public RhythmEvent(int index, double beat, RhythmAction action, int sectionIndex)
        {
            Index = index;
            Beat = beat;
            Action = action;
            SectionIndex = sectionIndex;
        }
    }

    public readonly struct JudgeResult
    {
        public readonly RhythmEvent Event;
        public readonly Judgement Judgement;

        /// <summary>Input time minus expected time in seconds. Negative is early. Zero for misses.</summary>
        public readonly double ErrorSeconds;

        public JudgeResult(RhythmEvent evt, Judgement judgement, double errorSeconds)
        {
            Event = evt;
            Judgement = judgement;
            ErrorSeconds = errorSeconds;
        }
    }
}
