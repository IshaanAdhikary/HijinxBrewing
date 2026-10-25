using System.Collections;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Fades a helper visual out and then switches its GameObject off. Put this on the same GameObject as the
    /// visual (anything implementing <see cref="IFadeable"/>, such as ScrollingNoteLane or StirHelperOverlay)
    /// and call <see cref="FadeOut"/> from a chart cue via a ChartCueListener binding. The visual's own
    /// Show() method brings it back.
    /// </summary>
    public class HelperFader : MonoBehaviour
    {
        [SerializeField, Tooltip("Seconds the fade takes.")] float fadeSeconds = 3f;

        IFadeable target;
        Coroutine running;

        void Awake()
        {
            target = GetComponent<IFadeable>();
            if (target == null) Debug.LogWarning("HelperFader: no IFadeable component on this GameObject to fade.", this);
        }

        public void FadeOut()
        {
            if (target == null || !isActiveAndEnabled) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Fade());
        }

        IEnumerator Fade()
        {
            float startAlpha = target.Alpha;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                target.Alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }

            target.Alpha = 0f;
            running = null;
            gameObject.SetActive(false);
        }
    }
}
