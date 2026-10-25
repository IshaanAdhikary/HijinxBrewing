using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Debug screen for the rhythm engine: a metronome, a scrolling lane of upcoming events, live hit
    /// feedback and a latency offset slider. Drop it on an empty GameObject and press Play.
    /// </summary>
    public class RhythmTestHarness : MonoBehaviour
    {
        [Tooltip("Leave empty to use a built-in test chart (purple, blue, red, purple at 120 BPM).")]
        [SerializeField] PatternChart chart;
        [SerializeField] bool autoStart;

        RhythmSession session;
        AudioSource sfxSource;
        AudioClip hitTone;
        PatternChart fallbackChart;

        float lastBeatRealtime = -10f;
        string lastResult = "";
        Color lastResultColor = Color.white;
        float lastResultRealtime = -10f;

        void Awake()
        {
            session = GetComponent<RhythmSession>();
            if (session == null) session = gameObject.AddComponent<RhythmSession>();

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            hitTone = ProceduralAudio.Tone(660f, 0.08f);
        }

        void OnEnable()
        {
            session.Conductor.BeatPassed += OnBeat;
            session.Judged += OnJudged;
            session.ButtonInput += OnButton;
        }

        void OnDisable()
        {
            session.Conductor.BeatPassed -= OnBeat;
            session.Judged -= OnJudged;
            session.ButtonInput -= OnButton;
        }

        void Start()
        {
            if (autoStart) StartChart();
        }

        public void StartChart()
        {
            var active = chart != null ? chart : GetFallbackChart();
            session.Begin(active, 2.0);
        }

        PatternChart GetFallbackChart()
        {
            if (fallbackChart != null) return fallbackChart;
            fallbackChart = ScriptableObject.CreateInstance<PatternChart>();
            fallbackChart.name = "Fallback Test Chart";
            fallbackChart.song = ScriptableObject.CreateInstance<Song>();
            fallbackChart.song.name = "Fallback Metronome";
            fallbackChart.sections = new[]
            {
                new ChartSection(0, 16, TempoMode.Normal),
                new ChartSection(16, 16, TempoMode.Half),
                new ChartSection(32, 16, TempoMode.Double),
                new ChartSection(48, 16, TempoMode.Normal),
            };
            return fallbackChart;
        }

        void OnBeat(int beat) => lastBeatRealtime = Time.realtimeSinceStartup;

        void OnButton(RhythmAction action)
        {
            sfxSource.pitch = action.kind == InputKind.Press ? 1f : 0.75f;
            sfxSource.PlayOneShot(hitTone);
        }

        void OnJudged(JudgeResult result)
        {
            lastResultRealtime = Time.realtimeSinceStartup;
            switch (result.Judgement)
            {
                case Judgement.Perfect:
                    lastResult = $"PERFECT  {result.ErrorSeconds * 1000:+0;-0} ms";
                    lastResultColor = new Color(0.4f, 1f, 0.5f);
                    break;
                case Judgement.Good:
                    lastResult = $"GOOD  {result.ErrorSeconds * 1000:+0;-0} ms";
                    lastResultColor = new Color(1f, 0.9f, 0.3f);
                    break;
                default:
                    lastResult = "MISS";
                    lastResultColor = new Color(1f, 0.35f, 0.35f);
                    break;
            }
        }

        void OnGUI()
        {
            DrawPanel();
            if (session.IsRunning) DrawLane();
        }

        void DrawPanel()
        {
            GUILayout.BeginArea(new Rect(10, 10, 380, Screen.height - 20));

            GUILayout.Label("Rhythm engine test");
            if (!session.IsRunning)
            {
                if (GUILayout.Button("Start")) StartChart();
            }
            else
            {
                if (GUILayout.Button("Stop")) session.End();
            }

            double offsetMs = RhythmCalibration.OffsetMilliseconds;
            GUILayout.Label($"Input offset: {offsetMs:+0;-0;0} ms (positive if you hit late)");
            double newOffset = GUILayout.HorizontalSlider((float)offsetMs, -250f, 250f);
            if (System.Math.Abs(newOffset - offsetMs) > 0.01) RhythmCalibration.OffsetMilliseconds = newOffset;

            if (session.IsRunning)
            {
                var conductor = session.Conductor;
                var judge = session.Judge;
                GUILayout.Space(8);
                GUILayout.Label($"Song time {conductor.SongTime:F2}s   beat {conductor.SongBeat:F2}");

                var pending = judge.Pending;
                if (pending.HasValue)
                {
                    string mode = session.Chart is PatternChart patternChart
                        ? patternChart.sections[pending.Value.Group].mode.ToString()
                        : "-";
                    GUILayout.Label($"Mode: {mode}    Next: {pending.Value.Action}");
                }
                else
                {
                    GUILayout.Label("Chart finished");
                }

                GUILayout.Label($"Perfect {judge.PerfectCount}   Good {judge.GoodCount}   Miss {judge.MissCount}");

                float age = Time.realtimeSinceStartup - lastResultRealtime;
                var previous = GUI.color;
                GUI.color = new Color(lastResultColor.r, lastResultColor.g, lastResultColor.b, Mathf.Clamp01(1.5f - age));
                GUILayout.Label($"<size=22><b>{lastResult}</b></size>", new GUIStyle(GUI.skin.label) { richText = true });
                GUI.color = previous;

                GUILayout.Label($"Held:  Left {(session.InputSource.IsHeld(0) ? "[X]" : "[ ]")}   Right {(session.InputSource.IsHeld(1) ? "[X]" : "[ ]")}");
            }
            else
            {
                GUILayout.Label("Controls: Left/A and Right/D.");
                GUILayout.Label("Hold left, release, hold right, release. Repeat on the beat.");
            }

            GUILayout.EndArea();
        }

        void DrawLane()
        {
            const float pixelsPerSecond = 320f;
            float laneY = Screen.height * 0.65f;
            float hitX = Screen.width * 0.3f;
            var conductor = session.Conductor;
            var judge = session.Judge;
            var beatClock = session.Chart.BeatClock;
            double now = session.JudgeTime;

            // Lane background and the hit line, which flashes on each beat.
            Fill(new Rect(0, laneY - 34, Screen.width, 68), new Color(0f, 0f, 0f, 0.35f));
            float flash = Mathf.Clamp01(1f - (Time.realtimeSinceStartup - lastBeatRealtime) * 6f);
            Fill(new Rect(hitX - 2 - flash * 3f, laneY - 44, 4 + flash * 6f, 88), Color.Lerp(Color.white, new Color(1f, 0.8f, 0.2f), flash));

            // Beat ticks.
            double firstBeat = System.Math.Floor(conductor.SongBeat) - 2;
            for (double beat = firstBeat; beat < firstBeat + 14; beat++)
            {
                float x = hitX + (float)((beatClock.BeatToSongTime(beat) - conductor.SongTime) * pixelsPerSecond);
                Fill(new Rect(x - 1, laneY - 38, 2, 8), new Color(1f, 1f, 1f, 0.5f));
            }

            // Upcoming and recent events. Presses are solid squares, releases are hollow.
            for (int i = System.Math.Max(0, judge.Cursor - 4); i < session.Events.Count; i++)
            {
                var evt = session.Events[i];
                float x = hitX + (float)((judge.GetEventTime(i) - now) * pixelsPerSecond);
                if (x > Screen.width + 20) break;

                bool resolved = i < judge.Cursor;
                Color baseColor = evt.Action.button == 0 ? new Color(0.35f, 0.65f, 1f) : new Color(1f, 0.6f, 0.25f);
                if (resolved) baseColor.a = 0.3f;

                const float size = 26f;
                var rect = new Rect(x - size / 2, laneY - size / 2, size, size);
                Fill(rect, baseColor);
                if (evt.Action.kind == InputKind.Release)
                    Fill(new Rect(rect.x + 6, rect.y + 6, size - 12, size - 12), new Color(0.1f, 0.08f, 0.15f, 1f));
            }
        }

        static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
