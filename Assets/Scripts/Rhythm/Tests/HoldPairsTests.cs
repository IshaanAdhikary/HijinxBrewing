using System.Collections.Generic;
using NUnit.Framework;

namespace Brewing.Rhythm.Tests
{
    public class HoldPairsTests
    {
        static RhythmEvent Event(int index, double beat, int button, InputKind kind)
        {
            return new RhythmEvent(index, beat, new RhythmAction(button, kind), 0);
        }

        [Test]
        public void EachPressPairsWithTheFollowingReleaseOfTheSameButton()
        {
            var events = ChartBuilder.Build(new[] { new ChartSection(0, 8, TempoMode.Normal) }, new[]
            {
                new RhythmAction(0, InputKind.Press),
                new RhythmAction(0, InputKind.Release),
                new RhythmAction(1, InputKind.Press),
                new RhythmAction(1, InputKind.Release),
            });

            CollectionAssert.AreEqual(new[] { 1, -1, 3, -1, 5, -1, 7, -1 }, HoldPairs.Compute(events));
        }

        [Test]
        public void OverlappingHoldsOnDifferentButtonsPairCorrectly()
        {
            var events = new List<RhythmEvent>
            {
                Event(0, 0, 0, InputKind.Press),
                Event(1, 1, 1, InputKind.Press),
                Event(2, 2, 0, InputKind.Release),
                Event(3, 3, 1, InputKind.Release),
            };
            CollectionAssert.AreEqual(new[] { 2, 3, -1, -1 }, HoldPairs.Compute(events));
        }

        [Test]
        public void APressFollowedByAnotherPressOfTheSameButtonHasNoHold()
        {
            var events = new List<RhythmEvent>
            {
                Event(0, 0, 0, InputKind.Press),
                Event(1, 1, 0, InputKind.Press),
                Event(2, 2, 0, InputKind.Release),
            };
            CollectionAssert.AreEqual(new[] { -1, 2, -1 }, HoldPairs.Compute(events));
        }

        [Test]
        public void ATrailingPressWithNoReleaseHasNoHold()
        {
            var events = new List<RhythmEvent> { Event(0, 0, 0, InputKind.Press) };
            CollectionAssert.AreEqual(new[] { -1 }, HoldPairs.Compute(events));
        }
    }
}
