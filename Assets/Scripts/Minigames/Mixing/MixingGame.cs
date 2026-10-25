using UnityEngine;
using Brewing.Minigames;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>
    /// Runs the mixing minigame: every judgement heats or cools the brew, and a boil-over restarts the chart.
    /// </summary>
    public class MixingGame : FailureRun
    {
        [Header("Mixing")]
        [SerializeField] PatternChart chart;

        protected override RhythmChart ChartToPlay => chart;

        protected override bool TryScore(JudgeResult result, out Judgement judgement)
        {
            judgement = result.Judgement;
            return true;
        }
    }
}
