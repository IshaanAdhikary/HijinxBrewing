using System;
using System.Collections;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Minigames
{
    /// <summary>
    /// A minigame that can boil over: tracks how close the brew is to boiling over, and restarts the chart
    /// from the beginning after a boil-over. Each minigame turns a raw judge result into the judgement that
    /// feeds the meter. Being a class (not just <see cref="IFailureSource"/>) lets the Inspector accept only
    /// valid objects.
    /// </summary>
    public abstract class FailureRun : MinigameRun, IFailureSource
    {
        [Header("Failure")]
        [SerializeField] FailureSettings failure = new FailureSettings();
        [SerializeField, Tooltip("Seconds between boiling over and the chart restarting from the beginning.")] float restartDelaySeconds = 2f;

        FailureMeter meter;
        Coroutine restartRoutine;

        /// <summary>How close the brew is to boiling over: 0 is calm, 1 is boiled over.</summary>
        public float FailureLevel => meter != null ? meter.Value : 0f;

        public bool IsBoilingOver { get; private set; }

        /// <summary>Raised once when the brew boils over, before the delayed restart.</summary>
        public event Action Failed;

        /// <summary>
        /// Turns a judge result into the judgement that moves the meter. Return false to ignore it
        /// (for example while waiting for the other half of a chord).
        /// </summary>
        protected abstract bool TryScore(JudgeResult result, out Judgement judgement);

        protected virtual void Awake()
        {
            meter = new FailureMeter(failure);
        }

        protected override void ResetRun()
        {
            if (restartRoutine != null)
            {
                StopCoroutine(restartRoutine);
                restartRoutine = null;
            }

            IsBoilingOver = false;
            meter.Reset();
        }

        protected override void HandleJudged(JudgeResult result)
        {
            if (IsBoilingOver) return;
            if (!TryScore(result, out var judgement)) return;
            if (meter.Apply(judgement)) BoilOver();
        }

        void BoilOver()
        {
            IsBoilingOver = true;
            Session.End();
            Failed?.Invoke();
            restartRoutine = StartCoroutine(RestartAfterDelay());
        }

        IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSecondsRealtime(restartDelaySeconds);
            restartRoutine = null;
            Begin();
        }
    }
}
