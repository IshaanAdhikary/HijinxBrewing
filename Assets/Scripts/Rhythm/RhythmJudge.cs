using System;
using System.Collections.Generic;
using UnityEngine;

namespace Brewing.Rhythm
{
    [Serializable]
    public class JudgeSettings
    {
        [Tooltip("Seconds either side of the expected time that still count as Perfect.")]
        public double perfectWindow = 0.05;

        [Tooltip("Seconds either side of the expected time that still count as a hit.")]
        public double goodWindow = 0.11;

        [Tooltip("Windows never exceed this fraction of the gap to the nearest neighbouring event, so fast sections stay unambiguous.")]
        public double maxFractionOfGap = 0.4;
    }

    /// <summary>
    /// Matches player input against a list of expected events. Works purely in song-time seconds, so it
    /// has no Unity dependencies and can be tested directly.
    /// </summary>
    public class RhythmJudge
    {
        readonly IReadOnlyList<RhythmEvent> events;
        readonly double[] times;
        readonly double[] goodWindows;
        readonly double[] perfectWindows;
        int cursor;

        public event Action<JudgeResult> Judged;

        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }

        public int Cursor => cursor;
        public bool IsFinished => cursor >= events.Count;
        public int EventCount => events.Count;

        /// <summary>The next unresolved event, or null if every event has been judged.</summary>
        public RhythmEvent? Pending => IsFinished ? (RhythmEvent?)null : events[cursor];

        public RhythmJudge(IReadOnlyList<RhythmEvent> events, BeatClock beatClock, JudgeSettings settings)
        {
            this.events = events;
            int n = events.Count;
            times = new double[n];
            goodWindows = new double[n];
            perfectWindows = new double[n];

            for (int i = 0; i < n; i++)
                times[i] = beatClock.BeatToSongTime(events[i].Beat);

            for (int i = 0; i < n; i++)
            {
                double gap = double.MaxValue;
                if (i > 0) gap = Math.Min(gap, times[i] - times[i - 1]);
                if (i < n - 1) gap = Math.Min(gap, times[i + 1] - times[i]);

                double good = Math.Min(settings.goodWindow, gap * settings.maxFractionOfGap);
                goodWindows[i] = good;
                perfectWindows[i] = Math.Min(settings.perfectWindow, good * 0.5);
            }
        }

        public double GetEventTime(int index) => times[index];

        /// <summary>Marks every event whose window has fully passed as a miss.</summary>
        public void AdvanceTo(double songTime)
        {
            while (cursor < events.Count && songTime > times[cursor] + goodWindows[cursor])
                Resolve(new JudgeResult(events[cursor], Judgement.Miss, 0.0));
        }

        /// <summary>
        /// Offers one player action. Returns true if it resolved the pending event. Wrong buttons and
        /// inputs that are too early for the pending event are ignored and return false.
        /// </summary>
        public bool Input(RhythmAction action, double songTime, out JudgeResult result)
        {
            AdvanceTo(songTime);
            result = default;

            if (IsFinished) return false;

            double error = songTime - times[cursor];
            if (Math.Abs(error) > goodWindows[cursor]) return false;
            if (!action.Equals(events[cursor].Action)) return false;

            var judgement = Math.Abs(error) <= perfectWindows[cursor] ? Judgement.Perfect : Judgement.Good;
            result = new JudgeResult(events[cursor], judgement, error);
            Resolve(result);
            return true;
        }

        public void Reset()
        {
            cursor = 0;
            PerfectCount = GoodCount = MissCount = 0;
        }

        void Resolve(JudgeResult result)
        {
            cursor++;
            switch (result.Judgement)
            {
                case Judgement.Perfect: PerfectCount++; break;
                case Judgement.Good: GoodCount++; break;
                default: MissCount++; break;
            }
            Judged?.Invoke(result);
        }
    }
}
