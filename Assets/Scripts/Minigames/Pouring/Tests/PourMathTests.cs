using NUnit.Framework;

namespace Brewing.Pouring.Tests
{
    public class PourMathTests
    {
        static readonly PourWindow[] Windows =
        {
            new PourWindow(0, 4, 8, BottleSize.Small),
            new PourWindow(1, 10, 18, BottleSize.Large),
        };

        [Test]
        public void LightIsRedBeforeAPourAndCountsDownToIt()
        {
            var reading = PourMath.Light(1, Windows);
            Assert.AreEqual(PourLight.Red, reading.State);
            Assert.AreEqual(3, reading.BeatsUntilChange, 1e-9);
        }

        [Test]
        public void LightIsGreenDuringAPourAndCountsDownToTheRelease()
        {
            var reading = PourMath.Light(4, Windows);
            Assert.AreEqual(PourLight.Green, reading.State, "green from the press beat");
            Assert.AreEqual(4, reading.BeatsUntilChange, 1e-9);

            Assert.AreEqual(PourLight.Red, PourMath.Light(8, Windows).State, "red from the release beat");
            Assert.AreEqual(2, PourMath.Light(8, Windows).BeatsUntilChange, 1e-9);
        }

        [Test]
        public void LightStaysRedAfterTheLastBottle()
        {
            var reading = PourMath.Light(30, Windows);
            Assert.AreEqual(PourLight.Red, reading.State);
            Assert.IsTrue(double.IsPositiveInfinity(reading.BeatsUntilChange));
        }

        [Test]
        public void CountdownShowsOnePipPerBeatLeft()
        {
            Assert.AreEqual(0, PourMath.CountdownPips(5, 4), "too far off");
            Assert.AreEqual(4, PourMath.CountdownPips(4, 4));
            Assert.AreEqual(4, PourMath.CountdownPips(3.5, 4));
            Assert.AreEqual(3, PourMath.CountdownPips(3, 4));
            Assert.AreEqual(1, PourMath.CountdownPips(0.2, 4));
            Assert.AreEqual(0, PourMath.CountdownPips(0, 4));
        }

        [Test]
        public void WarningFlashPeaksOnEachCountdownBeat()
        {
            Assert.AreEqual(1f, PourMath.CountdownFlash(3, 0.5), 1e-6f, "exactly on a beat");
            Assert.AreEqual(0.5f, PourMath.CountdownFlash(2.75, 0.5), 1e-6f, "a quarter beat later");
            Assert.AreEqual(0f, PourMath.CountdownFlash(2.4, 0.5), 1e-6f, "faded by half a beat");
        }

        [Test]
        public void BottleGlidesInWaitsAtTheSpoutThenGlidesOut()
        {
            var window = Windows[0];   // pour from beat 4 to 8
            Assert.IsFalse(PourMath.BottleX(1, window, 2, 2, 0, 10, -10, out _), "not on stage yet");

            Assert.IsTrue(PourMath.BottleX(2, window, 2, 2, 0, 10, -10, out float x));
            Assert.AreEqual(10f, x, 1e-5f, "starts gliding in from the right");

            PourMath.BottleX(3, window, 2, 2, 0, 10, -10, out x);
            Assert.AreEqual(5f, x, 1e-5f, "halfway in");

            PourMath.BottleX(4, window, 2, 2, 0, 10, -10, out x);
            Assert.AreEqual(0f, x, 1e-5f, "arrives on the pour beat");

            PourMath.BottleX(7, window, 2, 2, 0, 10, -10, out x);
            Assert.AreEqual(0f, x, 1e-5f, "waits while pouring");

            PourMath.BottleX(9, window, 2, 2, 0, 10, -10, out x);
            Assert.AreEqual(-5f, x, 1e-5f, "halfway out");

            Assert.IsFalse(PourMath.BottleX(10.5, window, 2, 2, 0, 10, -10, out _), "gone");
        }

        [Test]
        public void HoldingExactlyThePourFillsToTheLine()
        {
            Assert.AreEqual(0.25, PourMath.FillPerBeat(Windows[0]), 1e-9, "small: 4 beats");
            Assert.AreEqual(0.125, PourMath.FillPerBeat(Windows[1]), 1e-9, "large: 8 beats");
        }
    }
}
