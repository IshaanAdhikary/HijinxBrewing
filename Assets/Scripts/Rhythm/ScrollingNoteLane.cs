using System.Collections.Generic;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// A helper lane along the top of the screen. Notes scroll from right to left toward a hit line on the
    /// left, so you can see what to press and when. Presses are filled circles, releases are rings, and a bar
    /// joins each press to its release. Notes dim once they have been hit or missed.
    ///
    /// The lane follows the camera's top-left corner, so it stays at the top of the screen at any aspect
    /// ratio. Keep this object at the origin with scale 1. Sizes are in world units.
    /// </summary>
    public class ScrollingNoteLane : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RhythmSession session;
        [SerializeField, Tooltip("Must be orthographic. Defaults to the main camera.")] Camera targetCamera;
        [SerializeField] Sprite circleSprite;
        [SerializeField] Sprite ringSprite;
        [SerializeField] Sprite squareSprite;

        [Header("Layout (world units)")]
        [SerializeField] float laneHeight = 1.0f;
        [SerializeField, Tooltip("Gap between the top of the screen and the lane.")] float topMargin = 0.25f;
        [SerializeField, Tooltip("Distance from the left edge of the screen to the hit line.")] float hitLineInset = 1.6f;
        [SerializeField, Tooltip("Horizontal distance between beats. Fixed, so slow sections look sparse and fast ones dense.")] float unitsPerBeat = 1.6f;
        [SerializeField] float noteSize = 0.55f;
        [SerializeField] float holdBarHeight = 0.2f;

        [Header("Look")]
        [SerializeField, Tooltip("One color per button. Defaults are from the Okabe-Ito colorblind-safe palette: blue, then orange.")]
        Color[] buttonColors =
        {
            new Color(0f, 0.447f, 0.698f),
            new Color(0.902f, 0.624f, 0f),
        };
        [SerializeField] Color laneColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] Color hitLineColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField, Range(0f, 1f), Tooltip("Opacity of notes that have already been hit or missed.")] float dimmedAlpha = 0.25f;
        [SerializeField] int sortingOrder = 20;

        /// <summary>Overall opacity of the whole lane, used for fading it out.</summary>
        public float Alpha { get; set; } = 1f;

        class NoteView
        {
            public GameObject root;
            public SpriteRenderer head;
            public SpriteRenderer bar;
        }

        readonly Dictionary<int, NoteView> active = new Dictionary<int, NoteView>();
        readonly Stack<NoteView> pool = new Stack<NoteView>();
        readonly List<int> toRelease = new List<int>();

        IReadOnlyList<RhythmEvent> builtFor;
        int[] holdEnd;
        bool[] resolved;
        double maxHoldBeats;

        SpriteRenderer background;
        SpriteRenderer hitLine;
        SpriteRenderer targetRing;

        /// <summary>Brings the lane back at full opacity, for example after it has faded out.</summary>
        public void Show()
        {
            Alpha = 1f;
            gameObject.SetActive(true);
        }

        void Awake()
        {
            if (session == null) session = FindAnyObjectByType<RhythmSession>();
            if (circleSprite == null || ringSprite == null || squareSprite == null)
                Debug.LogWarning("ScrollingNoteLane: a sprite is missing. Assign the circle, ring and square sprites.", this);

            background = CreateStatic("Lane Background", squareSprite, sortingOrder);
            hitLine = CreateStatic("Hit Line", squareSprite, sortingOrder + 3);
            targetRing = CreateStatic("Hit Target", ringSprite, sortingOrder + 3);
        }

        void OnEnable()
        {
            if (session != null) session.Judged += OnJudged;
        }

        void OnDisable()
        {
            if (session != null) session.Judged -= OnJudged;
        }

        void OnJudged(JudgeResult result)
        {
            if (resolved != null && result.Event.Index < resolved.Length) resolved[result.Event.Index] = true;
        }

        void LateUpdate()
        {
            bool running = session != null && session.IsRunning && session.Chart != null;
            SetStaticVisible(running);
            if (!running)
            {
                ReleaseAll();
                return;
            }

            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null || !cam.orthographic) return;

            if (!ReferenceEquals(builtFor, session.Events)) Rebuild();

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 center = cam.transform.position;
            float left = center.x - halfWidth;
            float right = center.x + halfWidth;
            float laneY = center.y + halfHeight - topMargin - laneHeight * 0.5f;
            float hitX = left + hitLineInset;

            LayoutStatic(left, right, laneY, hitX);

            double beatNow = session.Conductor.SongBeat;
            double firstBeat = beatNow + (left - hitX - noteSize) / unitsPerBeat - maxHoldBeats;
            double lastBeat = beatNow + (right - hitX + noteSize) / unitsPerBeat;

            var events = session.Events;
            int first = FirstEventAtOrAfter(events, firstBeat);
            int last = first - 1;
            while (last + 1 < events.Count && events[last + 1].Beat <= lastBeat) last++;

            toRelease.Clear();
            foreach (var pair in active)
                if (pair.Key < first || pair.Key > last) toRelease.Add(pair.Key);
            for (int i = 0; i < toRelease.Count; i++) Release(toRelease[i]);

            for (int i = first; i <= last; i++)
            {
                if (!active.TryGetValue(i, out var view))
                {
                    view = pool.Count > 0 ? pool.Pop() : CreateNoteView();
                    view.root.SetActive(true);
                    active[i] = view;
                }
                UpdateView(view, i, events, hitX, laneY, beatNow);
            }
        }

        void UpdateView(NoteView view, int index, IReadOnlyList<RhythmEvent> events, float hitX, float laneY, double beatNow)
        {
            var evt = events[index];
            bool isPress = evt.Action.kind == InputKind.Press;
            float x = hitX + (float)((evt.Beat - beatNow) * unitsPerBeat);

            Color color = ColorFor(evt.Action.button);

            view.head.sprite = isPress ? circleSprite : ringSprite;
            view.head.transform.position = new Vector3(x, laneY, 0f);
            view.head.transform.localScale = new Vector3(noteSize, noteSize, 1f);
            view.head.color = WithAlpha(color, (resolved[index] ? dimmedAlpha : 1f) * Alpha);

            int end = holdEnd[index];
            bool hasBar = isPress && end >= 0;
            view.bar.gameObject.SetActive(hasBar);
            if (hasBar)
            {
                float endX = hitX + (float)((events[end].Beat - beatNow) * unitsPerBeat);
                view.bar.transform.position = new Vector3((x + endX) * 0.5f, laneY, 0f);
                view.bar.transform.localScale = new Vector3(Mathf.Max(0f, endX - x), holdBarHeight, 1f);
                view.bar.color = WithAlpha(color, 0.55f * (resolved[end] ? dimmedAlpha : 1f) * Alpha);
            }
        }

        void Rebuild()
        {
            ReleaseAll();
            builtFor = session.Events;
            holdEnd = HoldPairs.Compute(builtFor);
            resolved = new bool[builtFor.Count];

            maxHoldBeats = 0.0;
            for (int i = 0; i < holdEnd.Length; i++)
                if (holdEnd[i] >= 0) maxHoldBeats = System.Math.Max(maxHoldBeats, builtFor[holdEnd[i]].Beat - builtFor[i].Beat);
        }

        void LayoutStatic(float left, float right, float laneY, float hitX)
        {
            background.transform.position = new Vector3((left + right) * 0.5f, laneY, 0f);
            background.transform.localScale = new Vector3(right - left, laneHeight, 1f);
            background.color = WithAlpha(laneColor, laneColor.a * Alpha);

            hitLine.transform.position = new Vector3(hitX, laneY, 0f);
            hitLine.transform.localScale = new Vector3(0.05f, laneHeight, 1f);
            hitLine.color = WithAlpha(hitLineColor, hitLineColor.a * Alpha);

            targetRing.transform.position = new Vector3(hitX, laneY, 0f);
            targetRing.transform.localScale = new Vector3(noteSize * 1.3f, noteSize * 1.3f, 1f);
            targetRing.color = WithAlpha(hitLineColor, hitLineColor.a * 0.8f * Alpha);
        }

        void SetStaticVisible(bool visible)
        {
            if (background == null) return;
            background.enabled = visible;
            hitLine.enabled = visible;
            targetRing.enabled = visible;
        }

        SpriteRenderer CreateStatic(string objectName, Sprite sprite, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return renderer;
        }

        NoteView CreateNoteView()
        {
            var view = new NoteView { root = new GameObject("Note") };
            view.root.transform.SetParent(transform, false);

            var barObject = new GameObject("Hold Bar");
            barObject.transform.SetParent(view.root.transform, false);
            view.bar = barObject.AddComponent<SpriteRenderer>();
            view.bar.sprite = squareSprite;
            view.bar.sortingOrder = sortingOrder + 1;

            var headObject = new GameObject("Head");
            headObject.transform.SetParent(view.root.transform, false);
            view.head = headObject.AddComponent<SpriteRenderer>();
            view.head.sortingOrder = sortingOrder + 2;
            return view;
        }

        void Release(int index)
        {
            var view = active[index];
            active.Remove(index);
            view.root.SetActive(false);
            pool.Push(view);
        }

        void ReleaseAll()
        {
            if (active.Count == 0) return;
            toRelease.Clear();
            toRelease.AddRange(active.Keys);
            for (int i = 0; i < toRelease.Count; i++) Release(toRelease[i]);
        }

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

        static int FirstEventAtOrAfter(IReadOnlyList<RhythmEvent> events, double beat)
        {
            int low = 0, high = events.Count;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (events[mid].Beat < beat) low = mid + 1;
                else high = mid;
            }
            return low;
        }
    }
}
