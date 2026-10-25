using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Brewing.Rhythm;

namespace Brewing.Minigames
{
    /// <summary>
    /// The player-facing hit feedback: a painted bar that stays on screen while the chart plays. Each hit drops
    /// a marker on it, colored by its rating and placed by its timing (left is early, right is late, the middle
    /// is dead on), and the rating word pops up above. Markers and the word fade out. A miss has no timing, so
    /// it shows only the word.
    /// </summary>
    public class JudgementBar : MonoBehaviour
    {
        [SerializeField] MinigameRun game;
        [SerializeField, Tooltip("Shown while the chart plays, hidden otherwise. Keep this object (with the script) active.")] GameObject root;
        [SerializeField, Tooltip("The bar image. Markers are placed along its width.")] RectTransform bar;
        [SerializeField, Tooltip("A hidden marker image, copied for each hit.")] Image markerTemplate;
        [SerializeField] TMP_Text label;

        [Header("Timing")]
        [SerializeField, Tooltip("Timing error, in seconds, that reaches the end of the bar. Later or earlier hits sit at the end.")] float barRangeSeconds = 0.12f;
        [SerializeField, Tooltip("Seconds a marker and the word take to fade away.")] float fadeSeconds = 1f;
        [SerializeField, Tooltip("How much bigger the word pops on a new hit. 1.25 is 25% bigger.")] float labelPop = 1.25f;
        [SerializeField] float popSeconds = 0.15f;

        [Header("Look")]
        [SerializeField] string perfectText = "Perfect";
        [SerializeField] string goodText = "Good";
        [SerializeField] string missText = "Miss";
        [SerializeField] Color perfectColor = new Color(0.45f, 0.8f, 0.35f);
        [SerializeField] Color goodColor = new Color(0.95f, 0.78f, 0.25f);
        [SerializeField] Color missColor = new Color(0.88f, 0.3f, 0.22f);

        class Marker
        {
            public Image image;
            public Color color;
            public float time;
        }

        readonly List<Marker> markers = new List<Marker>();
        Color labelColor;
        float labelTime = float.NegativeInfinity;
        bool shown;

        void Awake()
        {
            markerTemplate.gameObject.SetActive(false);
            label.text = "";
            SetShown(false);
        }

        void OnEnable()
        {
            game.Session.Judged += OnJudged;
        }

        void OnDisable()
        {
            game.Session.Judged -= OnJudged;
        }

        void OnJudged(JudgeResult result)
        {
            var color = ColorFor(result.Judgement);
            label.text = TextFor(result.Judgement);
            labelColor = color;
            labelTime = Time.unscaledTime;

            if (result.Judgement == Judgement.Miss) return;

            var marker = FreeMarker();
            marker.color = color;
            marker.time = Time.unscaledTime;
            float t = barRangeSeconds > 0f ? Mathf.Clamp((float)result.ErrorSeconds / barRangeSeconds, -1f, 1f) : 0f;
            marker.image.rectTransform.anchoredPosition = new Vector2(t * bar.rect.width * 0.5f, 0f);
            marker.image.gameObject.SetActive(true);
            marker.image.transform.SetAsLastSibling();
        }

        void Update()
        {
            bool running = game.Session.IsRunning && !game.IsFinished;
            if (running != shown) SetShown(running);
            if (!shown) return;

            float now = Time.unscaledTime;
            foreach (var marker in markers)
            {
                if (!marker.image.gameObject.activeSelf) continue;
                float fade = Fade(now - marker.time);
                if (fade <= 0f)
                {
                    marker.image.gameObject.SetActive(false);
                    continue;
                }
                var color = marker.color;
                color.a *= fade;
                marker.image.color = color;
            }

            float age = now - labelTime;
            var text = labelColor;
            text.a *= Fade(age);
            label.color = text;
            float pop = popSeconds > 0f ? Mathf.Clamp01(1f - age / popSeconds) : 0f;
            label.transform.localScale = Vector3.one * (1f + (labelPop - 1f) * pop);
        }

        void SetShown(bool show)
        {
            shown = show;
            root.SetActive(show);
            if (show) return;
            foreach (var marker in markers) marker.image.gameObject.SetActive(false);
            labelTime = float.NegativeInfinity;
        }

        Marker FreeMarker()
        {
            foreach (var marker in markers)
                if (!marker.image.gameObject.activeSelf) return marker;

            var image = Instantiate(markerTemplate, markerTemplate.transform.parent);
            image.name = "Marker";
            var created = new Marker { image = image };
            markers.Add(created);
            return created;
        }

        /// <summary>1 when new, easing to 0 after <see cref="fadeSeconds"/>.</summary>
        float Fade(float age)
        {
            if (fadeSeconds <= 0f || age < 0f || age >= fadeSeconds) return 0f;
            float t = age / fadeSeconds;
            return 1f - t * t;
        }

        string TextFor(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect: return perfectText;
                case Judgement.Good: return goodText;
                default: return missText;
            }
        }

        Color ColorFor(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect: return perfectColor;
                case Judgement.Good: return goodColor;
                default: return missColor;
            }
        }
    }
}
