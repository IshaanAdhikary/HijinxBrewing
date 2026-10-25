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
    /// A named trigger placed on a beat. Nothing in the engine interprets the name: whatever listens for it
    /// decides what happens (fade the helper, start a distraction, and so on).
    /// </summary>
    [Serializable]
    public struct ChartCue
    {
        public double beat;
        public string name;

        public ChartCue(double beat, string name)
        {
            this.beat = beat;
            this.name = name;
        }
    }

    /// <summary>
    /// What every chart has, whatever the minigame: the song it plays to, and named cues. Each minigame has
    /// its own chart type built on this (PatternChart for tempo sections, PourChart for bottles) that decides
    /// which events the player must hit. The rhythm engine only needs the events, the song and the cues.
    /// </summary>
    public abstract class RhythmChart : ScriptableObject
    {
        [Tooltip("The music, its BPM and where beat 0 falls. Several charts can share one song.")]
        public Song song;

        [Tooltip("Named triggers fired when the song reaches their beat. Listen for them with a ChartCueListener.")]
        public ChartCue[] cues = new ChartCue[0];

        /// <summary>Beat timing from the song, or a plain 120 BPM if no song is assigned.</summary>
        public BeatClock BeatClock => song != null ? song.BeatClock : new BeatClock(Song.DefaultBpm, 0.0);

        /// <summary>The music to play, or null for a metronome.</summary>
        public AudioClip Clip => song != null ? song.clip : null;

        /// <summary>The events the player must hit, in beat order.</summary>
        public abstract List<RhythmEvent> BuildEvents();

        /// <summary>Problems with this chart type's own layout.</summary>
        protected abstract List<string> ValidateEvents();

        public List<string> Validate()
        {
            var problems = ValidateEvents();
            if (song == null) problems.Add($"No song assigned, so a {Song.DefaultBpm} BPM metronome plays.");
            if (cues != null)
            {
                for (int i = 0; i < cues.Length; i++)
                    if (string.IsNullOrWhiteSpace(cues[i].name))
                        problems.Add($"Cue {i} at beat {cues[i].beat} has no name.");
            }
            return problems;
        }
    }
}
