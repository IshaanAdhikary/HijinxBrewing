using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Brewing.Rhythm.Tests
{
    public class CueSchedulerTests
    {
        static List<ChartCue> Advance(CueScheduler scheduler, double beat)
        {
            var due = new List<ChartCue>();
            scheduler.Advance(beat, due);
            return due;
        }

        [Test]
        public void CuesFireWhenTheirBeatIsReached()
        {
            var scheduler = new CueScheduler(new[] { new ChartCue(32, "FadeHelper") });
            Assert.IsEmpty(Advance(scheduler, 31.99));

            var due = Advance(scheduler, 32.0);
            Assert.AreEqual(1, due.Count);
            Assert.AreEqual("FadeHelper", due[0].name);
        }

        [Test]
        public void EachCueFiresOnlyOnce()
        {
            var scheduler = new CueScheduler(new[] { new ChartCue(4, "A") });
            Assert.AreEqual(1, Advance(scheduler, 4.0).Count);
            Assert.IsEmpty(Advance(scheduler, 4.5));
            Assert.IsEmpty(Advance(scheduler, 100));
            Assert.AreEqual(0, scheduler.PendingCount);
        }

        [Test]
        public void CuesAreReturnedInBeatOrderEvenIfAuthoredOutOfOrder()
        {
            var scheduler = new CueScheduler(new[] { new ChartCue(20, "Late"), new ChartCue(5, "Early"), new ChartCue(10, "Middle") });
            var due = Advance(scheduler, 50);
            CollectionAssert.AreEqual(new[] { "Early", "Middle", "Late" }, due.Select(c => c.name).ToArray());
        }

        [Test]
        public void ASlowFrameReturnsEveryCueItPassed()
        {
            var scheduler = new CueScheduler(new[] { new ChartCue(1, "A"), new ChartCue(2, "B"), new ChartCue(9, "C") });
            Assert.AreEqual(2, Advance(scheduler, 3).Count);
            Assert.AreEqual(1, scheduler.PendingCount);
        }

        [Test]
        public void CuesDuringTheLeadInFireOnceTheSongReachesThem()
        {
            var scheduler = new CueScheduler(new[] { new ChartCue(-2, "Intro") });
            Assert.IsEmpty(Advance(scheduler, -4));
            Assert.AreEqual(1, Advance(scheduler, -1.5).Count);
        }

        [Test]
        public void NoCuesIsFine()
        {
            Assert.IsEmpty(Advance(new CueScheduler(null), 10));
            Assert.IsEmpty(Advance(new CueScheduler(new ChartCue[0]), 10));
        }

        [Test]
        public void ChartValidationFlagsUnnamedCues()
        {
            var chart = UnityEngine.ScriptableObject.CreateInstance<PatternChart>();
            var song = UnityEngine.ScriptableObject.CreateInstance<Song>();
            chart.song = song;
            chart.cues = new[] { new ChartCue(4, "Fine"), new ChartCue(8, "  ") };
            var problems = chart.Validate();
            UnityEngine.Object.DestroyImmediate(chart);
            UnityEngine.Object.DestroyImmediate(song);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("no name", problems[0]);
        }
    }
}
