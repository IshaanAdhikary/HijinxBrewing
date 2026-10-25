using System;
using UnityEngine;
using UnityEngine.Events;
using Brewing.Rhythm;

namespace Brewing.Minigames
{
    /// <summary>
    /// What every rhythm minigame does: starts its chart on the rhythm session (on load, or when asked),
    /// hears every judgement, and finishes with results for the results pop-up. Each minigame supplies its
    /// chart, decides what a judgement means, and decides when it is finished.
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

        /// <summary>True once the run has finished and its results are up, until the next <see cref="Begin"/>.</summary>
        public bool IsFinished { get; private set; }

        /// <summary>Raised once when the run finishes. Read <see cref="Results"/> for what to show.</summary>
        public event Action Finished;

        /// <summary>Pass or fail and the detail lines for the results pop-up.</summary>
        public virtual RunResults Results => new RunResults(true, "", "");

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
            IsFinished = false;
            ResetRun();
            OnBegin();
            session.Begin(ChartToPlay, leadInSeconds);
            chartStarted.Invoke();
        }

        /// <summary>Stops the music and raises <see cref="Finished"/>. Does nothing if already finished.</summary>
        protected void Finish()
        {
            if (IsFinished) return;
            IsFinished = true;
            session.End();
            Finished?.Invoke();
        }
    }
}
