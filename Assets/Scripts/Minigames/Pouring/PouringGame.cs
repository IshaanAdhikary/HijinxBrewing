using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Brewing.Mixing;
using Brewing.Rhythm;

namespace Brewing.Pouring
{
    /// <summary>
    /// Runs the pouring minigame. Each bottle is poured by pressing left and right together on its arrival
    /// beat and releasing both when it is full. Each press and each release is judged on both keys and the
    /// worse of the two counts. Bad pours heat the cauldron, like mixing, and a boil-over restarts the chart.
    /// </summary>
    public class PouringGame : MonoBehaviour, IFailureSource
    {
        [SerializeField] RhythmSession session;
        [SerializeField] PourChart chart;
        [SerializeField] bool startOnLoad = true;
        [SerializeField] float leadInSeconds = 2f;

        [Header("Failure")]
        [SerializeField] FailureSettings failure = new FailureSettings();
        [SerializeField, Tooltip("Seconds between boiling over and the chart restarting from the beginning.")] float restartDelaySeconds = 2f;

        [Header("Debug")]
        [SerializeField, Tooltip("Optional. Shows each combined (worse of both keys) result. Set its Listen To Session off.")]
        JudgementOverlay overlay;

        [Header("Events")]
        [SerializeField, Tooltip("Invoked every time the chart starts or restarts. Use it to reset visuals, such as bringing the light back.")]
        UnityEvent chartStarted = new UnityEvent();

        FailureMeter meter;
        readonly ChordCombiner combiner = new ChordCombiner();
        Coroutine restartRoutine;

        public RhythmSession Session => session;
        public PourChart Chart => chart;

        /// <summary>The pours for the current run, in beat order.</summary>
        public IReadOnlyList<PourWindow> Windows { get; private set; } = Array.Empty<PourWindow>();

        public float FailureLevel => meter != null ? meter.Value : 0f;
        public bool IsBoilingOver { get; private set; }

        /// <summary>True while the song runs and both keys are held, which is when liquid flows.</summary>
        public bool IsPouring =>
            session != null && session.IsRunning &&
            session.InputSource.IsHeld(PourChartBuilder.LeftButton) &&
            session.InputSource.IsHeld(PourChartBuilder.RightButton);

        public UnityEvent ChartStarted => chartStarted;

        /// <summary>Raised for each combined press or release (the worse of the two keys).</summary>
        public event Action<JudgeResult> PourJudged;

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
            combiner.Reset();
            Windows = chart.BuildWindows();
            session.Begin(chart, leadInSeconds);
            chartStarted.Invoke();
        }

        void OnJudged(JudgeResult result)
        {
            if (IsBoilingOver) return;
            if (!combiner.Add(result, out var combined)) return;

            if (overlay != null) overlay.Show(combined);
            PourJudged?.Invoke(combined);
            if (meter.Apply(combined.Judgement)) BoilOver();
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
