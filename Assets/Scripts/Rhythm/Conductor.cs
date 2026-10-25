using System;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// The beat clock. Plays the song on the audio system clock and reports song time and beat position.
    /// Song time is zero when the clip starts, and negative during the lead-in.
    ///
    /// A song can have an intense version. Both versions are scheduled to start on the same audio sample and
    /// play together, with one muted, so switching <see cref="Intense"/> is an instant cut with no drift.
    /// </summary>
    public class Conductor : MonoBehaviour
    {
        [SerializeField] AudioSource musicSource;
        [SerializeField, Tooltip("Plays the song's intense version. Created automatically if empty.")] AudioSource intenseSource;

        SongTimer timer;
        bool intense;
        double scheduledStart;
        bool startedAtPause;
        int lastBeat = -1;

        public BeatClock BeatClock { get; private set; }
        public bool IsRunning => timer != null && timer.IsRunning;
        public bool IsPaused => timer != null && timer.IsPaused;
        public double SongTime => timer?.SongTime ?? 0.0;
        public double SongBeat => BeatClock.SongTimeToBeat(SongTime);

        /// <summary>True when the playing song has an intense version to switch to.</summary>
        public bool HasIntenseVersion => intenseSource != null && intenseSource.clip != null;

        /// <summary>
        /// Which version of the song is heard. Takes effect immediately and stays set across songs, so set it
        /// before the lead-in ends to choose the version the song starts on.
        /// </summary>
        public bool Intense
        {
            get => intense;
            set
            {
                intense = value;
                ApplyIntensity();
            }
        }

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
            if (intenseSource == null || intenseSource == musicSource) intenseSource = gameObject.AddComponent<AudioSource>();
            intenseSource.playOnAwake = false;
        }

        /// <summary>
        /// Starts the song after a lead-in. A null clip plays a looping metronome at the given BPM. An intense
        /// clip, if given, plays in sync and is heard instead of the main clip while <see cref="Intense"/> is on.
        /// </summary>
        public void Play(AudioClip clip, double bpm, double firstBeatOffset, double leadInSeconds = 1.0, AudioClip intenseClip = null)
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
                intenseClip = null;
            }

            BeatClock = new BeatClock(bpm, firstBeatOffset);
            musicSource.clip = clip;
            musicSource.loop = metronome;
            intenseSource.clip = intenseClip;
            intenseSource.loop = false;
            ApplyIntensity();

            scheduledStart = AudioSettings.dspTime + leadInSeconds;
            musicSource.PlayScheduled(scheduledStart);
            if (intenseClip != null) intenseSource.PlayScheduled(scheduledStart);
            timer.Start(scheduledStart);
            lastBeat = -1;
        }

        public void Stop()
        {
            if (timer == null) return;
            musicSource.Stop();
            intenseSource.Stop();
            timer.Stop();
        }

        public void Pause()
        {
            if (!IsRunning || IsPaused) return;
            startedAtPause = AudioSettings.dspTime >= scheduledStart;
            timer.Pause();
            if (startedAtPause)
            {
                musicSource.Pause();
                intenseSource.Pause();
            }
        }

        public void Resume()
        {
            if (!IsRunning || !IsPaused) return;
            timer.Resume();
            if (startedAtPause)
            {
                musicSource.UnPause();
                intenseSource.UnPause();
            }
            else
            {
                // Paused during the lead-in: push the scheduled start back by the time spent paused.
                scheduledStart = timer.StartTime;
                musicSource.SetScheduledStartTime(scheduledStart);
                if (HasIntenseVersion) intenseSource.SetScheduledStartTime(scheduledStart);
            }
        }

        void ApplyIntensity()
        {
            if (musicSource == null || intenseSource == null) return;
            bool useIntense = intense && HasIntenseVersion;
            musicSource.mute = useIntense;
            intenseSource.mute = !useIntense;
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
