using System.Collections.Generic;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>
    /// Presentation for the mixing cauldron: moves the spoon round the stir path on the beat, and blends
    /// the liquid color toward the next section's tempo color as a warning.
    /// </summary>
    public class MixingCauldronView : MonoBehaviour
    {
        [SerializeField] RhythmSession session;
        [SerializeField] Transform spoon;
        [SerializeField] SpriteRenderer liquid;
        [SerializeField] float stirRadius = 1.8f;

        [Header("Tempo colors")]
        [SerializeField] Color halfTimeColor = new Color(0.25f, 0.45f, 1f);
        [SerializeField] Color normalTimeColor = new Color(0.65f, 0.25f, 0.9f);
        [SerializeField] Color doubleTimeColor = new Color(0.95f, 0.25f, 0.3f);

        [Tooltip("How many beats before a tempo change the color starts to shift.")]
        [SerializeField] float telegraphBeats = 4f;

        StirPath path;
        IReadOnlyList<RhythmEvent> pathBuiltFor;

        void OnEnable()
        {
            PlaceSpoon(-90.0);
        }

        void Update()
        {
            if (session == null || !session.IsRunning || session.Chart == null) return;

            if (!ReferenceEquals(pathBuiltFor, session.Events))
            {
                path = new StirPath(session.Events, session.Chart.sections);
                pathBuiltFor = session.Events;
            }

            double beat = session.Conductor.SongBeat;
            PlaceSpoon(path.AngleAt(beat));

            var blend = TempoBlend.At(beat, session.Chart.sections, telegraphBeats);
            liquid.color = Color.Lerp(ColorFor(blend.From), ColorFor(blend.To), blend.T);
        }

        void PlaceSpoon(double angleDegrees)
        {
            if (spoon != null) spoon.localPosition = StirPath.PointOnRing(angleDegrees, stirRadius);
        }

        Color ColorFor(TempoMode mode)
        {
            switch (mode)
            {
                case TempoMode.Half: return halfTimeColor;
                case TempoMode.Double: return doubleTimeColor;
                default: return normalTimeColor;
            }
        }
    }
}
