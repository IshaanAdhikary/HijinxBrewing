using System.Collections.Generic;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Crushing
{
    /// <summary>
    /// The recipe book. During each round the round's ingredient sits faintly on both pages; when the call
    /// plays a note, the ingredient on that note's page lights up, bounces and sounds. The player's own key
    /// presses light the pages and sound the same way, so the response echoes the call.
    /// </summary>
    public class CallBookView : MonoBehaviour
    {
        [SerializeField] CrushingGame game;

        [Header("Pages")]
        [SerializeField] SpriteRenderer leftIngredient;
        [SerializeField] SpriteRenderer rightIngredient;
        [SerializeField, Tooltip("Used for rounds with no ingredient of their own.")] Sprite placeholderIngredient;
        [SerializeField, Tooltip("Longest side of the ingredient on the page, in the page's own units, whatever the sprite's size.")]
        float ingredientSize = 3.5f;

        [Header("Look")]
        [SerializeField, Range(0f, 1f), Tooltip("Opacity of the ingredient between notes.")] float restingAlpha = 0.25f;
        [SerializeField, Tooltip("Beats a lit ingredient takes to fade back to resting.")] float fadeBeats = 0.75f;
        [SerializeField, Tooltip("How much a note makes the ingredient bounce. 0.3 is 30% bigger.")] float notePunch = 0.3f;
        [SerializeField, Tooltip("Bounce for the player's own presses, usually smaller than a call note.")] float pressPunch = 0.15f;

        [Header("Sound")]
        [SerializeField, Tooltip("Placeholder pitch of a left note, in Hz.")] float leftPitch = 523f;
        [SerializeField, Tooltip("Placeholder pitch of a right note, in Hz.")] float rightPitch = 784f;
        [SerializeField, Range(0f, 1f)] float volume = 0.5f;
        [SerializeField, Tooltip("Seconds ahead to schedule call notes on the audio clock, so they land exactly on the beat.")]
        double scheduleAheadSeconds = 0.1;

        readonly AudioSource[] voices = new AudioSource[4];
        readonly double[] litBeat = { double.NegativeInfinity, double.NegativeInfinity };
        AudioClip leftClip;
        AudioClip rightClip;
        RhythmPulse leftPulse;
        RhythmPulse rightPulse;
        int nextVoice;
        int flashCursor;
        int soundCursor;

        RhythmSession Session => game.Session;

        void Awake()
        {
            leftClip = ProceduralAudio.Tone(leftPitch, 0.18f);
            rightClip = ProceduralAudio.Tone(rightPitch, 0.18f);
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }

            // The pulse sits on the parent, so it can scale freely while this view sizes the sprite.
            leftPulse = leftIngredient.GetComponentInParent<RhythmPulse>();
            rightPulse = rightIngredient.GetComponentInParent<RhythmPulse>();
            SetVisible(false);
        }

        void OnEnable()
        {
            game.ChartStarted.AddListener(ResetCursors);
            Session.ButtonInput += OnButtonInput;
        }

        void OnDisable()
        {
            game.ChartStarted.RemoveListener(ResetCursors);
            Session.ButtonInput -= OnButtonInput;
        }

        void ResetCursors()
        {
            flashCursor = 0;
            soundCursor = 0;
            litBeat[0] = litBeat[1] = double.NegativeInfinity;
        }

        void Update()
        {
            if (!Session.IsRunning || game.Chart == null)
            {
                SetVisible(false);
                return;
            }

            var conductor = Session.Conductor;
            double beat = conductor.SongBeat;
            var notes = game.CallNotes;

            ScheduleSounds(notes, conductor);

            while (flashCursor < notes.Count && notes[flashCursor].Beat <= beat)
            {
                Light(notes[flashCursor].Button, notes[flashCursor].Beat, notePunch);
                flashCursor++;
            }

            ShowRound(game.Chart.RoundAt(beat), beat);
        }

        void ScheduleSounds(IReadOnlyList<CallNote> notes, Conductor conductor)
        {
            var clock = conductor.BeatClock;
            double now = conductor.SongTime;
            while (soundCursor < notes.Count)
            {
                var note = notes[soundCursor];
                double ahead = clock.BeatToSongTime(note.Beat) - now;
                if (ahead > scheduleAheadSeconds) break;
                // A note more than a moment late (after a hitch) is skipped rather than played off the beat.
                if (ahead > -0.05) Play(note.Button, AudioSettings.dspTime + System.Math.Max(0.0, ahead));
                soundCursor++;
            }
        }

        void OnButtonInput(RhythmAction action)
        {
            if (action.kind != InputKind.Press || action.button > 1) return;
            Play(action.button, AudioSettings.dspTime);
            Light(action.button, Session.Conductor.SongBeat, pressPunch);
        }

        void Light(int button, double beat, float punch)
        {
            if (button > 1) return;
            litBeat[button] = beat;
            var pulse = button == CallResponseBuilder.LeftButton ? leftPulse : rightPulse;
            if (pulse != null) pulse.Punch(punch);
        }

        void Play(int button, double dspTime)
        {
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.clip = button == CallResponseBuilder.LeftButton ? leftClip : rightClip;
            voice.volume = volume;
            voice.PlayScheduled(dspTime);
        }

        void ShowRound(int round, double beat)
        {
            if (round < 0)
            {
                SetVisible(false);
                return;
            }

            var entry = game.Chart.rounds[round];
            var sprite = entry.ingredient != null && entry.ingredient.sprite != null ? entry.ingredient.sprite : placeholderIngredient;
            Apply(leftIngredient, sprite, beat - litBeat[0]);
            Apply(rightIngredient, sprite, beat - litBeat[1]);
            SetVisible(true);
        }

        void Apply(SpriteRenderer page, Sprite sprite, double beatsSinceLit)
        {
            float lit = beatsSinceLit >= 0.0 && beatsSinceLit < fadeBeats ? 1f - (float)(beatsSinceLit / fadeBeats) : 0f;
            if (page.sprite != sprite)
            {
                page.sprite = sprite;
                if (sprite != null)
                {
                    var bounds = sprite.bounds.size;
                    page.transform.localScale = Vector3.one * (ingredientSize / Mathf.Max(0.0001f, Mathf.Max(bounds.x, bounds.y)));
                }
            }
            page.color = new Color(1f, 1f, 1f, Mathf.Lerp(restingAlpha, 1f, lit));
        }

        void SetVisible(bool visible)
        {
            leftIngredient.enabled = visible;
            rightIngredient.enabled = visible;
        }
    }
}
