using System;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>Generates simple placeholder clips in code, so no audio files are needed.</summary>
    public static class ProceduralAudio
    {
        /// <summary>
        /// A loopable metronome: a click on every beat, higher on the first beat of each loop.
        /// The click lands on sample 0, so the first beat offset is 0.
        /// </summary>
        public static AudioClip Metronome(double bpm, int beatsPerLoop = 4)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            int samplesPerBeat = (int)Math.Round(60.0 / bpm * sampleRate);
            var data = new float[samplesPerBeat * beatsPerLoop];

            for (int beat = 0; beat < beatsPerLoop; beat++)
            {
                float freq = beat == 0 ? 1600f : 1000f;
                int clickSamples = Math.Min((int)(sampleRate * 0.04f), samplesPerBeat);
                for (int i = 0; i < clickSamples; i++)
                {
                    float envelope = 1f - (float)i / clickSamples;
                    data[beat * samplesPerBeat + i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * envelope * 0.6f;
                }
            }

            var clip = AudioClip.Create("Metronome", data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>One metronome click: 1600 Hz for an accent, 1000 Hz otherwise, as in <see cref="Metronome"/>.</summary>
        public static AudioClip Click(bool accent) => Tone(accent ? 1600f : 1000f, 0.04f, 0.6f);

        /// <summary>A short decaying tone, useful as a placeholder hit sound.</summary>
        public static AudioClip Tone(float frequency, float seconds, float volume = 0.4f)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            var data = new float[(int)(sampleRate * seconds)];
            for (int i = 0; i < data.Length; i++)
            {
                float envelope = 1f - (float)i / data.Length;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
            }

            var clip = AudioClip.Create("Tone", data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
