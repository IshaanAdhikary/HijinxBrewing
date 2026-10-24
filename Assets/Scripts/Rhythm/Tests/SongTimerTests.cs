using NUnit.Framework;

namespace Brewing.Rhythm.Tests
{
    public class SongTimerTests
    {
        ManualClock clock;
        SongTimer timer;

        [SetUp]
        public void SetUp()
        {
            clock = new ManualClock();
            timer = new SongTimer(clock);
        }

        [Test]
        public void SongTimeIsNegativeDuringLeadIn()
        {
            clock.Time = 10.0;
            timer.Start(12.0);
            Assert.AreEqual(-2.0, timer.SongTime, 1e-9);

            clock.Time = 13.5;
            Assert.AreEqual(1.5, timer.SongTime, 1e-9);
        }

        [Test]
        public void PauseFreezesSongTimeAndResumeContinuesFromThere()
        {
            timer.Start(0.0);
            clock.Time = 5.0;
            timer.Pause();

            clock.Time = 9.0;
            Assert.AreEqual(5.0, timer.SongTime, 1e-9);

            timer.Resume();
            Assert.AreEqual(5.0, timer.SongTime, 1e-9);

            clock.Time = 10.0;
            Assert.AreEqual(6.0, timer.SongTime, 1e-9);
        }

        [Test]
        public void StoppedTimerReportsZero()
        {
            timer.Start(0.0);
            clock.Time = 3.0;
            timer.Stop();
            Assert.AreEqual(0.0, timer.SongTime);
        }

        [Test]
        public void BeatClockConvertsBothWays()
        {
            var beats = new BeatClock(120.0, 0.25);
            Assert.AreEqual(0.5, beats.SecondsPerBeat, 1e-9);
            Assert.AreEqual(0.25, beats.BeatToSongTime(0), 1e-9);
            Assert.AreEqual(2.25, beats.BeatToSongTime(4), 1e-9);
            Assert.AreEqual(4.0, beats.SongTimeToBeat(2.25), 1e-9);
            Assert.AreEqual(-0.5, beats.SongTimeToBeat(0.0), 1e-9);
        }
    }
}
