using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Brewing.Rhythm;

namespace Brewing.Minigames
{
    /// <summary>
    /// Shared run logic for minigames that can fail: starts the chart on the rhythm session, tracks how close
    /// the brew is to boiling over, and restarts the chart from the beginning after a boil-over. Each minigame
    /// supplies its chart and turns a raw judge result into the judgement that feeds the meter.
    /// Being a class (not just <see cref="IFailureSource"/>) lets the Inspector accept only valid objects.
    /// </summary>
    public abstract class FailureRun : MonoBehaviour, IFailureSource
    {
        [SerializeField] RhythmSession session;
        [SerializeField] bool startOnLoad = true;
        [SerializeField] float leadInSeconds = 2f;

        [Header("Failure")]
        [SerializeField] FailureSettings failure = new FailureSettings();
        [SerializeField, Tooltip("Seconds between boiling over and the chart restarting from the beginning.")] float restartDelaySeconds = 2f;

        [Header("Events")]
        [SerializeField, Tooltip("Invoked every time the chart starts or restarts. Use it to reset visuals, such as bringing the helper back.")]
        UnityEvent chartStarted = new UnityEvent();

        FailureMeter meter;
        Coroutine restartRoutine;

        public RhythmSession Session => session;

        /// <summary>How close the brew is to boiling over: 0 is calm, 1 is boiled over.</summary>
        public float FailureLevel => meter != null ? meter.Value : 0f;

        public bool IsBoilingOver { get; private set; }

        public UnityEvent ChartStarted => chartStarted;

        /// <summary>Raised once when the brew boils over, before the delayed restart.</summary>
        public event Action Failed;

        /// <summary>The chart this run plays.</summary>
        protected abstract RhythmChart ChartToPlay { get; }

        /// <summary>
        /// Turns a judge result into the judgement that moves the meter. Return false to ignore it
        /// (for example while waiting for the other half of a chord).
        /// </summary>
        protected abstract bool TryScore(JudgeResult result, out Judgement judgement);

        /// <summary>Called by <see cref="Begin"/> after the meter resets and before the chart starts.</summary>
        protected virtual void OnBegin() { }

        protected virtual void Awake()
        {
            meter = new FailureMeter(failure);
        }

        void OnEnable()
        {
            session.Judged += OnJudged;
        }

        void OnDisable()
        {
            session.Judged -= OnJudged;
        }

        void Start()
        {
            if (startOnLoad) Begin();
        }

        public void Begin()
        {
            if (restartRoutine != null)
            {
                StopCoroutine(restartRoutine);
                restartRoutine = null;
            }

            IsBoilingOver = false;
            meter.Reset();
            OnBegin();
            session.Begin(ChartToPlay, leadInSeconds);
            chartStarted.Invoke();
        }

        void OnJudged(JudgeResult result)
        {
            if (IsBoilingOver) return;
            if (!TryScore(result, out var judgement)) return;
            if (meter.Apply(judgement)) BoilOver();
        }

        void BoilOver()
        {
            IsBoilingOver = true;
            session.End();
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
