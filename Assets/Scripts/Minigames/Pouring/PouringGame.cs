using System;
using System.Collections.Generic;
using UnityEngine;
using Brewing.Minigames;
using Brewing.Rhythm;

namespace Brewing.Pouring
{
    /// <summary>
    /// Runs the pouring minigame. Each bottle is poured by pressing left and right together on its arrival
    /// beat and releasing both when it is full. Each press and each release is judged on both keys and the
    /// worse of the two counts. Bad pours heat the cauldron, like mixing, and a boil-over restarts the chart.
    /// </summary>
    public class PouringGame : FailureRun
    {
        [Header("Pouring")]
        [SerializeField] PourChart chart;

        [Header("Debug")]
        [SerializeField, Tooltip("Optional. Shows each combined (worse of both keys) result. Set its Listen To Session off.")]
        JudgementOverlay overlay;

        readonly ChordCombiner combiner = new ChordCombiner();

        public PourChart Chart => chart;

        /// <summary>The pours for the current run, in beat order.</summary>
        public IReadOnlyList<PourWindow> Windows { get; private set; } = Array.Empty<PourWindow>();

        /// <summary>True while the song runs and both keys are held, which is when liquid flows.</summary>
        public bool IsPouring =>
            Session != null && Session.IsRunning &&
            Session.InputSource.IsHeld(PourChartBuilder.LeftButton) &&
            Session.InputSource.IsHeld(PourChartBuilder.RightButton);

        /// <summary>Raised for each combined press or release (the worse of the two keys).</summary>
        public event Action<JudgeResult> PourJudged;

        protected override RhythmChart ChartToPlay => chart;

        protected override void OnBegin()
        {
            combiner.Reset();
            Windows = chart.BuildWindows();
        }

        protected override bool TryScore(JudgeResult result, out Judgement judgement)
        {
            judgement = default;
            // A stray press has no pour to pair with, so it counts on its own rather than going through the combiner.
            var combined = result;
            if (!result.IsStray && !combiner.Add(result, out combined)) return false;

            if (overlay != null) overlay.Show(combined);
            PourJudged?.Invoke(combined);
            judgement = combined.Judgement;
            return true;
        }
    }
}
