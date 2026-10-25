using System;
using System.Collections.Generic;
using UnityEngine;
using Brewing.Minigames;
using Brewing.Rhythm;

namespace Brewing.Crushing
{
    /// <summary>
    /// One call and response: the ingredient plays a pattern on the book, then the player repeats it.
    /// The pattern is written on an eighth-note grid: L is the left page (left key), R is the right page
    /// (right key), and . is a rest. Spaces and | are ignored, so "L.R.L... | R...L.R." reads as two bars.
    /// </summary>
    [Serializable]
    public class CallRound
    {
        [Tooltip("L = left, R = right, . = rest, one character per eighth note. Spaces and | are ignored.")]
        public string pattern = "L.R.L... | R...L.R.";
        [Tooltip("Shown on the book and crushed by the cat this round. Leave empty to use the placeholder.")]
        public Ingredient ingredient;
    }

    /// <summary>A note the ingredient plays during a call, which the player answers later.</summary>
    public readonly struct CallNote
    {
        public readonly int Round;
        public readonly double Beat;
        public readonly int Button;

        public CallNote(int round, double beat, int button)
        {
            Round = round;
            Beat = beat;
            Button = button;
        }
    }

    /// <summary>The timing grid every round shares.</summary>
    [Serializable]
    public struct CallLayout
    {
        [Tooltip("Beat the first call starts on.")] public int firstBeat;
        public int beatsPerBar;
        [Tooltip("Length of the call, in bars. The response is the same length and follows straight after.")] public int callBars;
        [Tooltip("Pattern characters per beat. 2 is eighth notes.")] public int slotsPerBeat;
        [Tooltip("Beats of music after the last round ends before the results show.")] public int waitBeatsAtEnd;

        public CallLayout(int firstBeat, int beatsPerBar, int callBars, int slotsPerBeat)
        {
            this.firstBeat = firstBeat;
            this.beatsPerBar = beatsPerBar;
            this.callBars = callBars;
            this.slotsPerBeat = slotsPerBeat;
            waitBeatsAtEnd = 0;
        }

        public static CallLayout Default => new CallLayout(0, 4, 2, 2);

        public int CallBeats => beatsPerBar * callBars;
        public int RoundBeats => CallBeats * 2;
        public int SlotsPerCall => CallBeats * slotsPerBeat;
        public double CallStart(int round) => firstBeat + round * RoundBeats;
        public double ResponseStart(int round) => CallStart(round) + CallBeats;
        public double RoundEnd(int round) => CallStart(round) + RoundBeats;
        public double ResultsBeat(int roundCount) => RoundEnd(Math.Max(0, roundCount - 1)) + Math.Max(0, waitBeatsAtEnd);
    }

    /// <summary>
    /// A chart for the crushing minigame: a list of call and response rounds played back to back. Only the
    /// responses are judged; the calls are for the book to play.
    /// </summary>
    [CreateAssetMenu(menuName = "Brewing/Call Response Chart (crushing)", fileName = "CallResponseChart")]
    public class CallResponseChart : RhythmChart
    {
        public CallLayout layout = CallLayout.Default;
        public CallRound[] rounds = { new CallRound() };

        /// <summary>The beat the results show on: the end of the last round plus the layout's wait.</summary>
        public double ResultsBeat => layout.ResultsBeat(rounds?.Length ?? 0);

        public List<CallNote> BuildCallNotes() => CallResponseBuilder.CallNotes(layout, Patterns());

        /// <summary>The round playing at this beat (call or response), or -1 before the first and after the last.</summary>
        public int RoundAt(double beat)
        {
            if (layout.RoundBeats <= 0 || beat < layout.firstBeat || rounds == null) return -1;
            int round = (int)Math.Floor((beat - layout.firstBeat) / layout.RoundBeats);
            return round < rounds.Length ? round : -1;
        }

        public override List<RhythmEvent> BuildEvents() => CallResponseBuilder.Build(layout, Patterns());

        protected override List<string> ValidateEvents() => CallResponseBuilder.Validate(layout, Patterns());

        string[] Patterns()
        {
            if (rounds == null) return Array.Empty<string>();
            var patterns = new string[rounds.Length];
            for (int i = 0; i < rounds.Length; i++) patterns[i] = rounds[i]?.pattern;
            return patterns;
        }
    }

    public static class CallResponseBuilder
    {
        public const int LeftButton = 0;
        public const int RightButton = 1;

        /// <summary>The notes each call plays, in beat order. A note's Round is its round's position in the chart.</summary>
        public static List<CallNote> CallNotes(CallLayout layout, IReadOnlyList<string> patterns)
        {
            var notes = new List<CallNote>();
            for (int round = 0; round < patterns.Count; round++)
            {
                int slot = 0;
                foreach (char c in patterns[round] ?? "")
                {
                    if (IsSpacer(c)) continue;
                    int button = ButtonFor(c);
                    if (button >= 0 && slot < layout.SlotsPerCall)
                        notes.Add(new CallNote(round, layout.CallStart(round) + (double)slot / layout.slotsPerBeat, button));
                    slot++;
                }
            }
            return notes;
        }

        /// <summary>
        /// One press per call note, a call length later. Each event's Group holds its round.
        /// </summary>
        public static List<RhythmEvent> Build(CallLayout layout, IReadOnlyList<string> patterns)
        {
            var events = new List<RhythmEvent>();
            foreach (var note in CallNotes(layout, patterns))
                events.Add(new RhythmEvent(events.Count, note.Beat + layout.CallBeats, new RhythmAction(note.Button, InputKind.Press), note.Round));
            return events;
        }

        public static List<string> Validate(CallLayout layout, IReadOnlyList<string> patterns)
        {
            var problems = new List<string>();
            if (layout.beatsPerBar <= 0 || layout.callBars <= 0 || layout.slotsPerBeat <= 0)
            {
                problems.Add("Beats per bar, call bars and slots per beat must all be at least 1.");
                return problems;
            }
            if (patterns.Count == 0) problems.Add("Call and response chart has no rounds.");

            for (int round = 0; round < patterns.Count; round++)
            {
                int slots = 0;
                int notes = 0;
                foreach (char c in patterns[round] ?? "")
                {
                    if (IsSpacer(c)) continue;
                    if (ButtonFor(c) >= 0) notes++;
                    else if (c != '.') problems.Add($"Round {round + 1}: '{c}' is not L, R or '.'.");
                    slots++;
                }
                if (slots > layout.SlotsPerCall)
                    problems.Add($"Round {round + 1} has {slots} slots but a call only fits {layout.SlotsPerCall}; the extra notes are dropped.");
                if (notes == 0) problems.Add($"Round {round + 1} has no notes.");
            }
            return problems;
        }

        static bool IsSpacer(char c) => c == ' ' || c == '|';

        static int ButtonFor(char c)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'L': return LeftButton;
                case 'R': return RightButton;
                default: return -1;
            }
        }
    }
}
