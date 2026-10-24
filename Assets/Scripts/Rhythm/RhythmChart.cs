using System;
using System.Collections.Generic;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>A stretch of the song played at one tempo mode.</summary>
    [Serializable]
    public struct ChartSection
    {
        public int startBeat;
        public int lengthBeats;
        public TempoMode mode;

        public ChartSection(int startBeat, int lengthBeats, TempoMode mode)
        {
            this.startBeat = startBeat;
            this.lengthBeats = lengthBeats;
            this.mode = mode;
        }

        public int EndBeat => startBeat + lengthBeats;
    }

    /// <summary>
    /// A song plus its tempo sections. Events are generated from the sections: within each section the
    /// action pattern repeats, one action per event spacing, restarting at the start of every section.
    /// </summary>
    [CreateAssetMenu(menuName = "Brewing/Rhythm Chart", fileName = "RhythmChart")]
    public class RhythmChart : ScriptableObject
    {
        [Tooltip("Leave empty to play a metronome at the BPM below.")]
        public AudioClip clip;
        public double bpm = 120.0;
        [Tooltip("Seconds from the start of the clip to beat 0.")]
        public double firstBeatOffsetSeconds;

        [Tooltip("The repeating cycle of actions. Default is the cauldron stir: hold left, release, hold right, release.")]
        public RhythmAction[] actionPattern =
        {
            new RhythmAction(0, InputKind.Press),
            new RhythmAction(0, InputKind.Release),
            new RhythmAction(1, InputKind.Press),
            new RhythmAction(1, InputKind.Release),
        };

        public ChartSection[] sections =
        {
            new ChartSection(0, 16, TempoMode.Normal),
        };

        public BeatClock BeatClock => new BeatClock(bpm, firstBeatOffsetSeconds);

        public List<RhythmEvent> BuildEvents() => ChartBuilder.Build(sections, actionPattern);

        public List<string> Validate() => ChartBuilder.Validate(sections, actionPattern);
    }
}
