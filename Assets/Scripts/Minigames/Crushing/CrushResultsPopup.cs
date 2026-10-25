using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brewing.Crushing
{
    /// <summary>
    /// The pop-up at the end of the chart: a pass or fail heading, how many ingredients were crushed out of
    /// the total, and how many were needed. Retry hides it and restarts the chart.
    /// </summary>
    public class CrushResultsPopup : MonoBehaviour
    {
        [SerializeField] CrushingGame game;
        [SerializeField, Tooltip("Shown and hidden as a whole. Keep this object (with the script) active.")] GameObject panel;
        [SerializeField] TMP_Text heading;
        [SerializeField] TMP_Text crushedText;
        [SerializeField] TMP_Text neededText;
        [SerializeField] Button retryButton;

        [Header("Wording")]
        [SerializeField] string passHeading = "Brew Complete!";
        [SerializeField] string failHeading = "Not Quite...";
        [SerializeField, Tooltip("{0} is the number crushed, {1} the total.")] string crushedFormat = "{0} / {1} ingredients crushed";
        [SerializeField, Tooltip("{0} is the number needed.")] string neededFormat = "{0} needed";
        [SerializeField] Color passColor = new Color(0.25f, 0.45f, 0.2f);
        [SerializeField] Color failColor = new Color(0.6f, 0.18f, 0.12f);

        void Awake()
        {
            panel.SetActive(false);
        }

        void OnEnable()
        {
            game.Finished += Show;
            game.ChartStarted.AddListener(Hide);
            retryButton.onClick.AddListener(Retry);
        }

        void OnDisable()
        {
            game.Finished -= Show;
            game.ChartStarted.RemoveListener(Hide);
            retryButton.onClick.RemoveListener(Retry);
        }

        void Show()
        {
            heading.text = game.Passed ? passHeading : failHeading;
            heading.color = game.Passed ? passColor : failColor;
            crushedText.text = string.Format(crushedFormat, game.Crushed, game.TotalIngredients);
            neededText.text = string.Format(neededFormat, game.Needed);
            panel.SetActive(true);
            retryButton.Select();
        }

        void Hide() => panel.SetActive(false);

        void Retry() => game.Begin();
    }
}
