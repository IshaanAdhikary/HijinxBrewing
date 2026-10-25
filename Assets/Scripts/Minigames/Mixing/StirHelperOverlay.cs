using System.Collections.Generic;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>
    /// A helper drawn over the cauldron that shows what to press and when. A single marker rides along with
    /// the spoon and shows the next action: a filled circle for a press, a ring for a release, colored by
    /// button. A ring shrinks onto it, closing exactly as the spoon reaches the stop. Arcs stay fixed on the
    /// cauldron joining each press to its release, since that is the stretch of the path you hold a key for.
    ///
    /// Made for the four-action stir pattern (press left, release left, press right, release right). Put this
    /// object at the center of the cauldron and assign the cauldron view so it can follow the spoon. Fade it
    /// with a <see cref="HelperFader"/> on the same object.
    /// </summary>
    public class StirHelperOverlay : MonoBehaviour, IFadeable
    {
        [Header("References")]
        [SerializeField] RhythmSession session;
        [SerializeField, Tooltip("The cauldron view. Needed so the marker can follow the spoon, and for the path radius.")] MixingCauldronView view;
        [SerializeField, Tooltip("Used when no view is assigned.")] float fallbackRadius = 1.8f;
        [SerializeField] Sprite circleSprite;
        [SerializeField] Sprite ringSprite;
        [SerializeField, Tooltip("Material for the hold arcs. A Sprites/Default material works.")] Material arcMaterial;

        [Header("Look")]
        [SerializeField] float markerSize = 0.7f;
        [SerializeField, Tooltip("How big the approach ring starts, as a multiple of the marker.")] float approachStartScale = 2.4f;
        [SerializeField, Range(0f, 1f)] float approachRingAlpha = 0.9f;
        [SerializeField] float arcWidth = 0.2f;
        [SerializeField, Tooltip("The arc for the hold in progress is this much thicker.")] float activeArcWidthScale = 1.6f;
        [SerializeField, Range(0f, 1f), Tooltip("Opacity of arcs (and compass markers, if shown) that are not next.")] float dimmedAlpha = 0.55f;
        [SerializeField, Tooltip("Extra size, as a fraction, the marker pops to when the spoon reaches a stop.")] float pulseScale = 0.3f;
        [SerializeField] float pulseSeconds = 0.2f;
        [SerializeField, Tooltip("One color per button. Defaults are a green that stands out against the blue brew, and the Okabe-Ito orange.")]
        Color[] buttonColors =
        {
            new Color(0.42f, 0.78f, 0.2f),
            new Color(0.902f, 0.624f, 0f),
        };
        [SerializeField, Tooltip("The spoon should draw above this. Arcs use this, the marker uses one or two more.")] int sortingOrder = 2;

        [Header("Optional")]
        [SerializeField, Tooltip("Also show the four fixed markers at the compass points. Off by default: a single marker follows the spoon instead.")]
        bool showCompassMarkers = false;
        [SerializeField, Tooltip("Also hide the shrinking ring on the quadrants where nothing is pressed. Off by default, so the ring still counts you in to each press.")]
        bool hideApproachRingWhenNotHeld = false;

        /// <summary>Overall opacity of the whole helper, used for fading it out.</summary>
        public float Alpha { get; set; } = 1f;

        const int Slots = 4;
        const int ArcPoints = 20;

        class Marker
        {
            public Transform root;
            public SpriteRenderer head;
            public SpriteRenderer approach;
        }

        readonly Marker[] compassMarkers = new Marker[Slots];
        readonly LineRenderer[] arcs = new LineRenderer[Slots];
        Marker follower;

        GameObject container;
        PatternChart chart;
        IReadOnlyList<RhythmEvent> builtFor;
        int[] slotOf;
        RhythmAction[] pattern;
        bool[] arcStartsAt;
        bool patternOk;
        float builtRadius = -1f;

        /// <summary>Brings the helper back at full opacity, for example after it has faded out.</summary>
        public void Show()
        {
            Alpha = 1f;
            gameObject.SetActive(true);
        }

        void Awake()
        {
            if (session == null) session = FindAnyObjectByType<RhythmSession>();
            if (view == null) view = FindAnyObjectByType<MixingCauldronView>();
            if (circleSprite == null || ringSprite == null)
                Debug.LogWarning("StirHelperOverlay: a sprite is missing. Assign the circle and ring sprites.", this);
            if (view == null)
                Debug.LogWarning("StirHelperOverlay: no MixingCauldronView found, so the marker cannot follow the spoon.", this);

            container = new GameObject("Helper Visuals");
            container.transform.SetParent(transform, false);
            container.SetActive(false);

            follower = CreateMarker("Follower", sortingOrder + 2);

            for (int slot = 0; slot < Slots; slot++)
            {
                compassMarkers[slot] = CreateMarker("Compass Marker " + slot, sortingOrder + 2);

                var arcObject = new GameObject("Hold Arc " + slot);
                arcObject.transform.SetParent(container.transform, false);
                var line = arcObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.sharedMaterial = arcMaterial;
                line.sortingOrder = sortingOrder;
                line.numCapVertices = 6;
                line.positionCount = ArcPoints;
                line.enabled = false;
                arcs[slot] = line;
            }
        }

        Marker CreateMarker(string objectName, int headOrder)
        {
            var root = new GameObject(objectName).transform;
            root.SetParent(container.transform, false);
            var marker = new Marker
            {
                root = root,
                head = CreateSprite("Head", root, headOrder),
                approach = CreateSprite("Approach Ring", root, headOrder - 1),
            };
            marker.approach.sprite = ringSprite;
            return marker;
        }

        SpriteRenderer CreateSprite(string objectName, Transform parent, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        void LateUpdate()
        {
            chart = session != null ? session.Chart as PatternChart : null;
            bool running = session != null && session.IsRunning && chart != null;
            if (container.activeSelf != running) container.SetActive(running);
            if (!running) return;

            if (!ReferenceEquals(builtFor, session.Events)) Rebuild();
            if (!patternOk) return;

            float radius = view != null ? view.StirRadius : fallbackRadius;
            if (!Mathf.Approximately(radius, builtRadius)) LayoutGeometry(radius);

            var events = session.Events;
            var judge = session.Judge;
            double beatNow = session.Conductor.SongBeat;
            double secondsPerBeat = chart.BeatClock.SecondsPerBeat;

            int cursor = judge.Cursor;
            int nextSlot = cursor < events.Count ? slotOf[cursor] : -1;

            int last = LastEventAtOrBefore(events, beatNow);
            int lastSlot = last >= 0 ? slotOf[last] : -1;
            float pulse = last >= 0 ? Mathf.Clamp01(1f - (float)((beatNow - events[last].Beat) * secondsPerBeat) / pulseSeconds) : 0f;

            UpdateFollower(cursor, events, beatNow, pulse);

            for (int slot = 0; slot < Slots; slot++)
            {
                bool isNext = slot == nextSlot;
                Color color = ColorFor(pattern[slot].button);

                var marker = compassMarkers[slot];
                marker.root.gameObject.SetActive(showCompassMarkers);
                if (showCompassMarkers)
                {
                    float size = markerSize * (isNext ? 1.15f : 1f) * (1f + (slot == lastSlot ? pulseScale * pulse : 0f));
                    marker.head.transform.localScale = new Vector3(size, size, 1f);
                    marker.head.color = WithAlpha(color, (isNext ? 1f : dimmedAlpha) * Alpha);
                    ShowApproach(marker, isNext, cursor, events, beatNow, color);
                }

                var arc = arcs[slot];
                if (arc.enabled)
                {
                    bool holdInProgress = nextSlot == (slot + 1) % Slots;
                    arc.widthMultiplier = arcWidth * (holdInProgress ? activeArcWidthScale : 1f);
                    Color arcColor = WithAlpha(color, (holdInProgress ? 1f : dimmedAlpha) * Alpha);
                    arc.startColor = arcColor;
                    arc.endColor = arcColor;
                }
            }
        }

        // The single marker that rides with the spoon and shows the next action.
        void UpdateFollower(int cursor, IReadOnlyList<RhythmEvent> events, double beatNow, float pulse)
        {
            bool hasNext = cursor < events.Count && view != null && view.Spoon != null;
            follower.root.gameObject.SetActive(hasNext);
            if (!hasNext) return;

            var action = events[cursor].Action;
            Color color = ColorFor(action.button);

            // A release being next means a key is held, so the spoon is on one of the hold arcs. The dot only
            // shows there, and is hidden on the quadrants where nothing is pressed.
            bool keyHeld = action.kind == InputKind.Release;

            follower.root.localPosition = view.Spoon.localPosition;
            follower.head.enabled = keyHeld;
            follower.head.sprite = action.kind == InputKind.Press ? circleSprite : ringSprite;
            float size = markerSize * 1.15f * (1f + pulseScale * pulse);
            follower.head.transform.localScale = new Vector3(size, size, 1f);
            follower.head.color = WithAlpha(color, Alpha);
            ShowApproach(follower, keyHeld || !hideApproachRingWhenNotHeld, cursor, events, beatNow, color);
        }

        void ShowApproach(Marker marker, bool visible, int cursor, IReadOnlyList<RhythmEvent> events, double beatNow, Color color)
        {
            marker.approach.enabled = visible;
            if (!visible) return;

            double spacing = chart.sections[events[cursor].SectionIndex].mode.EventSpacingBeats();
            float fraction = StirHelperMath.ApproachFraction(events[cursor].Beat, beatNow, spacing);
            float ringSize = markerSize * Mathf.Lerp(1.15f, approachStartScale, fraction);
            marker.approach.transform.localScale = new Vector3(ringSize, ringSize, 1f);
            marker.approach.color = WithAlpha(color, approachRingAlpha * Alpha);
        }

        void Rebuild()
        {
            builtFor = session.Events;
            builtRadius = -1f;

            pattern = chart.actionPattern;
            patternOk = pattern != null && pattern.Length == Slots;
            if (!patternOk)
            {
                Debug.LogWarning($"StirHelperOverlay: needs a {Slots}-action pattern but the chart has {(pattern == null ? 0 : pattern.Length)}.", this);
                return;
            }

            slotOf = StirHelperMath.SlotOfEachEvent(builtFor, pattern.Length);
            arcStartsAt = StirHelperMath.HoldArcStartsAt(pattern);

            for (int slot = 0; slot < Slots; slot++)
            {
                compassMarkers[slot].head.sprite = pattern[slot].kind == InputKind.Press ? circleSprite : ringSprite;
                arcs[slot].enabled = arcStartsAt[slot];
            }
        }

        // Places the compass markers on the ring and bends each hold arc along the path from one stop to the next.
        void LayoutGeometry(float radius)
        {
            builtRadius = radius;
            for (int slot = 0; slot < Slots; slot++)
            {
                compassMarkers[slot].root.localPosition = StirPath.PointOnRing(AngleOf(slot), radius);

                if (!arcStartsAt[slot]) continue;
                var arc = arcs[slot];
                for (int p = 0; p < ArcPoints; p++)
                {
                    double angle = AngleOf(slot) - 90.0 * p / (ArcPoints - 1);
                    arc.SetPosition(p, StirPath.PointOnRing(angle, radius));
                }
            }
        }

        // The spoon's stops run clockwise from the bottom: -90, -180, -270, -360 degrees.
        static double AngleOf(int slot) => -90.0 - 90.0 * slot;

        Color ColorFor(int button)
        {
            if (buttonColors == null || buttonColors.Length == 0) return Color.white;
            return buttonColors[button % buttonColors.Length];
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static int LastEventAtOrBefore(IReadOnlyList<RhythmEvent> events, double beat)
        {
            int low = 0, high = events.Count;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (events[mid].Beat <= beat) low = mid + 1;
                else high = mid;
            }
            return low - 1;
        }
    }
}
