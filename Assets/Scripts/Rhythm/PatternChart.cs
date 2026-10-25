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

        public ChartSection[] sections =
        {
            new ChartSection(0, 16, TempoMode.Normal),
        };

        public override List<RhythmEvent> BuildEvents() => ChartBuilder.Build(sections, actionPattern);

        protected override List<string> ValidateEvents() => ChartBuilder.Validate(sections, actionPattern);
    }
}
