using UnityEngine;

namespace Brewing.Crushing
{
    /// <summary>
    /// Temporary debug widget: failed ingredients so far against the most allowed, and how many have been
    /// crushed. The player is not meant to see this, so disable or delete it once real feedback exists.
    /// </summary>
    public class CrushDebugCounter : MonoBehaviour
    {
        [SerializeField] CrushingGame game;
        [SerializeField, Tooltip("Distance from the left and bottom edges of the screen, in pixels.")] Vector2 margin = new Vector2(12f, 14f);

        GUIStyle style;

        void OnGUI()
        {
            if (game == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };

            bool over = game.Failures > game.MaxFailures;
            string text = $"FAILURES {game.Failures} / {game.MaxFailures}    CRUSHED {game.Crushed} / {game.TotalIngredients}";

            var previous = GUI.color;
            var rect = new Rect(margin.x, Screen.height - margin.y - 22f, 420f, 22f);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(rect.x - 4f, rect.y, style.CalcSize(new GUIContent(text)).x + 8f, rect.height), Texture2D.whiteTexture);
            GUI.color = over ? new Color(1f, 0.35f, 0.35f) : Color.white;
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }
    }
}
