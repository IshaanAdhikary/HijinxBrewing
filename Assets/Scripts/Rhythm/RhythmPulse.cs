using System;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Makes an object bounce in time with the music: on every pulse it swells a little and eases back.
    /// Pulses follow the session's beat (every N beats, with an offset), and <see cref="Punch"/> adds a pulse
    /// at any moment, such as when a note plays. Works on anything with a transform, in any minigame.
    /// </summary>
    public class RhythmPulse : MonoBehaviour
    {
        [SerializeField] RhythmSession session;

        [Header("Beat")]
        [SerializeField, Tooltip("Pulse every this many beats: 1 is every beat, 4 is once a bar. 0 turns beat pulses off, leaving only Punch.")]
        float everyBeats = 1f;
        [SerializeField, Tooltip("Shifts the beat pulses, in beats. 0.5 pulses on the off-beats.")] float offsetBeats;
        [SerializeField, Tooltip("Extra size at the peak of a beat pulse. 0.05 is 5% bigger.")] float strength = 0.05f;

        [Header("Shape")]
        [SerializeField, Tooltip("How long a pulse takes to settle, in beats.")] float decayBeats = 0.4f;
        [SerializeField, Tooltip("Which axes swell. (1, 1) grows evenly; (0.5, 1) stretches upward more than sideways.")]
        Vector2 axes = Vector2.one;

        Vector3 baseScale;
        double punchBeat = double.NegativeInfinity;
        float punchStrength;

        public RhythmSession Session
        {
            get => session;
            set => session = value;
        }

        void Awake()
        {
            baseScale = transform.localScale;
        }

        void OnDisable()
        {
            transform.localScale = baseScale;
        }

        /// <summary>Adds a pulse now, on top of the beat pulses. Strength 0 or less uses the beat strength.</summary>
        public void Punch(float punch = 0f)
        {
            if (session == null || !session.IsRunning) return;
            punchBeat = session.Conductor.SongBeat;
            punchStrength = punch > 0f ? punch : strength;
        }

        void LateUpdate()
        {
            float amount = 0f;
            if (session != null && session.IsRunning)
            {
                double beat = session.Conductor.SongBeat;
                if (everyBeats > 0f)
                {
                    double since = Mod(beat - offsetBeats, everyBeats);
                    amount = strength * Envelope(since);
                }
                amount = Mathf.Max(amount, punchStrength * Envelope(beat - punchBeat));
            }

            transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + amount * axes.x, 1f + amount * axes.y, 1f));
        }

        /// <summary>1 at the moment of a pulse, easing to 0 after <see cref="decayBeats"/>.</summary>
        float Envelope(double beatsSince)
        {
            if (beatsSince < 0.0 || decayBeats <= 0f || beatsSince >= decayBeats) return 0f;
            float t = 1f - (float)(beatsSince / decayBeats);
            return t * t;
        }

        static double Mod(double value, double divisor) => value - divisor * Math.Floor(value / divisor);
    }
}
