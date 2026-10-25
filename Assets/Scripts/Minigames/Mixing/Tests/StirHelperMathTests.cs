using NUnit.Framework;
using Brewing.Rhythm;

namespace Brewing.Mixing.Tests
{
    public class StirHelperMathTests
    {
        static readonly RhythmAction[] Stir =
        {
            new RhythmAction(0, InputKind.Press),
            new RhythmAction(0, InputKind.Release),
            new RhythmAction(1, InputKind.Press),
            new RhythmAction(1, InputKind.Release),
        };

        [Test]
        public void EventsCycleThroughTheFourCompassSlots()
        {
            var events = ChartBuilder.Build(new[] { new ChartSection(0, 8, TempoMode.Normal) }, Stir);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0, 1, 2, 3 }, StirHelperMath.SlotOfEachEvent(events, 4));
        }

        [Test]
        public void EachSectionRestartsAtTheFirstSlot()
        {
            // Two events in the first section, so the second section must start again at slot 0.
            var events = ChartBuilder.Build(new[]
            {
                new ChartSection(0, 2, TempoMode.Normal),
                new ChartSection(4, 4, TempoMode.Normal),
            }, Stir);
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 1, 2, 3 }, StirHelperMath.SlotOfEachEvent(events, 4));
        }

        [Test]
        public void SlotsFollowTheEventCountNotTheTempo()
        {
            var events = ChartBuilder.Build(new[] { new ChartSection(0, 2, TempoMode.Double) }, Stir);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, StirHelperMath.SlotOfEachEvent(events, 4));
        }

        [Test]
        public void HoldArcsRunFromEachPressToItsRelease()
        {
            CollectionAssert.AreEqual(new[] { true, false, true, false }, StirHelperMath.HoldArcStartsAt(Stir));
        }

        [Test]
        public void NoArcWhenTheReleaseIsOfAnotherButton()
        {
            var pattern = new[]
            {
                new RhythmAction(0, InputKind.Press),
                new RhythmAction(1, InputKind.Release),
            };
            CollectionAssert.AreEqual(new[] { false, false }, StirHelperMath.HoldArcStartsAt(pattern));
        }

        [Test]
        public void ApproachShrinksToZeroOverExactlyOneEventSpacing()
        {
            Assert.AreEqual(1f, StirHelperMath.ApproachFraction(10, 9, 1), 1e-6f, "a full spacing away");
            Assert.AreEqual(0.5f, StirHelperMath.ApproachFraction(10, 9.5, 1), 1e-6f);
            Assert.AreEqual(0f, StirHelperMath.ApproachFraction(10, 10, 1), 1e-6f, "due now");
        }

        [Test]
        public void ApproachIsClampedBeforeItStartsAndAfterItPasses()
        {
            Assert.AreEqual(1f, StirHelperMath.ApproachFraction(10, 2, 1), 1e-6f);
            Assert.AreEqual(0f, StirHelperMath.ApproachFraction(10, 10.4, 1), 1e-6f);
        }

        [Test]
        public void ApproachScalesWithTheEventSpacing()
        {
            // Half time: two beats per stop, so one beat before the event is halfway.
            Assert.AreEqual(0.5f, StirHelperMath.ApproachFraction(10, 9, 2), 1e-6f);
            // Double time: half a beat per stop, so half a beat before the event is the full ring.
            Assert.AreEqual(1f, StirHelperMath.ApproachFraction(10, 9.5, 0.5), 1e-6f);
        }
    }
}
