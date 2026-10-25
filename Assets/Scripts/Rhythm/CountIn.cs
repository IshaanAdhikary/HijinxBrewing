using System.Collections.Generic;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Metronome clicks on the beats just before the chart's first note, so the player knows exactly when to
    /// come in. The count is a pattern with one character per beat, ending the beat before the first note:
    /// "1.2.1234" is the classic "1... 2... 1 2 3 4". A 1 plays the higher accent click, other digits the
    /// normal click, and . is a silent beat. Clicks are scheduled on the audio clock, so they land on the beat.
    /// The session's lead-in must be long enough to fit the whole pattern (8 beats at 120 BPM is 4 seconds),
    /// unless the chart's first note comes after some music.
    /// </summary>
    public class CountIn : MonoBehaviour
    {
        [SerializeField] RhythmSession session;
        [SerializeField, Tooltip("One character per beat, ending the beat before the first note. 1 = accent click, other digits = click, . = silent beat.")]
        string pattern = "1.2.1234";
        [SerializeField, Range(0f, 1f)] float volume = 0.8f;

        readonly List<AudioSource> voices = new List<AudioSource>();
        AudioClip click;
        AudioClip accent;

        void Awake()
        {
            click = ProceduralAudio.Click(false);
            accent = ProceduralAudio.Click(true);
        }

        void OnEnable()
        {
            session.Started += Schedule;
        }

        void OnDisable()
        {
            session.Started -= Schedule;
            StopAll();
        }

        void Update()
        {
            // A restart or boil-over during the lead-in cancels the clicks still waiting to play.
            if (!session.IsRunning) StopAll();
        }

        void Schedule(RhythmChart chart)
        {
            StopAll();
            var events = session.Events;
            if (events.Count == 0 || string.IsNullOrEmpty(pattern)) return;

            var conductor = session.Conductor;
            var clock = conductor.BeatClock;
            double now = conductor.SongTime;
            double firstBeat = events[0].Beat;
            int beats = pattern.Length;
            int skipped = 0;

            for (int i = 0; i < beats; i++)
            {
                char c = pattern[i];
                if (!char.IsDigit(c)) continue;

                double songTime = clock.BeatToSongTime(firstBeat - beats + i);
                // A little tolerance, so a click right at the start of the lead-in still plays.
                if (songTime < now - 0.02)
                {
                    skipped++;
                    continue;
                }

                var voice = Voice(i);
                voice.clip = c == '1' ? accent : click;
                voice.volume = volume;
                voice.PlayScheduled(AudioSettings.dspTime + System.Math.Max(0.0, songTime - now));
            }

            if (skipped > 0)
                Debug.LogWarning($"CountIn: {skipped} click(s) fell before the lead-in started and were skipped. The count needs {beats * clock.SecondsPerBeat:0.##} seconds of lead-in.", this);
        }

        AudioSource Voice(int index)
        {
            while (voices.Count <= index)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                voices.Add(source);
            }
            return voices[index];
        }

        void StopAll()
        {
            foreach (var voice in voices)
                if (voice.isPlaying) voice.Stop();
        }
    }
}
