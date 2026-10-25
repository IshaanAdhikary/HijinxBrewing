using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Brewing.Minigames;
using Brewing.Rhythm;

namespace Brewing.Crushing
{
    /// <summary>
    /// Runs the crushing minigame: the book plays each round's call, then the player repeats it with the
    /// left and right keys to crush that round's ingredient. There is no boiling over: the chart always plays
    /// to the end, then the results show how many ingredients were crushed against how many were needed.
    /// </summary>
    public class CrushingGame : MinigameRun
    {
        [Header("Crushing")]
        [SerializeField] CallResponseChart chart;
        [SerializeField, Tooltip("Misses an ingredient can have and still count as crushed.")] int allowedMissesPerIngredient = 1;
        [SerializeField, Tooltip("Failed ingredients the player can have and still pass.")] int maxFailures = 1;
        [SerializeField, Tooltip("Shortest wait, in seconds, between the last ingredient being judged and the results, so its throw can finish. The chart's Wait Beats At End can make the wait longer.")]
        float resultsDelaySeconds = 1.5f;

        [Header("Results text")]
        [SerializeField, Tooltip("{0} is the number crushed, {1} the total.")] string crushedFormat = "{0} / {1} ingredients crushed";
        [SerializeField, Tooltip("{0} is the number needed.")] string neededFormat = "{0} needed";

        readonly List<int> remainingInRound = new List<int>();
        readonly List<int> missesInRound = new List<int>();
        Coroutine resultsRoutine;

        public CallResponseChart Chart => chart;

        /// <summary>The notes the book plays for the current run, in beat order.</summary>
        public IReadOnlyList<CallNote> CallNotes { get; private set; } = Array.Empty<CallNote>();

        /// <summary>Ingredients in this run: every round with at least one note.</summary>
        public int TotalIngredients { get; private set; }
        public int Crushed { get; private set; }
        public int Failures { get; private set; }
        public int MaxFailures => maxFailures;

        /// <summary>Ingredients that must be crushed to pass.</summary>
        public int Needed => Mathf.Max(0, TotalIngredients - maxFailures);

        public bool Passed => Crushed >= Needed;

        public override RunResults Results =>
            new RunResults(Passed, string.Format(crushedFormat, Crushed, TotalIngredients), string.Format(neededFormat, Needed));

        /// <summary>Raised for each response note, hit or missed, and for each stray press during a response.</summary>
        public event Action<JudgeResult> NoteJudged;

        /// <summary>Raised when the last note of a round is judged. The bool is true if the ingredient was crushed.</summary>
        public event Action<int, bool> RoundFinished;

        protected override RhythmChart ChartToPlay => chart;

        protected override void OnBegin()
        {
            if (resultsRoutine != null)
            {
                StopCoroutine(resultsRoutine);
                resultsRoutine = null;
            }

            CallNotes = chart.BuildCallNotes();
            Crushed = 0;
            Failures = 0;

            remainingInRound.Clear();
            missesInRound.Clear();
            for (int i = 0; i < chart.rounds.Length; i++)
            {
                remainingInRound.Add(0);
                missesInRound.Add(0);
            }
            foreach (var note in CallNotes) remainingInRound[note.Round]++;

            TotalIngredients = 0;
            foreach (int notes in remainingInRound)
                if (notes > 0) TotalIngredients++;
        }

        protected override void HandleJudged(JudgeResult result)
        {
            if (result.IsStray)
            {
                HandleStray(result);
                return;
            }

            NoteJudged?.Invoke(result);

            int round = result.Event.Group;
            if (result.Judgement == Judgement.Miss) missesInRound[round]++;
            if (--remainingInRound[round] > 0) return;

            bool crushed = missesInRound[round] <= allowedMissesPerIngredient;
            if (crushed) Crushed++;
            else Failures++;
            RoundFinished?.Invoke(round, crushed);

            if (Crushed + Failures == TotalIngredients) resultsRoutine = StartCoroutine(ShowResultsAfterDelay());
        }

        /// <summary>
        /// A press that hit no note. Free during a call, so players can tap along to learn the pattern; during a
        /// response it counts as a miss against that round's ingredient, until the ingredient has been decided.
        /// </summary>
        void HandleStray(JudgeResult result)
        {
            double beat = result.Event.Beat;
            int round = chart.RoundAt(beat);
            if (round < 0 || beat < chart.layout.ResponseStart(round)) return;

            NoteJudged?.Invoke(result);
            if (remainingInRound[round] > 0) missesInRound[round]++;
        }

        IEnumerator ShowResultsAfterDelay()
        {
            yield return new WaitForSecondsRealtime(resultsDelaySeconds);
            while (Session.IsRunning && Session.Conductor.SongBeat < chart.ResultsBeat) yield return null;
            resultsRoutine = null;
            Finish();
        }
    }
}
