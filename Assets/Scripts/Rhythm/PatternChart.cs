using System.Collections.Generic;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// A chart made of tempo sections, used by mixing. Within each section the action pattern repeats, one
    /// action per event spacing (half, normal or double time), restarting at the start of every section.
    /// </summary>
    [CreateAssetMenu(menuName = "Brewing/Pattern Chart (mixing)", fileName = "PatternChart")]
    public class PatternChart : RhythmChart
    {
        [Tooltip("The repeating cycle of actions. Default is the cauldron stir: hold left, release, hold right, release.")]
        public RhythmAction[] actionPattern =
        {
            new RhythmAction(0, InputKind.Press),
            new RhythmAction(0, InputKind.Release),
            new RhythmAction(1, InputKind.Press),
            new RhythmAction(1, InputKind.Release),
        };

        [Tooltip("Beat the first section starts on. Each later section starts where the previous one ends.")]
        public int firstBeat;

        [Tooltip("Played back to back. Start beats are worked out from the lengths, so only set Length and Mode.")]
        public ChartSection[] sections =
        {
            new ChartSection(0, 16, TempoMode.Normal),
        };

        [Tooltip("Beats of music after the last section ends before the results show.")]
        public int waitBeatsAtEnd;

        /// <summary>The beat the last section ends on.</summary>
        public int EndBeat
        {
            get
            {
                int end = 0;
                if (sections != null)
                    foreach (var section in sections) end = Mathf.Max(end, section.EndBeat);
                return end;
            }
        }

        /// <summary>The beat the results show on: the end of the last section plus <see cref="waitBeatsAtEnd"/>.</summary>
        public double ResultsBeat => EndBeat + Mathf.Max(0, waitBeatsAtEnd);

        public override List<RhythmEvent> BuildEvents()
        {
            ChartBuilder.LayOut(sections, firstBeat);
            return ChartBuilder.Build(sections, actionPattern);
        }

        protected override List<string> ValidateEvents()
        {
            ChartBuilder.LayOut(sections, firstBeat);
            return ChartBuilder.Validate(sections, actionPattern);
        }

        // Keeps the start beats shown in the Inspector up to date as lengths are edited.
        void OnValidate() => ChartBuilder.LayOut(sections, firstBeat);
    }
}
