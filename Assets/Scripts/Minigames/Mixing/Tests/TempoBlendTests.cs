using NUnit.Framework;
using Brewing.Rhythm;

namespace Brewing.Mixing.Tests
{
    public class TempoBlendTests
    {
        static readonly ChartSection[] ThreeSections =
        {
            new ChartSection(0, 16, TempoMode.Normal),
            new ChartSection(16, 16, TempoMode.Half),
            new ChartSection(32, 16, TempoMode.Double),
        };

        [Test]
        public void ShowsTheCurrentColorUntilTheTelegraphWindow()
        {
            var state = TempoBlend.At(5, ThreeSections, 4);
            Assert.AreEqual(TempoMode.Normal, state.From);
            Assert.AreEqual(0f, state.T);
        }

        [Test]
        public void BlendsTowardTheNextSectionOverTheTelegraphWindow()
        {
            var start = TempoBlend.At(12, ThreeSections, 4);
            Assert.AreEqual(TempoMode.Normal, start.From);
            Assert.AreEqual(TempoMode.Half, start.To);
            Assert.AreEqual(0f, start.T, 1e-6f);

            Assert.AreEqual(0.5f, TempoBlend.At(14, ThreeSections, 4).T, 1e-6f);
            Assert.AreEqual(0.75f, TempoBlend.At(15, ThreeSections, 4).T, 1e-6f);
        }

        [Test]
        public void BlendIsCompleteExactlyWhenTheNextSectionStarts()
        {
            // Just before the boundary the blend is nearly done; at the boundary the new section's color is shown.
            Assert.AreEqual(1f, TempoBlend.At(15.999, ThreeSections, 4).T, 1e-3f);

            var atBoundary = TempoBlend.At(16, ThreeSections, 4);
            Assert.AreEqual(TempoMode.Half, atBoundary.From);
            Assert.AreEqual(0f, atBoundary.T);
        }

        [Test]
        public void LastSectionNeverBlends()
        {
            var state = TempoBlend.At(47, ThreeSections, 4);
            Assert.AreEqual(TempoMode.Double, state.From);
            Assert.AreEqual(TempoMode.Double, state.To);
            Assert.AreEqual(0f, state.T);
        }

        [Test]
        public void BeforeTheSongStartsItShowsTheFirstSection()
        {
            var state = TempoBlend.At(-4, ThreeSections, 4);
            Assert.AreEqual(TempoMode.Normal, state.From);
            Assert.AreEqual(0f, state.T);
        }

        [Test]
        public void SectionsShorterThanTheWindowBlendOverTheirOwnLength()
        {
            var sections = new[]
            {
                new ChartSection(0, 8, TempoMode.Normal),
                new ChartSection(8, 2, TempoMode.Half),
                new ChartSection(10, 10, TempoMode.Double),
            };

            var start = TempoBlend.At(8, sections, 4);
            Assert.AreEqual(TempoMode.Half, start.From);
            Assert.AreEqual(TempoMode.Double, start.To);
            Assert.AreEqual(0f, start.T, 1e-6f);
            Assert.AreEqual(0.5f, TempoBlend.At(9, sections, 4).T, 1e-6f);
        }

        [Test]
        public void NoSectionsDefaultsToNormal()
        {
            var state = TempoBlend.At(3, new ChartSection[0], 4);
            Assert.AreEqual(TempoMode.Normal, state.From);
            Assert.AreEqual(0f, state.T);
        }
    }
}
