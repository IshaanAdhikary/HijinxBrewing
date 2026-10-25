using System;
using System.Collections.Generic;
using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Pouring
{
    /// <summary>
    /// The pouring stage: bottles glide along the bottom and stop under the spout for their pour, the cauldron
    /// tips while both keys are held, and a stream fills whichever bottle is under it. The fill follows how
    /// long you actually pour, so releasing on the beat stops exactly on the line, early leaves it short,
    /// and late overflows.
    ///
    /// The cat, cauldron, spout and stream are ordinary scene objects, so their sprites can be swapped for
    /// art directly. Bottles are built from the sprites below.
    /// </summary>
    public class PourStationView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PouringGame game;
        [SerializeField, Tooltip("Rotates to tip the cauldron while pouring.")] Transform cauldron;
        [SerializeField, Tooltip("Where the stream starts. Usually a child of the cauldron at its lip.")] Transform spout;
        [SerializeField, Tooltip("A sprite stretched from the spout down to the bottle or floor.")] SpriteRenderer stream;
        [SerializeField] Sprite squareSprite;
        [SerializeField] Sprite circleSprite;

        [Header("Stage (world units)")]
        [SerializeField, Tooltip("Where bottles stop to be filled, and where the stream falls.")] float spoutX = 0f;
        [SerializeField, Tooltip("Height of the floor bottles stand on.")] float floorY = -3.4f;
        [SerializeField, Tooltip("Where bottles come from (off screen right).")] float enterX = 10.5f;
        [SerializeField, Tooltip("Where bottles leave to (off screen left).")] float exitX = -10.5f;
        [SerializeField, Tooltip("Beats a bottle takes to glide in. It arrives exactly on its pour beat.")] float approachBeats = 2f;
        [SerializeField, Tooltip("Beats a bottle takes to glide away after its pour.")] float exitBeats = 2f;

        [Header("Bottles")]
        [SerializeField, Tooltip("Width and height of a small bottle's body.")] Vector2 smallBottleSize = new Vector2(0.9f, 1.5f);
        [SerializeField, Tooltip("Width and height of a large bottle's body.")] Vector2 largeBottleSize = new Vector2(1.25f, 2.6f);
        [SerializeField, Range(0.3f, 0.95f), Tooltip("Height of the fill line as a fraction of the body. A perfect pour stops here.")] float fillLineFraction = 0.75f;
        [SerializeField] Color glassColor = new Color(0.8f, 0.9f, 0.95f, 0.45f);
        [SerializeField] Color liquidColor = new Color(0.65f, 0.25f, 0.9f);
        [SerializeField] Color fillLineColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] int sortingOrder = 10;

        [Header("Pouring")]
        [SerializeField] float pourTiltDegrees = 45f;
        [SerializeField, Tooltip("How quickly the cauldron tips and rights itself.")] float tiltSpeed = 10f;
        [SerializeField] float streamWidth = 0.14f;

        class BottleView
        {
            public GameObject root;
            public SpriteRenderer body;
            public SpriteRenderer neck;
            public SpriteRenderer liquid;
            public SpriteRenderer line;
            public SpriteRenderer spill;
        }

        const float BottomPadding = 0.04f;

        readonly Dictionary<int, BottleView> active = new Dictionary<int, BottleView>();
        readonly Stack<BottleView> pool = new Stack<BottleView>();
        readonly List<int> toRelease = new List<int>();

        IReadOnlyList<PourWindow> builtFor;
        double[] fills;
        double lastBeat;
        float tilt;
        SpriteRenderer floorSplash;
        float floorSplashAmount;

        /// <summary>How full a bottle is: 1 is exactly on the line.</summary>
        public double FillOf(int bottle) => fills != null && bottle < fills.Length ? fills[bottle] : 0.0;

        void Awake()
        {
            if (game == null) game = FindAnyObjectByType<PouringGame>();

            var splash = new GameObject("Floor Splash");
            splash.transform.SetParent(transform, false);
            floorSplash = splash.AddComponent<SpriteRenderer>();
            floorSplash.sprite = circleSprite;
            floorSplash.sortingOrder = sortingOrder - 1;
            floorSplash.color = liquidColor;
        }

        void LateUpdate()
        {
            if (game == null) return;
            var session = game.Session;
            bool running = session != null && session.IsRunning;

            if (!ReferenceEquals(builtFor, game.Windows)) Rebuild();

            double beat = running ? session.Conductor.SongBeat : lastBeat;
            double deltaBeats = running && !double.IsNegativeInfinity(lastBeat) ? Math.Max(0.0, beat - lastBeat) : 0.0;
            lastBeat = beat;

            bool pouring = game.IsPouring;
            int underSpout = PlaceBottles(beat);

            if (pouring && underSpout >= 0)
                fills[underSpout] += deltaBeats * PourMath.FillPerBeat(builtFor[underSpout]);

            foreach (var pair in active) UpdateFill(pair.Value, builtFor[pair.Key], fills[pair.Key]);

            UpdateCauldronAndStream(pouring, underSpout);
        }

        void Rebuild()
        {
            builtFor = game.Windows;
            fills = new double[builtFor.Count];
            lastBeat = double.NegativeInfinity;
            floorSplashAmount = 0f;
            ReleaseAll();
        }

        // Positions every bottle on stage and returns the one standing under the spout, or -1.
        int PlaceBottles(double beat)
        {
            int underSpout = -1;
            toRelease.Clear();
            foreach (var key in active.Keys) toRelease.Add(key);

            for (int i = 0; i < builtFor.Count; i++)
            {
                var window = builtFor[i];
                if (!PourMath.BottleX(beat, window, approachBeats, exitBeats, spoutX, enterX, exitX, out float x)) continue;

                toRelease.Remove(i);
                if (!active.TryGetValue(i, out var view))
                {
                    view = pool.Count > 0 ? pool.Pop() : CreateBottle();
                    view.root.SetActive(true);
                    active[i] = view;
                }
                view.root.transform.position = new Vector3(x, floorY, 0f);

                float width = SizeOf(window).x;
                if (Mathf.Abs(x - spoutX) < width * 0.45f) underSpout = i;
            }

            for (int i = 0; i < toRelease.Count; i++) Release(toRelease[i]);
            return underSpout;
        }

        void UpdateFill(BottleView view, PourWindow window, double fill)
        {
            Vector2 size = SizeOf(window);
            float lineHeight = size.y * fillLineFraction;
            float capacity = size.y - BottomPadding * 2f;
            float wanted = (float)fill * lineHeight;
            float liquidHeight = Mathf.Min(wanted, capacity);
            float overflow = Mathf.Max(0f, wanted - capacity);

            view.body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            view.body.transform.localScale = new Vector3(size.x, size.y, 1f);

            view.neck.transform.localPosition = new Vector3(0f, size.y + 0.15f, 0f);
            view.neck.transform.localScale = new Vector3(size.x * 0.4f, 0.3f, 1f);

            view.liquid.enabled = liquidHeight > 0.001f;
            view.liquid.transform.localPosition = new Vector3(0f, BottomPadding + liquidHeight * 0.5f, 0f);
            view.liquid.transform.localScale = new Vector3(size.x * 0.82f, liquidHeight, 1f);

            view.line.transform.localPosition = new Vector3(0f, BottomPadding + lineHeight, 0f);
            view.line.transform.localScale = new Vector3(size.x * 1.1f, 0.05f, 1f);

            // Overflow pools around the base of the bottle.
            view.spill.enabled = overflow > 0.001f;
            float spillWidth = size.x + Mathf.Min(overflow * 3f, 2.5f);
            view.spill.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            view.spill.transform.localScale = new Vector3(spillWidth, 0.18f, 1f);
        }

        void UpdateCauldronAndStream(bool pouring, int underSpout)
        {
            float target = pouring ? pourTiltDegrees : 0f;
            tilt = Mathf.Lerp(tilt, target, 1f - Mathf.Exp(-tiltSpeed * Time.deltaTime));
            if (cauldron != null) cauldron.localRotation = Quaternion.Euler(0f, 0f, tilt);

            // Pouring with no bottle underneath splashes on the floor.
            floorSplashAmount = pouring && underSpout < 0
                ? Mathf.Min(floorSplashAmount + Time.deltaTime * 1.5f, 2.5f)
                : Mathf.Max(floorSplashAmount - Time.deltaTime * 0.5f, 0f);
            floorSplash.enabled = floorSplashAmount > 0.01f;
            floorSplash.transform.position = new Vector3(spoutX, floorY, 0f);
            floorSplash.transform.localScale = new Vector3(0.3f + floorSplashAmount, 0.18f, 1f);

            if (stream == null || spout == null) return;
            stream.enabled = pouring;
            if (!pouring) return;

            float top = spout.position.y;
            float bottom = floorY;
            if (underSpout >= 0) bottom = floorY + SizeOf(builtFor[underSpout]).y * 0.9f;
            float length = Mathf.Max(0.01f, top - bottom);
            stream.transform.position = new Vector3(spoutX, bottom + length * 0.5f, 0f);
            stream.transform.rotation = Quaternion.identity;
            stream.transform.localScale = new Vector3(streamWidth, length, 1f);
        }

        Vector2 SizeOf(PourWindow window) => window.Size == BottleSize.Small ? smallBottleSize : largeBottleSize;

        BottleView CreateBottle()
        {
            var root = new GameObject("Bottle");
            root.transform.SetParent(transform, false);
            return new BottleView
            {
                root = root,
                spill = CreatePart("Spill", root.transform, circleSprite, liquidColor, sortingOrder - 1),
                body = CreatePart("Body", root.transform, squareSprite, glassColor, sortingOrder),
                neck = CreatePart("Neck", root.transform, squareSprite, glassColor, sortingOrder),
                liquid = CreatePart("Liquid", root.transform, squareSprite, liquidColor, sortingOrder + 1),
                line = CreatePart("Fill Line", root.transform, squareSprite, fillLineColor, sortingOrder + 2),
            };
        }

        static SpriteRenderer CreatePart(string objectName, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        void Release(int bottle)
        {
            var view = active[bottle];
            active.Remove(bottle);
            view.root.SetActive(false);
            pool.Push(view);
        }

        void ReleaseAll()
        {
            toRelease.Clear();
            toRelease.AddRange(active.Keys);
            for (int i = 0; i < toRelease.Count; i++) Release(toRelease[i]);
        }
    }
}
