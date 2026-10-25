using System.Linq;
using NUnit.Framework;

namespace Brewing.Rhythm.Tests
{
    public class ChartBuilderTests
    {
        static readonly RhythmAction[] Stir =
        {
            new RhythmAction(0, InputKind.Press),
            new RhythmAction(0, InputKind.Release),
            new RhythmAction(1, InputKind.Press),
            new RhythmAction(1, InputKind.Release),
        };

        [Test]
        public void EventSpacingFollowsTempoMode()
        {
            var events = ChartBuilder.Build(new[]
            {
                new ChartSection(0, 8, TempoMode.Half),
                new ChartSection(8, 4, TempoMode.Normal),
                new ChartSection(12, 2, TempoMode.Double),
            }, Stir);

            Assert.AreEqual(new[] { 0.0, 2.0, 4.0, 6.0 }, events.Where(e => e.Group == 0).Select(e => e.Beat).ToArray());
            Assert.AreEqual(new[] { 8.0, 9.0, 10.0, 11.0 }, events.Where(e => e.Group == 1).Select(e => e.Beat).ToArray());
            Assert.AreEqual(new[] { 12.0, 12.5, 13.0, 13.5 }, events.Where(e => e.Group == 2).Select(e => e.Beat).ToArray());
        }

        [Test]
        public void ActionsCycleThroughThePattern()
        {
            var events = ChartBuilder.Build(new[] { new ChartSection(0, 8, TempoMode.Normal) }, Stir);
            Assert.AreEqual(8, events.Count);
            for (int i = 0; i < events.Count; i++)
                Assert.AreEqual(Stir[i % 4], events[i].Action, $"event {i}");
        }

        [Test]
        public void EachSectionRestartsThePattern()
        {
            var events = ChartBuilder.Build(new[]
            {
                new ChartSection(0, 2, TempoMode.Normal),  // two events: ends mid-cycle
                new ChartSection(4, 4, TempoMode.Normal),
            }, Stir);

            var secondSection = events.Where(e => e.Group == 1).ToList();
            Assert.AreEqual(Stir[0], secondSection[0].Action);
        }

        [Test]
        public void EventIndicesAreSequential()
        {
            var events = ChartBuilder.Build(new[]
            {
                new ChartSection(0, 4, TempoMode.Normal),
                new ChartSection(4, 4, TempoMode.Normal),
            }, Stir);
            for (int i = 0; i < events.Count; i++) Assert.AreEqual(i, events[i].Index);
        }

        [Test]
        public void ValidChartHasNoProblems()
        {
            var problems = ChartBuilder.Validate(new[]
            {
                new ChartSection(0, 16, TempoMode.Normal),
                new ChartSection(16, 16, TempoMode.Half),
                new ChartSection(32, 16, TempoMode.Double),
            }, Stir);
            Assert.IsEmpty(problems);
        }

        [Test]
        public void ValidateFlagsSectionsThatEndMidCycle()
        {
            // Half time needs 8 beats per revolution; 12 beats is 6 events.
            var problems = ChartBuilder.Validate(new[] { new ChartSection(0, 12, TempoMode.Half) }, Stir);
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("mid-cycle", problems[0]);
        }

        [Test]
        public void ValidateFlagsOverlapAndEmptyInputs()
        {
            var overlap = ChartBuilder.Validate(new[]
            {
                new ChartSection(0, 8, TempoMode.Normal),
                new ChartSection(4, 8, TempoMode.Normal),
            }, Stir);
            Assert.IsTrue(overlap.Any(p => p.Contains("before the previous section ends")));

            Assert.IsNotEmpty(ChartBuilder.Validate(new ChartSection[0], Stir));
            Assert.IsNotEmpty(ChartBuilder.Validate(new[] { new ChartSection(0, 4, TempoMode.Normal) }, new RhythmAction[0]));
        }
    }
}
