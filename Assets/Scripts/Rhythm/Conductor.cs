using System;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// The beat clock. Plays the song on the audio system clock and reports song time and beat position.
    /// Song time is zero when the clip starts, and negative during the lead-in.
    /// </summary>
    public class Conductor : MonoBehaviour
    {
        [SerializeField] AudioSource musicSource;

        SongTimer timer;
        double scheduledStart;
        bool startedAtPause;
        int lastBeat = -1;

        public BeatClock BeatClock { get; private set; }
        public bool IsRunning => timer != null && timer.IsRunning;
        public bool IsPaused => timer != null && timer.IsPaused;
        public double SongTime => timer?.SongTime ?? 0.0;
        public double SongBeat => BeatClock.SongTimeToBeat(SongTime);

        /// <summary>Raised once for each beat index from 0 upwards as it passes.</summary>
        public event Action<int> BeatPassed;

        void Awake()
        {
            EnsureReady();
        }

        void EnsureReady()
        {
            if (timer != null) return;
            timer = new SongTimer(new DspClock());
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
                if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            }
            musicSource.playOnAwake = false;
        }

        /// <summary>
        /// Starts the song after a lead-in. A null clip plays a looping metronome at the given BPM.
        /// </summary>
        public void Play(AudioClip clip, double bpm, double firstBeatOffset, double leadInSeconds = 1.0)
        {
            EnsureReady();
            Stop();

            if (FindAnyObjectByType<AudioListener>() == null)
                Debug.LogWarning("Conductor: no AudioListener in the scene, so the music will be silent. Add one to the camera.");

            bool metronome = clip == null;
            if (metronome)
            {
                clip = ProceduralAudio.Metronome(bpm);
                firstBeatOffset = 0.0;
            }

            BeatClock = new BeatClock(bpm, firstBeatOffset);
            musicSource.clip = clip;
            musicSource.loop = metronome;

            scheduledStart = AudioSettings.dspTime + leadInSeconds;
            musicSource.PlayScheduled(scheduledStart);
            timer.Start(scheduledStart);
            lastBeat = -1;
        }

        public void Stop()
        {
            if (timer == null) return;
            musicSource.Stop();
            timer.Stop();
        }

        public void Pause()
        {
            if (!IsRunning || IsPaused) return;
            startedAtPause = AudioSettings.dspTime >= scheduledStart;
            timer.Pause();
            if (startedAtPause) musicSource.Pause();
        }

        public void Resume()
        {
            if (!IsRunning || !IsPaused) return;
            timer.Resume();
            if (startedAtPause)
            {
                musicSource.UnPause();
            }
            else
            {
                // Paused during the lead-in: push the scheduled start back by the time spent paused.
                scheduledStart = timer.StartTime;
                musicSource.SetScheduledStartTime(scheduledStart);
            }
        }

        public double SongTimeAtRealtime(double realtimeSinceStartup)
        {
            return timer?.SongTimeAtRealtime(realtimeSinceStartup) ?? 0.0;
        }

        void Update()
        {
            if (!IsRunning || IsPaused) return;

            int beat = (int)Math.Floor(SongBeat);
            while (lastBeat < beat)
            {
                lastBeat++;
                BeatPassed?.Invoke(lastBeat);
            }
        }
    }
}
