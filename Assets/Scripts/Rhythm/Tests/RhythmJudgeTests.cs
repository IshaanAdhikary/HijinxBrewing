using System.Collections.Generic;
using NUnit.Framework;

namespace Brewing.Rhythm.Tests
{
    public class RhythmJudgeTests
    {
        // 120 BPM: one beat is 0.5s.
        static readonly BeatClock Clock = new BeatClock(120.0, 0.0);

        static readonly RhythmAction PressLeft = new RhythmAction(0, InputKind.Press);
        static readonly RhythmAction ReleaseLeft = new RhythmAction(0, InputKind.Release);
        static readonly RhythmAction PressRight = new RhythmAction(1, InputKind.Press);

        static RhythmJudge Make(out List<JudgeResult> results, params ChartSection[] sections)
        {
            var pattern = new[]
            {
                PressLeft, ReleaseLeft, PressRight, new RhythmAction(1, InputKind.Release),
            };
            var judge = new RhythmJudge(ChartBuilder.Build(sections, pattern), Clock, new JudgeSettings());
            var list = new List<JudgeResult>();
            judge.Judged += list.Add;
            results = list;
            return judge;
        }

        [Test]
        public void ExactHitIsPerfect()
        {
            var judge = Make(out var results, new ChartSection(0, 4, TempoMode.Normal));
            Assert.IsTrue(judge.Input(PressLeft, 0.0, out var r));
            Assert.AreEqual(Judgement.Perfect, r.Judgement);
            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void InsidePerfectWindowIsPerfectAndOutsideItIsGood()
        {
            var judge = Make(out _, new ChartSection(0, 4, TempoMode.Normal));
            judge.Input(PressLeft, 0.04, out var perfect);
            judge.Input(ReleaseLeft, 0.5 - 0.09, out var good);
            Assert.AreEqual(Judgement.Perfect, perfect.Judgement);
            Assert.AreEqual(Judgement.Good, good.Judgement);
            Assert.AreEqual(-0.09, good.ErrorSeconds, 1e-9);
        }

        [Test]
        public void InputTooEarlyIsIgnoredNotJudged()
        {
            var judge = Make(out var results, new ChartSection(0, 4, TempoMode.Normal));
            Assert.IsFalse(judge.Input(PressLeft, -0.2, out _));
            Assert.AreEqual(0, results.Count);
            Assert.AreEqual(0, judge.Cursor);
        }

        [Test]
        public void WrongButtonPressIsAStrayMissAndLeavesTheNoteOpen()
        {
            var judge = Make(out var results, new ChartSection(0, 4, TempoMode.Normal));
            Assert.IsFalse(judge.Input(PressRight, 0.0, out var stray));
            Assert.IsTrue(stray.IsStray);
            Assert.AreEqual(Judgement.Miss, stray.Judgement);
            Assert.AreEqual(-1, stray.Event.Index);
            Assert.AreEqual(PressRight, stray.Event.Action);

            // A wrong release is still ignored, and the right input afterwards still works.
            Assert.IsFalse(judge.Input(ReleaseLeft, 0.0, out _));
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(judge.Input(PressLeft, 0.01, out _));
        }

        [Test]
        public void PressBetweenWindowsIsAStrayMissAtItsBeat()
        {
            var judge = Make(out var results, new ChartSection(0, 4, TempoMode.Normal));
            judge.Input(PressLeft, 0.0, out _);
            judge.Input(PressLeft, 0.25, out var stray);

            Assert.IsTrue(stray.IsStray);
            Assert.AreEqual(0.5, stray.Event.Beat, 1e-9);
            Assert.AreEqual(1, judge.StrayCount);
            Assert.AreEqual(0, judge.MissCount, "strays are not missed notes");
            Assert.AreEqual(1, judge.Cursor, "the next note is still open");
            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void PressesBeforeTheFirstWindowOrAfterTheLastAreFree()
        {
            var judge = Make(out var results, new ChartSection(0, 4, TempoMode.Normal));
            judge.Input(PressLeft, -0.5, out var early);
            Assert.IsFalse(early.IsStray);

            // The last of the four events is at 1.5s.
            judge.AdvanceTo(2.0);
            int missedNotes = results.Count;
            judge.Input(PressLeft, 2.0, out var late);
            Assert.IsFalse(late.IsStray);
            Assert.AreEqual(missedNotes, results.Count);
            Assert.AreEqual(0, judge.StrayCount);
        }

        [Test]
        public void StrayPressesCanBeTurnedOff()
        {
            var pattern = new[] { PressLeft, ReleaseLeft, PressRight, new RhythmAction(1, InputKind.Release) };
            var judge = new RhythmJudge(ChartBuilder.Build(new[] { new ChartSection(0, 4, TempoMode.Normal) }, pattern), Clock,
                new JudgeSettings { strayPressesAreMisses = false });
            var results = new List<JudgeResult>();
            judge.Judged += results.Add;

            Assert.IsFalse(judge.Input(PressRight, 0.0, out _));
            Assert.AreEqual(0, results.Count);
            Assert.AreEqual(0, judge.StrayCount);
        }

        [Test]
        public void PassingTheWindowRecordsAMiss()
        {
            var judge = Make(out var results, new ChartSection(0, 4, TempoMode.Normal));
            judge.AdvanceTo(0.10);
            Assert.AreEqual(0, results.Count, "still inside the 110ms window");

            judge.AdvanceTo(0.12);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(Judgement.Miss, results[0].Judgement);
            Assert.AreEqual(1, judge.MissCount);
            Assert.AreEqual(1, judge.Cursor);
        }

        [Test]
        public void InputExpiresEarlierMissesFirst()
        {
            var judge = Make(out var results, new ChartSection(0, 4, TempoMode.Normal));
            // Skip the first event entirely and play the second one on time.
            Assert.IsTrue(judge.Input(ReleaseLeft, 0.5, out var r));
            Assert.AreEqual(Judgement.Perfect, r.Judgement);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(Judgement.Miss, results[0].Judgement);
            Assert.AreEqual(Judgement.Perfect, results[1].Judgement);
        }

        [Test]
        public void WindowsShrinkInDoubleTime()
        {
            // Double time at 120 BPM: events 0.25s apart, so the window is capped at 0.4 * 0.25 = 0.1s.
            var judge = Make(out var results, new ChartSection(0, 2, TempoMode.Double));
            Assert.IsFalse(judge.Input(PressLeft, -0.105, out _), "too early for a 0.1s window");
            Assert.IsTrue(judge.Input(PressLeft, -0.09, out var r));
            Assert.AreEqual(Judgement.Good, r.Judgement);
        }

        [Test]
        public void WindowsDoNotOverlapInDoubleTime()
        {
            var judge = Make(out _, new ChartSection(0, 2, TempoMode.Double));
            // Event 0 at 0.0 and event 1 at 0.25. An input for event 1 at 0.16 is past event 0's window.
            Assert.IsTrue(judge.Input(ReleaseLeft, 0.16, out var r));
            Assert.AreEqual(1, r.Event.Index);
            Assert.AreEqual(1, judge.MissCount);
        }

        [Test]
        public void FinishedJudgeIgnoresInputAndReportsNoPending()
        {
            var judge = Make(out _, new ChartSection(0, 1, TempoMode.Normal));
            judge.Input(PressLeft, 0.0, out _);
            Assert.IsTrue(judge.IsFinished);
            Assert.IsFalse(judge.Pending.HasValue);
            Assert.IsFalse(judge.Input(PressLeft, 0.5, out _));
        }

        static RhythmJudge MakeChord(out List<JudgeResult> results)
        {
            // Press both buttons together at beat 0, release both together at beat 4 (2 seconds).
            var events = new List<RhythmEvent>
            {
                new RhythmEvent(0, 0, new RhythmAction(0, InputKind.Press), 0),
                new RhythmEvent(1, 0, new RhythmAction(1, InputKind.Press), 0),
                new RhythmEvent(2, 4, new RhythmAction(0, InputKind.Release), 0),
                new RhythmEvent(3, 4, new RhythmAction(1, InputKind.Release), 0),
            };
            var judge = new RhythmJudge(events, Clock, new JudgeSettings());
            var list = new List<JudgeResult>();
            judge.Judged += list.Add;
            results = list;
            return judge;
        }

        [Test]
        public void ChordEventsCanBeHitInEitherOrder()
        {
            var judge = MakeChord(out var results);
            Assert.IsTrue(judge.Input(PressRight, 0.01, out var right), "right first");
            Assert.IsTrue(judge.Input(PressLeft, 0.02, out var left), "then left");
            Assert.AreEqual(1, right.Event.Index);
            Assert.AreEqual(0, left.Event.Index);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(2, judge.Cursor, "both presses resolved, releases pending");
        }

        [Test]
        public void ChordEventsKeepAFullWindow()
        {
            // Events sharing a beat must not shrink each other's window to zero.
            var judge = MakeChord(out _);
            Assert.IsTrue(judge.Input(PressLeft, 0.1, out var r));
            Assert.AreEqual(Judgement.Good, r.Judgement);
        }

        [Test]
        public void PressingOnlyOneKeyOfAChordMissesTheOther()
        {
            var judge = MakeChord(out var results);
            judge.Input(PressLeft, 0.0, out _);
            judge.AdvanceTo(0.2);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(Judgement.Perfect, results[0].Judgement);
            Assert.AreEqual(Judgement.Miss, results[1].Judgement);
            Assert.AreEqual(1, results[1].Event.Index);
        }

        [Test]
        public void TheSameKeyCannotHitAChordTwice()
        {
            var judge = MakeChord(out _);
            Assert.IsTrue(judge.Input(PressLeft, 0.0, out _));
            Assert.IsFalse(judge.Input(PressLeft, 0.01, out _));
        }

        [Test]
        public void CountsAddUpAcrossASection()
        {
            var judge = Make(out _, new ChartSection(0, 4, TempoMode.Normal));
            judge.Input(PressLeft, 0.0, out _);
            judge.Input(ReleaseLeft, 0.5 + 0.08, out _);
            // Skip the third event (a miss), play the fourth on time.
            judge.Input(new RhythmAction(1, InputKind.Release), 1.5, out _);
            Assert.AreEqual(2, judge.PerfectCount);
            Assert.AreEqual(1, judge.GoodCount);
            Assert.AreEqual(1, judge.MissCount);
            Assert.IsTrue(judge.IsFinished);
        }
    }
}
