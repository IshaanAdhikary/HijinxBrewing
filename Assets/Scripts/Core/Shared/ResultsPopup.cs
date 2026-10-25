using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brewing.Minigames
{
    /// <summary>
    /// The pop-up at the end of a minigame: a pass or fail heading, up to two lines of detail from the
    /// minigame's <see cref="MinigameRun.Results"/>, and a Retry button that restarts the chart. Lives on the
    /// ResultsPopup prefab; drop an instance in a scene and set its Game.
    /// </summary>
    public class ResultsPopup : MonoBehaviour
    {
        [SerializeField, Tooltip("The minigame in this scene. Set this on each scene's instance of the prefab.")] MinigameRun game;
        [SerializeField, Tooltip("Shown and hidden as a whole. Keep this object (with the script) active.")] GameObject panel;
        [SerializeField] TMP_Text heading;
        [SerializeField] TMP_Text detail;
        [SerializeField] TMP_Text subDetail;
        [SerializeField] Button retryButton;

        [Header("Wording")]
        [SerializeField] string passHeading = "Brew Complete!";
        [SerializeField] string failHeading = "Not Quite...";
        [SerializeField] Color passColor = new Color(0.25f, 0.45f, 0.2f);
        [SerializeField] Color failColor = new Color(0.6f, 0.18f, 0.12f);

        void Awake()
        {
            panel.SetActive(false);
            if (game == null) Debug.LogError("ResultsPopup: no minigame assigned, so it will never show. Set Game on this instance.", this);
        }

        void OnEnable()
        {
            if (game == null) return;
            game.Finished += Show;
            game.ChartStarted.AddListener(Hide);
            retryButton.onClick.AddListener(Retry);
        }

        void OnDisable()
        {
            if (game == null) return;
            game.Finished -= Show;
            game.ChartStarted.RemoveListener(Hide);
            retryButton.onClick.RemoveListener(Retry);
        }

        void Show()
        {
            var results = game.Results;
            heading.text = results.Passed ? passHeading : failHeading;
            heading.color = results.Passed ? passColor : failColor;
            SetLine(detail, results.Detail);
            SetLine(subDetail, results.SubDetail);
            panel.SetActive(true);
            retryButton.Select();
        }

        void Hide() => panel.SetActive(false);

        void Retry() => game.Begin();

        static void SetLine(TMP_Text line, string text)
        {
            line.text = text;
            line.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }
}
