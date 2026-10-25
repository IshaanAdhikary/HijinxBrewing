using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Pouring
{
    /// <summary>
    /// A screen-corner traffic light that teaches when to pour: green while you should hold both keys, red
    /// otherwise. Before each change it warns you: a row of pips counts down the last few beats, one pip
    /// per beat, and the lamp that is about to light flashes on each of those beats.
    ///
    /// It is pinned to the camera's top-right corner, so it is part of the interface, not the scene. Fade it
    /// with a <see cref="HelperFader"/> on the same object; <see cref="Show"/> brings it back.
    /// </summary>
    public class TrafficLightHud : MonoBehaviour, IFadeable
    {
        [Header("References")]
        [SerializeField] PouringGame game;
        [SerializeField, Tooltip("Must be orthographic. Defaults to the main camera.")] Camera targetCamera;
        [SerializeField] Sprite squareSprite;
        [SerializeField] Sprite circleSprite;

        [Header("Layout (world units)")]
        [SerializeField, Tooltip("Gap from the top-right corner of the screen.")] Vector2 margin = new Vector2(0.4f, 0.4f);
        [SerializeField] float housingWidth = 0.9f;
        [SerializeField] float lampSize = 0.62f;
        [SerializeField] float pipSize = 0.2f;

        [Header("Warning")]
        [SerializeField, Tooltip("How many beats before a change the countdown starts.")] int warnBeats = 4;
        [SerializeField, Tooltip("How long each warning flash takes to fade, in beats.")] float flashBeats = 0.5f;

        [Header("Look")]
        [SerializeField] Color redColor = new Color(0.92f, 0.22f, 0.2f);
        [SerializeField] Color greenColor = new Color(0.25f, 0.85f, 0.35f);
        [SerializeField] Color housingColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        [SerializeField, Range(0f, 1f), Tooltip("Opacity of an unlit lamp.")] float unlitAlpha = 0.18f;
        [SerializeField] int sortingOrder = 50;

        /// <summary>Overall opacity, used for fading it out.</summary>
        public float Alpha { get; set; } = 1f;

        GameObject container;
        SpriteRenderer housing;
        SpriteRenderer redLamp;
        SpriteRenderer greenLamp;
        SpriteRenderer[] pips;

        /// <summary>Brings the light back at full opacity, for example after it has faded out.</summary>
        public void Show()
        {
            Alpha = 1f;
            gameObject.SetActive(true);
        }

        void Awake()
        {
            if (game == null) game = FindAnyObjectByType<PouringGame>();

            container = new GameObject("Traffic Light");
            container.transform.SetParent(transform, false);
            housing = CreatePart("Housing", squareSprite, sortingOrder);
            redLamp = CreatePart("Red Lamp", circleSprite, sortingOrder + 1);
            greenLamp = CreatePart("Green Lamp", circleSprite, sortingOrder + 1);

            pips = new SpriteRenderer[Mathf.Max(0, warnBeats)];
            for (int i = 0; i < pips.Length; i++) pips[i] = CreatePart("Countdown Pip " + i, circleSprite, sortingOrder + 1);
        }

        SpriteRenderer CreatePart(string objectName, Sprite sprite, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(container.transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        void LateUpdate()
        {
            var session = game != null ? game.Session : null;
            bool running = session != null && session.IsRunning;
            if (container.activeSelf != running) container.SetActive(running);
            if (!running) return;

            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null || !cam.orthographic) return;

            Layout(cam);

            var reading = PourMath.Light(session.Conductor.SongBeat, game.Windows);
            bool green = reading.State == PourLight.Green;

            float flash = PourMath.CountdownPips(reading.BeatsUntilChange, warnBeats) > 0
                ? PourMath.CountdownFlash(reading.BeatsUntilChange, flashBeats)
                : 0f;

            // The lit lamp is full; the one about to light flashes on each countdown beat.
            float redLevel = green ? Mathf.Max(unlitAlpha, flash * 0.8f) : 1f;
            float greenLevel = green ? 1f : Mathf.Max(unlitAlpha, flash * 0.8f);
            redLamp.color = WithAlpha(redColor, redLevel * Alpha);
            greenLamp.color = WithAlpha(greenColor, greenLevel * Alpha);
            housing.color = WithAlpha(housingColor, housingColor.a * Alpha);

            // The visible pips are centered under the light, so the row shrinks toward the middle as it counts down.
            int pipCount = PourMath.CountdownPips(reading.BeatsUntilChange, warnBeats);
            Color upcoming = green ? redColor : greenColor;
            float spacing = pipSize * 1.7f;
            float firstX = lightX - spacing * (pipCount - 1) * 0.5f;
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i].enabled = i < pipCount;
                pips[i].color = WithAlpha(upcoming, Alpha);
                if (i < pipCount) Place(pips[i], firstX + spacing * i, pipY, pipSize, pipSize);
            }
        }

        float lightX;
        float pipY;

        void Layout(Camera cam)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 center = cam.transform.position;
            float housingHeight = lampSize * 2f + 0.35f;

            float x = center.x + halfWidth - margin.x - housingWidth * 0.5f;
            float y = center.y + halfHeight - margin.y - housingHeight * 0.5f;

            Place(housing, x, y, housingWidth, housingHeight);
            Place(redLamp, x, y + lampSize * 0.5f + 0.06f, lampSize, lampSize);
            Place(greenLamp, x, y - lampSize * 0.5f - 0.06f, lampSize, lampSize);

            lightX = x;
            pipY = y - housingHeight * 0.5f - pipSize - 0.08f;
        }

        static void Place(SpriteRenderer renderer, float x, float y, float width, float height)
        {
            renderer.transform.position = new Vector3(x, y, 0f);
            renderer.transform.localScale = new Vector3(width, height, 1f);
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
