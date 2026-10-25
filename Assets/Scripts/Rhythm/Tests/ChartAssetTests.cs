using NUnit.Framework;
using UnityEngine;

namespace Brewing.Rhythm.Tests
{
    public class ChartAssetTests
    {
        [Test]
        public void ChartTakesItsTimingFromTheSong()
        {
            var song = ScriptableObject.CreateInstance<Song>();
            song.bpm = 100;
            song.firstBeatOffsetSeconds = 0.25;
            var chart = ScriptableObject.CreateInstance<PatternChart>();
            chart.song = song;

            Assert.AreEqual(100.0, chart.BeatClock.Bpm, 1e-9);
            Assert.AreEqual(0.25, chart.BeatClock.FirstBeatOffset, 1e-9);
            Assert.IsNull(chart.Clip, "no clip means a metronome");

            Object.DestroyImmediate(chart);
            Object.DestroyImmediate(song);
        }

        [Test]
        public void ChartWithoutASongFallsBackTo120BpmAndSaysSo()
        {
            var chart = ScriptableObject.CreateInstance<PatternChart>();

            Assert.AreEqual(Song.DefaultBpm, chart.BeatClock.Bpm, 1e-9);
            Assert.AreEqual(0.0, chart.BeatClock.FirstBeatOffset, 1e-9);
            Assert.IsTrue(chart.Validate().Exists(p => p.Contains("No song")));

            Object.DestroyImmediate(chart);
        }

        [Test]
        public void PatternChartBuildsEventsFromItsSections()
        {
            var song = ScriptableObject.CreateInstance<Song>();
            var chart = ScriptableObject.CreateInstance<PatternChart>();
            chart.song = song;
            chart.sections = new[] { new ChartSection(0, 8, TempoMode.Half) };

            Assert.AreEqual(4, chart.BuildEvents().Count);
            Assert.IsEmpty(chart.Validate());

            Object.DestroyImmediate(chart);
            Object.DestroyImmediate(song);
        }
    }
}
