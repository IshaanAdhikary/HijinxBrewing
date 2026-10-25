# Rhythm engine

Namespace `Brewing.Rhythm`. Everything here is abstract: it knows about beats, buttons and timing windows, not specific minigames.

## Try it

Open `Assets/Scenes/Prototypes/RhythmTest.unity`, press Play, click **Start**. Left/A and Right/D. Hold left, release, hold right, release, on the beat. Adjust the offset slider if hits feel consistently early or late.

## The pieces

| Class | What it does |
|---|---|
| `Conductor` | The beat clock. Plays the song on the audio clock (`AudioSettings.dspTime`) and exposes `SongTime`, `SongBeat` and a `BeatPassed` event. Song time is negative during the lead-in. |
| `RhythmChart` (ScriptableObject) | A song, its BPM and first-beat offset, and a list of **sections** (start beat, length, tempo mode). Create with Assets > Create > Brewing > Rhythm Chart. No clip means a metronome plays. |
| `ChartBuilder` | Turns sections into the flat list of expected `RhythmEvent`s, and validates charts. |
| `RhythmJudge` | Matches player input to expected events. Pure C#, no Unity dependencies. |
| `RhythmInput` | Press and release events for buttons 0 (left) and 1 (right), timestamped from the Input System. |
| `RhythmSession` | Glue: `Begin(chart)` starts everything, `Judged` fires on each hit or miss. |
| `RhythmCalibration` | The player's latency offset, saved in PlayerPrefs. |
| `RhythmTestHarness` | The debug screen. Not for shipping. |

## Using it from a minigame

```csharp
[SerializeField] RhythmSession session;
[SerializeField] RhythmChart chart;

void Start()
{
    session.Judged += OnJudged;
    session.Begin(chart);          // starts after a 2 second lead-in
}

void OnJudged(JudgeResult r)
{
    // r.Judgement is Perfect, Good or Miss. r.ErrorSeconds is negative if early.
    // r.Event.Action says which press or release it was.
}

void Update()
{
    double beat = session.Conductor.SongBeat;   // drive animation from this, not from Time.time
}
```

## Tempo sections

Each section plays at one tempo mode, and the action pattern restarts at the start of each section.

| Mode | Events | One stir (4 actions) |
|---|---|---|
| Half | every 2 beats | 8 beats |
| Normal | every beat | 4 beats |
| Double | every half beat | 2 beats |

Keep section lengths a multiple of that, so tempo changes happen at the bottom of the stir with no key held. `RhythmChart.Validate()` warns in the console when a section ends mid-cycle.

## Timing notes

- Judging uses the audio clock, not frame time, and the Input System's own event timestamps. Don't read `Time.time` for anything beat-related.
- Hit windows are generous and shrink automatically in double time (never more than 40% of the gap to the next event). Tune them in the `Judge Settings` on `RhythmSession`.
- Wrong buttons and inputs that are too early are ignored. Events only become misses once their window has passed.
- Rebinding or gamepad: assign an `InputActionAsset` on `RhythmInput`, with actions named `Left` and `Right` in a map called `Rhythm`.

## Tests

Window > General > Test Runner > EditMode. The tests run on a fake clock, so they are fast and exact.

## Not done yet

- A proper calibration screen (the harness slider is the only UI for the offset).
- Pausing when the window loses focus. `Conductor.Pause()` and `Resume()` exist but nothing calls them automatically.
- Checking audio latency in a WebGL build.
