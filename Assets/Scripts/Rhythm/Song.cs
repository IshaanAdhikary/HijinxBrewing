using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// A piece of music and its timing: the clip, its BPM, and where beat 0 falls. Charts point at a song, so
    /// several charts (easy and hard, or call and response) can share one, and fixing the timing here fixes
    /// it for all of them.
    /// </summary>
    [CreateAssetMenu(menuName = "Brewing/Song", fileName = "Song")]
    public class Song : ScriptableObject
    {
        /// <summary>Tempo used when a chart has no song assigned.</summary>
        public const double DefaultBpm = 120.0;

        [Tooltip("Leave empty to play a metronome at the BPM below.")]
        public AudioClip clip;
        [Tooltip("Optional. The same music with extra instruments, the same length and timing as the clip. It plays in sync, muted, and minigames that support it (mixing) switch between the two.")]
        public AudioClip intenseClip;
        public double bpm = DefaultBpm;
        [Tooltip("Seconds from the start of the clip to beat 0.")]
        public double firstBeatOffsetSeconds;

        public BeatClock BeatClock => new BeatClock(bpm, firstBeatOffsetSeconds);
    }
}
