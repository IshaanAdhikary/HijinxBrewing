using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Pouring
{
    public enum BottleSize { Small, Large }

    /// <summary>One bottle in a pour chart: when it arrives under the spout, and how big it is.</summary>
    [Serializable]
    public struct BottleEntry
    {
        [Tooltip("Beat the bottle reaches the spout. Pour (press both keys) on this beat.")]
        public double arriveBeat;
        public BottleSize size;

        public BottleEntry(double arriveBeat, BottleSize size)
        {
            this.arriveBeat = arriveBeat;
            this.size = size;
        }
    }

    /// <summary>A bottle's pour, from the beat you press both keys to the beat you release them.</summary>
    public readonly struct PourWindow
    {
        public readonly int Bottle;
        public readonly double Start;
        public readonly double End;
        public readonly BottleSize Size;

        public PourWindow(int bottle, double start, double end, BottleSize size)
        {
            Bottle = bottle;
            Start = start;
            End = end;
            Size = size;
        }

        public double Length => End - Start;
    }

    /// <summary>
    /// A chart for the pouring minigame: a list of bottles instead of tempo sections. Each bottle becomes a
    /// chord of both presses on its arrival beat and a chord of both releases a hold length later.
    /// The tempo sections and action pattern inherited from RhythmChart are not used.
    /// </summary>
    [CreateAssetMenu(menuName = "Brewing/Pour Chart", fileName = "PourChart")]
    public class PourChart : RhythmChart
    {
        [Header("Bottles")]
        [Tooltip("Beats to hold for a small bottle.")] public int smallHoldBeats = 4;
        [Tooltip("Beats to hold for a large bottle.")] public int largeHoldBeats = 8;
        [Tooltip("Smallest allowed gap, in beats, between one bottle's release and the next bottle's arrival.")] public double minimumGapBeats = 1;
        public BottleEntry[] bottles = new BottleEntry[0];

        public List<PourWindow> BuildWindows() => PourChartBuilder.Windows(bottles, smallHoldBeats, largeHoldBeats);

        public override List<RhythmEvent> BuildEvents() => PourChartBuilder.Build(BuildWindows());

        protected override List<string> ValidateEvents() =>
            PourChartBuilder.Validate(bottles, smallHoldBeats, largeHoldBeats, minimumGapBeats);
    }

    public static class PourChartBuilder
    {
        public const int LeftButton = 0;
        public const int RightButton = 1;

        /// <summary>The pour windows in beat order. A window's Bottle is its position in that order.</summary>
        public static List<PourWindow> Windows(IEnumerable<BottleEntry> bottles, int smallHoldBeats, int largeHoldBeats)
        {
            var windows = new List<PourWindow>();
            if (bottles == null) return windows;

            foreach (var bottle in bottles.OrderBy(b => b.arriveBeat))
            {
                int hold = bottle.size == BottleSize.Small ? smallHoldBeats : largeHoldBeats;
                windows.Add(new PourWindow(windows.Count, bottle.arriveBeat, bottle.arriveBeat + hold, bottle.size));
            }
            return windows;
        }

        /// <summary>
        /// Four events per bottle: press left and right on its start beat, release both on its end beat.
        /// Each event's SectionIndex holds the bottle it belongs to.
        /// </summary>
        public static List<RhythmEvent> Build(IReadOnlyList<PourWindow> windows)
        {
            var events = new List<RhythmEvent>();
            foreach (var window in windows)
            {
                Add(events, window.Start, LeftButton, InputKind.Press, window.Bottle);
                Add(events, window.Start, RightButton, InputKind.Press, window.Bottle);
                Add(events, window.End, LeftButton, InputKind.Release, window.Bottle);
                Add(events, window.End, RightButton, InputKind.Release, window.Bottle);
            }
            return events;
        }

        public static List<string> Validate(IReadOnlyList<BottleEntry> bottles, int smallHoldBeats, int largeHoldBeats, double minimumGapBeats)
        {
            var problems = new List<string>();
            if (smallHoldBeats <= 0 || largeHoldBeats <= 0) problems.Add("Hold lengths must be at least one beat.");
            if (bottles == null || bottles.Count == 0)
            {
                problems.Add("Pour chart has no bottles.");
                return problems;
            }

            var windows = Windows(bottles, smallHoldBeats, largeHoldBeats);
            for (int i = 1; i < windows.Count; i++)
            {
                double gap = windows[i].Start - windows[i - 1].End;
                if (gap < minimumGapBeats)
                    problems.Add($"Bottle arriving at beat {windows[i].Start} comes {gap} beats after the previous release at beat {windows[i - 1].End}; the minimum gap is {minimumGapBeats}.");
            }
            return problems;
        }

        static void Add(List<RhythmEvent> events, double beat, int button, InputKind kind, int bottle)
        {
            events.Add(new RhythmEvent(events.Count, beat, new RhythmAction(button, kind), bottle));
        }
    }
}
