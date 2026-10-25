using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// The player's input latency offset, saved between sessions.
    /// A positive value means the player hits later than the audio clock says, so judging
    /// shifts expected times later by that amount.
    /// </summary>
    public static class RhythmCalibration
    {
        const string Key = "rhythm.inputOffsetSeconds";

        public static double OffsetSeconds
        {
            get => PlayerPrefs.GetFloat(Key, 0f);
            set => PlayerPrefs.SetFloat(Key, (float)value);
        }

        public static double OffsetMilliseconds
        {
            get => OffsetSeconds * 1000.0;
            set => OffsetSeconds = value / 1000.0;
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
