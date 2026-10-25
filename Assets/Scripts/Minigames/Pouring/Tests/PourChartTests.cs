using System.Linq;
using NUnit.Framework;
using Brewing.Rhythm;

namespace Brewing.Pouring.Tests
{
    public class PourChartTests
    {
        [Test]
        public void SmallAndLargeBottlesHoldForTheirLengths()
        {
            var windows = PourChartBuilder.Windows(new[]
            {
                new BottleEntry(4, BottleSize.Small),
                new BottleEntry(10, BottleSize.Large),
            }, 4, 8);

            Assert.AreEqual(2, windows.Count);
            Assert.AreEqual(4, windows[0].Start);
            Assert.AreEqual(8, windows[0].End);
            Assert.AreEqual(10, windows[1].Start);
            Assert.AreEqual(18, windows[1].End);
        }

        [Test]
        public void BottlesAreOrderedByArrivalAndNumberedInThatOrder()
        {
            var windows = PourChartBuilder.Windows(new[]
            {
                new BottleEntry(20, BottleSize.Small),
                new BottleEntry(4, BottleSize.Large),
            }, 4, 8);

            Assert.AreEqual(4, windows[0].Start);
            Assert.AreEqual(0, windows[0].Bottle);
            Assert.AreEqual(20, windows[1].Start);
            Assert.AreEqual(1, windows[1].Bottle);
        }

        [Test]
        public void EachBottleIsAChordOfPressesAndAChordOfReleases()
        {
            var windows = PourChartBuilder.Windows(new[] { new BottleEntry(4, BottleSize.Small) }, 4, 8);
            var events = PourChartBuilder.Build(windows);

            Assert.AreEqual(4, events.Count);
            CollectionAssert.AreEqual(new[] { 4.0, 4.0, 8.0, 8.0 }, events.Select(e => e.Beat).ToArray());
            Assert.AreEqual(new RhythmAction(0, InputKind.Press), events[0].Action);
            Assert.AreEqual(new RhythmAction(1, InputKind.Press), events[1].Action);
            Assert.AreEqual(new RhythmAction(0, InputKind.Release), events[2].Action);
            Assert.AreEqual(new RhythmAction(1, InputKind.Release), events[3].Action);
            Assert.IsTrue(events.All(e => e.SectionIndex == 0), "events carry their bottle number");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, events.Select(e => e.Index).ToArray());
        }

        [Test]
        public void ValidationFlagsBottlesThatArriveBeforeThePreviousPourEnds()
        {
            var problems = PourChartBuilder.Validate(new[]
            {
                new BottleEntry(0, BottleSize.Large),   // 0 to 8
                new BottleEntry(6, BottleSize.Small),   // overlaps
            }, 4, 8, 1);
            Assert.AreEqual(1, problems.Count);
        }

        [Test]
        public void ValidationEnforcesTheMinimumGap()
        {
            var tooClose = PourChartBuilder.Validate(new[]
            {
                new BottleEntry(0, BottleSize.Small),   // 0 to 4
                new BottleEntry(4.5, BottleSize.Small),
            }, 4, 8, 1);
            Assert.AreEqual(1, tooClose.Count);

            var fine = PourChartBuilder.Validate(new[]
            {
                new BottleEntry(0, BottleSize.Small),
                new BottleEntry(5, BottleSize.Small),
            }, 4, 8, 1);
            Assert.IsEmpty(fine);
        }

        [Test]
        public void ValidationFlagsAnEmptyChart()
        {
            Assert.IsNotEmpty(PourChartBuilder.Validate(new BottleEntry[0], 4, 8, 1));
        }

        [Test]
        public void TheChartAssetBuildsBottleEventsInsteadOfSections()
        {
            var chart = UnityEngine.ScriptableObject.CreateInstance<PourChart>();
            chart.bottles = new[] { new BottleEntry(2, BottleSize.Large) };
            var events = chart.BuildEvents();
            var problems = chart.Validate();
            UnityEngine.Object.DestroyImmediate(chart);

            Assert.AreEqual(4, events.Count);
            Assert.AreEqual(10.0, events[3].Beat);
            Assert.IsEmpty(problems);
        }
    }
}
