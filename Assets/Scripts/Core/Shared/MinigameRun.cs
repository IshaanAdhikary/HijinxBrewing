using UnityEngine;
using UnityEngine.Events;
using Brewing.Rhythm;

namespace Brewing.Minigames
{
    /// <summary>
    /// What every rhythm minigame does: starts its chart on the rhythm session (on load, or when asked) and
    /// hears every judgement. Each minigame supplies its chart and decides what a judgement means.
    /// </summary>
    public abstract class MinigameRun : MonoBehaviour
    {
        [SerializeField] RhythmSession session;
        [SerializeField] bool startOnLoad = true;
        [SerializeField] float leadInSeconds = 2f;

        [Header("Events")]
        [SerializeField, Tooltip("Invoked every time the chart starts or restarts. Use it to reset visuals, such as bringing the helper back.")]
        UnityEvent chartStarted = new UnityEvent();

        public RhythmSession Session => session;

        public UnityEvent ChartStarted => chartStarted;

        /// <summary>The chart this run plays.</summary>
        protected abstract RhythmChart ChartToPlay { get; }

        /// <summary>Called for every hit or miss while the chart runs.</summary>
        protected abstract void HandleJudged(JudgeResult result);

        /// <summary>For in-between base classes to reset their own state. Runs before <see cref="OnBegin"/>.</summary>
        protected virtual void ResetRun() { }

        /// <summary>Called by <see cref="Begin"/> before the chart starts. Reset the minigame's state here.</summary>
        protected virtual void OnBegin() { }

        protected virtual void OnEnable()
        {
            session.Judged += HandleJudged;
        }

        protected virtual void OnDisable()
        {
            session.Judged -= HandleJudged;
        }

        void Start()
        {
            if (startOnLoad) Begin();
        }

        /// <summary>Starts the chart from the beginning, restarting it if it is already running.</summary>
        public void Begin()
        {
            ResetRun();
            OnBegin();
            session.Begin(ChartToPlay, leadInSeconds);
            chartStarted.Invoke();
        }
    }
}
