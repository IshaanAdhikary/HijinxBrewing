using System;
using System.Collections.Generic;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Runs one rhythm chart: starts the conductor, builds the events, and feeds player input to the judge.
    /// Minigames subscribe to <see cref="Judged"/> and read the conductor for visuals.
    /// </summary>
    public class RhythmSession : MonoBehaviour
    {
        [SerializeField] Conductor conductor;
        [SerializeField] RhythmInput input;
        [SerializeField] JudgeSettings judgeSettings = new JudgeSettings();

        public Conductor Conductor { get { EnsureReady(); return conductor; } }
        public RhythmInput InputSource { get { EnsureReady(); return input; } }
        public RhythmChart Chart { get; private set; }
        public RhythmJudge Judge { get; private set; }
        public IReadOnlyList<RhythmEvent> Events { get; private set; } = Array.Empty<RhythmEvent>();
        public bool IsRunning { get; private set; }

        /// <summary>Raised when an expected event is hit or missed.</summary>
        public event Action<JudgeResult> Judged;

        /// <summary>Raised for every press or release, hit or not. Use it for immediate feedback sounds.</summary>
        public event Action<RhythmAction> ButtonInput;

        /// <summary>Raised once when the song reaches a cue placed in the chart.</summary>
        public event Action<ChartCue> CueTriggered;

        CueScheduler cueScheduler;
        readonly List<ChartCue> dueCues = new List<ChartCue>();

        /// <summary>Song time as the judge sees it: the conductor's time with the player's calibration removed.</summary>
        public double JudgeTime => conductor.SongTime - RhythmCalibration.OffsetSeconds;

        void Awake()
        {
            EnsureReady();
        }

        void EnsureReady()
        {
            if (conductor == null) conductor = GetComponent<Conductor>();
            if (conductor == null) conductor = gameObject.AddComponent<Conductor>();
            if (input == null) input = GetComponent<RhythmInput>();
            if (input == null) input = gameObject.AddComponent<RhythmInput>();
        }

        void OnEnable()
        {
            input.ButtonInput += OnButtonInput;
        }

        void OnDisable()
        {
            input.ButtonInput -= OnButtonInput;
        }

        public void Begin(RhythmChart chart, double leadInSeconds = 2.0)
        {
            End();

            Chart = chart;
            var problems = chart.Validate();
            foreach (var problem in problems) Debug.LogWarning($"RhythmChart '{chart.name}': {problem}");

            var events = chart.BuildEvents();
            Events = events;
            Judge = new RhythmJudge(events, chart.BeatClock, judgeSettings);
            Judge.Judged += OnJudged;
            cueScheduler = new CueScheduler(chart.cues);

            conductor.Play(chart.clip, chart.bpm, chart.firstBeatOffsetSeconds, leadInSeconds);
            IsRunning = true;
        }

        public void End()
        {
            if (Judge != null) Judge.Judged -= OnJudged;
            conductor.Stop();
            IsRunning = false;
        }

        void Update()
        {
            if (!IsRunning) return;

            Judge.AdvanceTo(JudgeTime);

            cueScheduler.Advance(conductor.SongBeat, dueCues);
            for (int i = 0; i < dueCues.Count; i++) FireCue(dueCues[i]);
            dueCues.Clear();
        }

        void FireCue(ChartCue cue)
        {
            if (CueTriggered == null)
                Debug.LogWarning($"RhythmSession: cue '{cue.name}' at beat {cue.beat} fired but nothing is listening. Add a ChartCueListener.");
            CueTriggered?.Invoke(cue);
        }

        void OnButtonInput(RhythmAction action, double realtimeTimestamp)
        {
            if (!IsRunning) return;
            ButtonInput?.Invoke(action);

            double songTime = conductor.SongTimeAtRealtime(realtimeTimestamp) - RhythmCalibration.OffsetSeconds;
            Judge.Input(action, songTime, out _);
        }

        void OnJudged(JudgeResult result) => Judged?.Invoke(result);
    }
}
