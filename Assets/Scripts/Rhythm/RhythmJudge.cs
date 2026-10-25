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
    ///
    /// Several events can share a beat (a chord, such as pressing left and right together). They are judged
    /// independently and can be hit in any order.
    /// </summary>
    public class RhythmJudge
    {
        const double SameTimeTolerance = 1e-6;

        readonly IReadOnlyList<RhythmEvent> events;
        readonly double[] times;
        readonly double[] goodWindows;
        readonly double[] perfectWindows;
        readonly bool[] resolved;
        readonly double widestWindow;
        int cursor;

        public event Action<JudgeResult> Judged;

        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }

        public int Cursor => cursor;
        public bool IsFinished => cursor >= events.Count;
        public int EventCount => events.Count;

        /// <summary>The earliest unresolved event, or null if every event has been judged.</summary>
        public RhythmEvent? Pending => IsFinished ? (RhythmEvent?)null : events[cursor];

        public RhythmJudge(IReadOnlyList<RhythmEvent> events, BeatClock beatClock, JudgeSettings settings)
        {
            this.events = events;
            int n = events.Count;
            times = new double[n];
            goodWindows = new double[n];
            perfectWindows = new double[n];
            resolved = new bool[n];

            for (int i = 0; i < n; i++)
                times[i] = beatClock.BeatToSongTime(events[i].Beat);

            for (int i = 0; i < n; i++)
            {
                // The gap is measured to the nearest event at a different time, so events sharing a beat
                // (a chord) still get a usable window.
                double gap = double.MaxValue;
                int previous = i - 1;
                while (previous >= 0 && SameTime(times[previous], times[i])) previous--;
                if (previous >= 0) gap = Math.Min(gap, times[i] - times[previous]);
                int next = i + 1;
                while (next < n && SameTime(times[next], times[i])) next++;
                if (next < n) gap = Math.Min(gap, times[next] - times[i]);

                double good = Math.Min(settings.goodWindow, gap * settings.maxFractionOfGap);
                goodWindows[i] = good;
                perfectWindows[i] = Math.Min(settings.perfectWindow, good * 0.5);
                widestWindow = Math.Max(widestWindow, good);
            }
        }

        public double GetEventTime(int index) => times[index];

        /// <summary>Marks every event whose window has fully passed as a miss.</summary>
        public void AdvanceTo(double songTime)
        {
            // Events at or after songTime cannot have expired yet.
            for (int i = cursor; i < events.Count && times[i] < songTime; i++)
            {
                if (!resolved[i] && songTime > times[i] + goodWindows[i])
                    Resolve(i, new JudgeResult(events[i], Judgement.Miss, 0.0));
            }
        }

        /// <summary>
        /// Offers one player action. Returns true if it hit an unresolved event of that action whose window
        /// contains the input. Wrong buttons and inputs outside every window are ignored and return false.
        /// </summary>
        public bool Input(RhythmAction action, double songTime, out JudgeResult result)
        {
            AdvanceTo(songTime);
            result = default;

            for (int i = cursor; i < events.Count && times[i] - widestWindow <= songTime; i++)
            {
                if (resolved[i]) continue;
                double error = songTime - times[i];
                if (Math.Abs(error) > goodWindows[i]) continue;
                if (!action.Equals(events[i].Action)) continue;

                var judgement = Math.Abs(error) <= perfectWindows[i] ? Judgement.Perfect : Judgement.Good;
                result = new JudgeResult(events[i], judgement, error);
                Resolve(i, result);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            cursor = 0;
            Array.Clear(resolved, 0, resolved.Length);
            PerfectCount = GoodCount = MissCount = 0;
        }

        void Resolve(int index, JudgeResult result)
        {
            resolved[index] = true;
            while (cursor < events.Count && resolved[cursor]) cursor++;
            switch (result.Judgement)
            {
                case Judgement.Perfect: PerfectCount++; break;
                case Judgement.Good: GoodCount++; break;
                default: MissCount++; break;
            }
            Judged?.Invoke(result);
        }

        static bool SameTime(double a, double b) => Math.Abs(a - b) < SameTimeTolerance;
    }
}
