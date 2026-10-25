using NUnit.Framework;
using Brewing.Rhythm;

namespace Brewing.Mixing.Tests
{
    public class FailureMeterTests
    {
        static FailureSettings Settings() => new FailureSettings
        {
            startValue = 0.5f,
            perfectDelta = -0.1f,
            goodDelta = -0.05f,
            missDelta = 0.2f,
        };

        [Test]
        public void StartsAtTheConfiguredValue()
        {
            Assert.AreEqual(0.5f, new FailureMeter(Settings()).Value, 1e-6f);
        }

        [Test]
        public void HitsCalmTheBrewAndMissesHeatIt()
        {
            var meter = new FailureMeter(Settings());
            meter.Apply(Judgement.Perfect);
            Assert.AreEqual(0.4f, meter.Value, 1e-6f);
            meter.Apply(Judgement.Good);
            Assert.AreEqual(0.35f, meter.Value, 1e-6f);
            meter.Apply(Judgement.Miss);
            Assert.AreEqual(0.55f, meter.Value, 1e-6f);
        }

        [Test]
        public void ValueNeverGoesBelowZero()
        {
            var meter = new FailureMeter(Settings());
            for (int i = 0; i < 20; i++) meter.Apply(Judgement.Perfect);
            Assert.AreEqual(0f, meter.Value);
        }

        [Test]
        public void FailureIsReportedOnlyByTheCallThatCausesIt()
        {
            var meter = new FailureMeter(Settings());
            Assert.IsFalse(meter.Apply(Judgement.Miss));   // 0.7
            Assert.IsFalse(meter.Apply(Judgement.Miss));   // 0.9
            Assert.IsTrue(meter.Apply(Judgement.Miss));    // 1.0
            Assert.IsTrue(meter.HasFailed);
            Assert.AreEqual(1f, meter.Value);

            Assert.IsFalse(meter.Apply(Judgement.Miss), "already failed");
        }

        [Test]
        public void AFailedMeterIgnoresFurtherHitsUntilReset()
        {
            var meter = new FailureMeter(Settings());
            for (int i = 0; i < 3; i++) meter.Apply(Judgement.Miss);

            meter.Apply(Judgement.Perfect);
            Assert.AreEqual(1f, meter.Value);

            meter.Reset();
            Assert.IsFalse(meter.HasFailed);
            Assert.AreEqual(0.5f, meter.Value, 1e-6f);
        }

        [Test]
        public void ChangingTheSettingsTakesEffectImmediately()
        {
            var settings = Settings();
            var meter = new FailureMeter(settings);
            settings.missDelta = 0.01f;
            meter.Apply(Judgement.Miss);
            Assert.AreEqual(0.51f, meter.Value, 1e-6f);
        }
    }
}
