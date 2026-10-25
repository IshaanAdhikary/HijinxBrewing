using System.Linq;
using NUnit.Framework;
using Brewing.Rhythm;

namespace Brewing.Crushing.Tests
{
    public class CallResponseChartTests
    {
        static readonly CallLayout Layout = CallLayout.Default;

        [Test]
        public void CallNotesFollowTheEighthNoteGrid()
        {
            var notes = CallResponseBuilder.CallNotes(Layout, new[] { "L.R.L... | R...L.R." });

            Assert.AreEqual(new[] { 0.0, 1.0, 2.0, 4.0, 6.0, 7.0 }, notes.Select(n => n.Beat).ToArray());
            Assert.AreEqual(new[] { 0, 1, 0, 1, 0, 1 }, notes.Select(n => n.Button).ToArray());
        }

        [Test]
        public void OffBeatSlotsLandOnHalfBeats()
        {
            var notes = CallResponseBuilder.CallNotes(Layout, new[] { ".L.R" });
            Assert.AreEqual(new[] { 0.5, 1.5 }, notes.Select(n => n.Beat).ToArray());
        }

        [Test]
        public void ResponsesArePressesOneCallLengthLater()
        {
            var events = CallResponseBuilder.Build(Layout, new[] { "L..R" });

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(8.0, events[0].Beat);
            Assert.AreEqual(9.5, events[1].Beat);
            Assert.AreEqual(new RhythmAction(0, InputKind.Press), events[0].Action);
            Assert.AreEqual(new RhythmAction(1, InputKind.Press), events[1].Action);
        }

        [Test]
        public void RoundsFollowEachOtherAndCarryTheirRoundAsGroup()
        {
            var events = CallResponseBuilder.Build(Layout, new[] { "L", "R" });

            Assert.AreEqual(new[] { 8.0, 24.0 }, events.Select(e => e.Beat).ToArray());
            Assert.AreEqual(new[] { 0, 1 }, events.Select(e => e.Group).ToArray());
            Assert.AreEqual(new[] { 0, 1 }, events.Select(e => e.Index).ToArray());
        }

        [Test]
        public void FirstBeatShiftsEveryRound()
        {
            var layout = new CallLayout(4, 4, 2, 2);
            var notes = CallResponseBuilder.CallNotes(layout, new[] { "L", "R" });
            Assert.AreEqual(new[] { 4.0, 20.0 }, notes.Select(n => n.Beat).ToArray());
        }

        [Test]
        public void LowercaseWorksAndSpacersAreIgnored()
        {
            var notes = CallResponseBuilder.CallNotes(Layout, new[] { "l | r" });
            Assert.AreEqual(new[] { 0.0, 0.5 }, notes.Select(n => n.Beat).ToArray());
        }

        [Test]
        public void NotesPastTheEndOfTheCallAreDroppedAndReported()
        {
            string tooLong = new string('.', 16) + "L";
            Assert.AreEqual(0, CallResponseBuilder.CallNotes(Layout, new[] { tooLong }).Count);
            Assert.IsTrue(CallResponseBuilder.Validate(Layout, new[] { tooLong }).Any(p => p.Contains("only fits 16")));
        }

        [Test]
        public void ValidationReportsUnknownCharactersAndEmptyRounds()
        {
            var problems = CallResponseBuilder.Validate(Layout, new[] { "L.X.", "...." });
            Assert.IsTrue(problems.Any(p => p.Contains("'X'")));
            Assert.IsTrue(problems.Any(p => p.Contains("Round 2 has no notes")));
        }

        [Test]
        public void RoundAtCoversCallAndResponseAndNothingOutside()
        {
            var chart = UnityEngine.ScriptableObject.CreateInstance<CallResponseChart>();
            chart.rounds = new[] { new CallRound(), new CallRound() };

            Assert.AreEqual(-1, chart.RoundAt(-0.5));
            Assert.AreEqual(0, chart.RoundAt(0.0));
            Assert.AreEqual(0, chart.RoundAt(15.9));
            Assert.AreEqual(1, chart.RoundAt(16.0));
            Assert.AreEqual(-1, chart.RoundAt(32.0));
            UnityEngine.Object.DestroyImmediate(chart);
        }

        [Test]
        public void ValidChartHasNoProblems()
        {
            Assert.IsEmpty(CallResponseBuilder.Validate(Layout, new[] { "L.R.L... | R...L.R.", "LLRR" }));
        }
    }
}
