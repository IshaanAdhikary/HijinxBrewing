using System.Collections;
using UnityEngine;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Fades a <see cref="ScrollingNoteLane"/> out and then switches it off. Call <see cref="FadeOut"/> from a
    /// chart cue (via a ChartCueListener binding). Put this on the lane's own GameObject; it is switched off
    /// along with the lane, and <see cref="ScrollingNoteLane.Show"/> brings the lane back.
    /// </summary>
    public class NoteLaneFader : MonoBehaviour
    {
        [SerializeField] ScrollingNoteLane lane;
        [SerializeField, Tooltip("Seconds the fade takes.")] float fadeSeconds = 1.5f;

        Coroutine running;

        void Awake()
        {
            if (lane == null) lane = GetComponent<ScrollingNoteLane>();
        }

        public void FadeOut()
        {
            if (!isActiveAndEnabled) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Fade());
        }

        IEnumerator Fade()
        {
            float startAlpha = lane.Alpha;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                lane.Alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }

            lane.Alpha = 0f;
            running = null;
            lane.gameObject.SetActive(false);
        }
    }
}
