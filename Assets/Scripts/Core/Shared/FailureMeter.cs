using System;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Minigames
{
    /// <summary>
    /// How much each judgement moves the failure meter. The meter runs from 0 (calm) to 1 (boils over).
    /// Negative deltas calm the brew, positive ones heat it up.
    /// </summary>
    [Serializable]
    public class FailureSettings
    {
        [Range(0f, 1f), Tooltip("Where the meter starts, and where it resets to on a restart.")]
        public float startValue = 0f;

        [Tooltip("Change for a Perfect hit. Normally negative.")]
        public float perfectDelta = -0.06f;

        [Tooltip("Change for a Good hit. Normally negative, smaller than Perfect.")]
        public float goodDelta = -0.03f;

        [Tooltip("Change for a miss. Normally positive.")]
        public float missDelta = 0.12f;
    }

    /// <summary>
    /// The hidden "how close to boiling over" value. Hits pull it down, misses push it up, and reaching 1
    /// means failure. It reads the settings live, so tuning them in the Inspector during Play takes effect.
    /// </summary>
    public sealed class FailureMeter
    {
        readonly FailureSettings settings;

        public float Value { get; private set; }
        public bool HasFailed => Value >= 1f;

        public FailureMeter(FailureSettings settings)
        {
            this.settings = settings;
            Reset();
        }

        public void Reset()
        {
            Value = Mathf.Clamp01(settings.startValue);
        }

        /// <summary>Applies a judgement. Returns true only on the call that makes the brew boil over.</summary>
        public bool Apply(Judgement judgement)
        {
            if (HasFailed) return false;
            Value = Mathf.Clamp01(Value + DeltaFor(judgement));
            return HasFailed;
        }

        public float DeltaFor(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect: return settings.perfectDelta;
                case Judgement.Good: return settings.goodDelta;
                default: return settings.missDelta;
            }
        }
    }
}
