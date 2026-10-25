using NUnit.Framework;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Mixing.Tests
{
    public class StirPathTests
    {
        static readonly RhythmAction[] Stir =
        {
            new RhythmAction(0, InputKind.Press),
            new RhythmAction(0, InputKind.Release),
            new RhythmAction(1, InputKind.Press),
            new RhythmAction(1, InputKind.Release),
        };

        static StirPath Make(params ChartSection[] sections)
        {
            return new StirPath(ChartBuilder.Build(sections, Stir), sections);
        }

        // 0 = right, 90 = top, 180 = left, 270 = bottom.
        static double Wrap(double degrees) => ((degrees % 360.0) + 360.0) % 360.0;

        const double Bottom = 270, Left = 180, Top = 90, Right = 0;

        [Test]
        public void SpoonLeavesOnTheActionAndArrivesOnTheNextOne()
        {
            // Normal time: actions on beats 0 (press left), 1 (release), 2 (press right), 3 (release).
            var path = Make(new ChartSection(0, 8, TempoMode.Normal));
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(0)), 1e-6, "still at the bottom as the press lands");
            Assert.AreEqual(Left, Wrap(path.AngleAt(1)), 1e-6, "reaches the left edge as you release");
            Assert.AreEqual(Top, Wrap(path.AngleAt(2)), 1e-6, "reaches the top as you press right");
            Assert.AreEqual(Right, Wrap(path.AngleAt(3)), 1e-6, "reaches the right edge as you release");
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(4)), 1e-6, "back at the bottom as the next stir begins");
            Assert.AreEqual(Left, Wrap(path.AngleAt(5)), 1e-6);
        }

        [Test]
        public void SpoonTravelsClockwiseBetweenStops()
        {
            var path = Make(new ChartSection(0, 8, TempoMode.Normal));
            Assert.AreEqual(225.0, Wrap(path.AngleAt(0.5)), 1e-6, "halfway from bottom to left is lower left");
            Assert.AreEqual(135.0, Wrap(path.AngleAt(1.5)), 1e-6, "halfway from left to top is upper left");
        }

        [Test]
        public void SpoonRestsAtTheBottomBeforeTheFirstAction()
        {
            var path = Make(new ChartSection(0, 8, TempoMode.Normal));
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(-4)), 1e-6);
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(-0.01)), 1e-6);
        }

        [Test]
        public void SpoonFinishesAtTheBottomAfterTheLastStir()
        {
            var path = Make(new ChartSection(0, 8, TempoMode.Normal));
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(8)), 1e-6);
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(50)), 1e-6);
        }

        [Test]
        public void HalfTimeTakesTwoBeatsPerStop()
        {
            // Last normal action is on beat 3, so the spoon reaches the bottom at beat 4 as the half-time
            // section begins. Half time then spends two beats on each stop.
            var path = Make(new ChartSection(0, 4, TempoMode.Normal), new ChartSection(4, 8, TempoMode.Half));
            Assert.AreEqual(315.0, Wrap(path.AngleAt(3.5)), 1e-6, "halfway home from the right edge");
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(4)), 1e-6);
            Assert.AreEqual(225.0, Wrap(path.AngleAt(5)), 1e-6);
            Assert.AreEqual(Left, Wrap(path.AngleAt(6)), 1e-6);
            Assert.AreEqual(Top, Wrap(path.AngleAt(8)), 1e-6);
        }

        [Test]
        public void DoubleTimeStopsAreHalfABeatApart()
        {
            var path = Make(new ChartSection(0, 8, TempoMode.Half), new ChartSection(8, 2, TempoMode.Double));
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(8.0)), 1e-6);
            Assert.AreEqual(225.0, Wrap(path.AngleAt(8.25)), 1e-6);
            Assert.AreEqual(Left, Wrap(path.AngleAt(8.5)), 1e-6);
            Assert.AreEqual(Top, Wrap(path.AngleAt(9.0)), 1e-6);
            Assert.AreEqual(Right, Wrap(path.AngleAt(9.5)), 1e-6);
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(10.0)), 1e-6);
        }

        [Test]
        public void SpoonWaitsAtTheBottomAcrossAGapBetweenSections()
        {
            var path = Make(new ChartSection(0, 4, TempoMode.Normal), new ChartSection(8, 4, TempoMode.Normal));
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(5)), 1e-6);
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(8)), 1e-6);
            Assert.AreEqual(225.0, Wrap(path.AngleAt(8.5)), 1e-6);
        }

        [Test]
        public void PointOnRingMatchesTheAngles()
        {
            Assert.AreEqual(0f, StirPath.PointOnRing(-90, 2f).x, 1e-5f);
            Assert.AreEqual(-2f, StirPath.PointOnRing(-90, 2f).y, 1e-5f);
            Assert.AreEqual(-2f, StirPath.PointOnRing(-180, 2f).x, 1e-5f);
            Assert.AreEqual(2f, StirPath.PointOnRing(-270, 2f).y, 1e-5f);
            Assert.AreEqual(2f, StirPath.PointOnRing(-360, 2f).x, 1e-5f);
        }

        [Test]
        public void EmptyChartKeepsTheSpoonAtTheBottom()
        {
            var path = new StirPath(new System.Collections.Generic.List<RhythmEvent>(), new ChartSection[0]);
            Assert.AreEqual(Bottom, Wrap(path.AngleAt(5)), 1e-6);
        }
    }
}
