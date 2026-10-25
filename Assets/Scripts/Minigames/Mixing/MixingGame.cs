using UnityEngine;
using Brewing.Minigames;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>
    /// Runs the mixing minigame: every judgement heats or cools the brew, and a boil-over restarts the chart.
    /// Reaching the end of the chart (plus its Wait Beats At End) without boiling over finishes the run and
    /// shows the results.
    /// </summary>
    public class MixingGame : FailureRun
    {
        [Header("Mixing")]
        [SerializeField] PatternChart chart;

        [Header("Results text")]
        [SerializeField, Tooltip("{0} Perfect, {1} Good, {2} missed.")] string judgementsFormat = "{0} Perfect   {1} Good   {2} Missed";
        [SerializeField, Tooltip("{0} is the number hit, {1} the total.")] string hitsFormat = "{0} / {1} stirs hit";

        protected override RhythmChart ChartToPlay => chart;

        public override RunResults Results
        {
            get
            {
                var judge = Session.Judge;
                if (judge == null) return new RunResults(true, "", "");
                int hits = judge.PerfectCount + judge.GoodCount;
                return new RunResults(true,
                    string.Format(judgementsFormat, judge.PerfectCount, judge.GoodCount, judge.MissCount),
                    string.Format(hitsFormat, hits, judge.EventCount));
            }
        }

        protected override bool TryScore(JudgeResult result, out Judgement judgement)
        {
            judgement = result.Judgement;
            return true;
        }

        void Update()
        {
            if (IsFinished || IsBoilingOver || !Session.IsRunning) return;
            if (Session.Conductor.SongBeat >= chart.ResultsBeat) Finish();
        }
    }
}
