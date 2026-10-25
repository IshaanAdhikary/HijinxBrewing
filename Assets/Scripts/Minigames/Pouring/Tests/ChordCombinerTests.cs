using NUnit.Framework;
using Brewing.Rhythm;

namespace Brewing.Pouring.Tests
{
    public class ChordCombinerTests
    {
        static JudgeResult Result(int bottle, int button, InputKind kind, Judgement judgement, double error)
        {
            var evt = new RhythmEvent(0, 0, new RhythmAction(button, kind), bottle);
            return new JudgeResult(evt, judgement, error);
        }

        [Test]
        public void WaitsForBothKeys()
        {
            var combiner = new ChordCombiner();
            Assert.IsFalse(combiner.Add(Result(0, 0, InputKind.Press, Judgement.Perfect, 0.01), out _));
            Assert.IsTrue(combiner.Add(Result(0, 1, InputKind.Press, Judgement.Perfect, 0.02), out _));
        }

        [Test]
        public void ReportsTheWorseRating()
        {
            var combiner = new ChordCombiner();
            combiner.Add(Result(0, 0, InputKind.Press, Judgement.Perfect, 0.01), out _);
            combiner.Add(Result(0, 1, InputKind.Press, Judgement.Good, 0.08), out var combined);
            Assert.AreEqual(Judgement.Good, combined.Judgement);

            combiner.Add(Result(0, 0, InputKind.Release, Judgement.Miss, 0), out _);
            combiner.Add(Result(0, 1, InputKind.Release, Judgement.Perfect, 0.0), out combined);
            Assert.AreEqual(Judgement.Miss, combined.Judgement);
        }

        [Test]
        public void ForEqualRatingsReportsTheOneFurtherOffTheBeat()
        {
            var combiner = new ChordCombiner();
            combiner.Add(Result(0, 0, InputKind.Press, Judgement.Good, -0.09), out _);
            combiner.Add(Result(0, 1, InputKind.Press, Judgement.Good, 0.06), out var combined);
            Assert.AreEqual(-0.09, combined.ErrorSeconds, 1e-9);
        }

        [Test]
        public void PressesAndReleasesAndBottlesAreKeptSeparate()
        {
            var combiner = new ChordCombiner();
            Assert.IsFalse(combiner.Add(Result(0, 0, InputKind.Press, Judgement.Perfect, 0), out _));
            Assert.IsFalse(combiner.Add(Result(0, 0, InputKind.Release, Judgement.Perfect, 0), out _), "a release does not complete a press");
            Assert.IsFalse(combiner.Add(Result(1, 1, InputKind.Press, Judgement.Perfect, 0), out _), "another bottle does not complete it either");
            Assert.IsTrue(combiner.Add(Result(0, 1, InputKind.Press, Judgement.Perfect, 0), out _));
        }

        [Test]
        public void ResetForgetsHalfFinishedChords()
        {
            var combiner = new ChordCombiner();
            combiner.Add(Result(0, 0, InputKind.Press, Judgement.Perfect, 0), out _);
            combiner.Reset();
            Assert.IsFalse(combiner.Add(Result(0, 1, InputKind.Press, Judgement.Perfect, 0), out _));
        }
    }
}
