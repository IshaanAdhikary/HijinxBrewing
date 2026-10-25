using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>
    /// Runs the mixing minigame: starts its chart on the rhythm session, tracks how close the brew is to
    /// boiling over, and restarts the chart from the beginning after a boil-over.
    /// </summary>
    public class MixingGame : MonoBehaviour, IFailureSource
    {
        [SerializeField] RhythmSession session;
        [SerializeField] PatternChart chart;
        [SerializeField] bool startOnLoad = true;
        [SerializeField] float leadInSeconds = 2f;

        [Header("Failure")]
        [SerializeField] FailureSettings failure = new FailureSettings();
        [SerializeField, Tooltip("Seconds between boiling over and the chart restarting from the beginning.")] float restartDelaySeconds = 2f;

        [Header("Events")]
        [SerializeField, Tooltip("Invoked every time the chart starts or restarts. Use it to reset visuals, such as bringing the helper lane back.")]
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

        void Awake()
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
            session.Begin(chart, leadInSeconds);
            chartStarted.Invoke();
        }

        void OnJudged(JudgeResult result)
        {
            if (IsBoilingOver) return;
            if (meter.Apply(result.Judgement)) BoilOver();
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
