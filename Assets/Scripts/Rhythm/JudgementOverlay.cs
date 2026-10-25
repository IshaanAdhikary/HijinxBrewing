using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Temporary debug graphic: shows the latest hit rating, its timing error in milliseconds, and where it
    /// landed on an early/late bar. Drop it in any scene with a <see cref="RhythmSession"/>. Disable or
    /// delete it once real feedback art exists.
    /// </summary>
    public class JudgementOverlay : MonoBehaviour
    {
        [SerializeField] RhythmSession session;
        [SerializeField, Tooltip("How long a result stays on screen, in seconds.")] float showSeconds = 1.2f;
        [SerializeField, Tooltip("Timing error shown at the edge of the bar, in seconds.")] float barRangeSeconds = 0.12f;
        [SerializeField, Range(0f, 1f), Tooltip("Top of the block as a fraction of the screen height from the top.")] float verticalPosition = 0.83f;
        [SerializeField, Tooltip("Show every result from the session. Turn off when a minigame passes its own combined results to Show() instead.")]
        bool listenToSession = true;

        bool hasResult;
        JudgeResult result;
        float resultRealtime;
        GUIStyle bigStyle;
        GUIStyle smallStyle;

        /// <summary>Shows a result directly, for minigames that combine several judgements into one.</summary>
        public void Show(JudgeResult judged) => OnJudged(judged);

        void Awake()
        {
            if (session == null) session = FindAnyObjectByType<RhythmSession>();
        }

        void OnEnable()
        {
            if (session != null && listenToSession) session.Judged += OnJudged;
        }

        void OnDisable()
        {
            if (session != null && listenToSession) session.Judged -= OnJudged;
        }

        void OnJudged(JudgeResult judged)
        {
            result = judged;
            resultRealtime = Time.realtimeSinceStartup;
            hasResult = true;
        }

        void OnGUI()
        {
            if (!hasResult) return;

            float age = Time.realtimeSinceStartup - resultRealtime;
            if (age > showSeconds) return;
            float alpha = Mathf.Clamp01((showSeconds - age) / 0.4f);

            if (bigStyle == null)
            {
                bigStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            }
            bigStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.06f);
            smallStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.028f);

            float centerX = Screen.width * 0.5f;
            float y = Screen.height * verticalPosition;
            bool miss = result.Judgement == Judgement.Miss;
            Color color = ColorFor(result.Judgement);
            color.a = alpha;

            string label = miss ? "MISS" : result.Judgement.ToString().ToUpper();
            string action = result.Event.Action.ToString();
            string timing = miss ? action : $"{result.ErrorSeconds * 1000.0:+0;-0;0} ms {(result.ErrorSeconds < 0 ? "early" : "late")}  -  {action}";

            var previous = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(centerX - 300, y, 600, Screen.height * 0.08f), label, bigStyle);
            GUI.Label(new Rect(centerX - 300, y + Screen.height * 0.075f, 600, Screen.height * 0.04f), timing, smallStyle);

            if (!miss) DrawBar(centerX, y + Screen.height * 0.135f, alpha, color);
            GUI.color = previous;
        }

        void DrawBar(float centerX, float y, float alpha, Color markerColor)
        {
            const float width = 320f;
            const float height = 10f;

            Fill(new Rect(centerX - width / 2, y, width, height), new Color(0f, 0f, 0f, 0.45f * alpha));
            Fill(new Rect(centerX - 1.5f, y - 6, 3, height + 12), new Color(1f, 1f, 1f, 0.8f * alpha));

            float t = Mathf.Clamp((float)result.ErrorSeconds / barRangeSeconds, -1f, 1f);
            Fill(new Rect(centerX + t * width / 2 - 4, y - 8, 8, height + 16), markerColor);
        }

        static Color ColorFor(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect: return new Color(0.4f, 1f, 0.5f);
                case Judgement.Good: return new Color(1f, 0.9f, 0.3f);
                default: return new Color(1f, 0.35f, 0.35f);
            }
        }

        static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }
    }
}
