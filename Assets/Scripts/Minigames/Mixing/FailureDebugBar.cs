using UnityEngine;
using UnityEngine.Serialization;

namespace Brewing.Mixing
{
    /// <summary>
    /// Temporary debug widget: a bar showing how close the brew is to boiling over. The player is not
    /// meant to see this in the real game, so disable or delete it once the bubbles do the job.
    /// </summary>
    public class FailureDebugBar : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("game"), Tooltip("The minigame to show (anything implementing IFailureSource, such as MixingGame or PouringGame).")]
        MonoBehaviour failureSource;
        [SerializeField] Vector2 size = new Vector2(240f, 18f);
        [SerializeField, Tooltip("Distance from the left and bottom edges of the screen, in pixels.")] Vector2 margin = new Vector2(12f, 14f);

        GUIStyle labelStyle;
        IFailureSource game;

        void Awake()
        {
            game = FailureSources.Resolve(failureSource, this);
        }

        void OnGUI()
        {
            if (game == null) return;

            if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            labelStyle.fontSize = 14;

            float level = game.FailureLevel;
            float x = margin.x;
            float y = Screen.height - margin.y - size.y;

            var previous = GUI.color;

            string text = game.IsBoilingOver ? "BOIL OVER" : $"FAILURE {level * 100f:0}%";
            GUI.color = game.IsBoilingOver ? new Color(1f, 0.35f, 0.35f) : Color.white;
            GUI.Label(new Rect(x, y - 24f, size.x, 22f), text, labelStyle);

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, size.x + 4f, size.y + 4f), Texture2D.whiteTexture);

            // Green when calm, through yellow, to red near boiling over.
            GUI.color = level < 0.5f
                ? Color.Lerp(new Color(0.3f, 0.85f, 0.4f), new Color(1f, 0.9f, 0.3f), level * 2f)
                : Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.3f, 0.3f), (level - 0.5f) * 2f);
            GUI.DrawTexture(new Rect(x, y, size.x * Mathf.Clamp01(level), size.y), Texture2D.whiteTexture);

            GUI.color = previous;
        }
    }
}
