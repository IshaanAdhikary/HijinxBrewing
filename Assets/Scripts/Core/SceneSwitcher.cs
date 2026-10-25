using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSwitcher : MonoBehaviour
{
    // The order scenes are played in. NextScene goes to the one after the current scene, looping back to the start.
    static readonly string[] Flow = { "Title", "Opening", "Crushing", "Mixing", "Credits" };

    static SceneSwitcher instance;

    /// <summary>The scene switcher, created on first use so scenes can be played directly in the Editor.</summary>
    public static SceneSwitcher Instance
    {
        get
        {
            if (instance == null) new GameObject("Scene Switcher").AddComponent<SceneSwitcher>();
            return instance;
        }
    }

    [SerializeField, Tooltip("Seconds to fade to black, and again to fade back in.")] float fadeSeconds = 0.4f;

    CanvasGroup fade;
    AudioSource sfx;
    bool switching;

    private void Awake()
    {
        // Check if an instance already exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instance
            return;
        }

        // Set the active instance
        instance = this;

        // Keep this object alive across scene changes
        DontDestroyOnLoad(gameObject);
        BuildFade();
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
    }

    public void ToTitle()
    {
        LoadWithFade(Flow[0]);
    }

    /// <summary>
    /// Goes to the next scene, playing a sound first. The sound plays from this object, which survives the
    /// scene change, so it is not cut off when the old scene unloads.
    /// </summary>
    public void NextScene(AudioClip sound)
    {
        if (switching) return;
        if (sound != null) sfx.PlayOneShot(sound);
        NextScene();
    }

    public void NextScene()
    {
        string current = SceneManager.GetActiveScene().name;
        int index = System.Array.IndexOf(Flow, current);
        if (index < 0)
        {
            Debug.LogWarning($"Scene '{current}' is not in the scene flow, so going to {Flow[0]}.");
            LoadWithFade(Flow[0]);
            return;
        }
        LoadWithFade(Flow[(index + 1) % Flow.Length]);
    }

    void LoadWithFade(string scene)
    {
        if (switching) return;
        StartCoroutine(FadeAndLoad(scene));
    }

    IEnumerator FadeAndLoad(string scene)
    {
        switching = true;
        fade.blocksRaycasts = true;
        yield return Fade(0f, 1f);

        yield return SceneManager.LoadSceneAsync(scene);

        yield return Fade(1f, 0f);
        fade.blocksRaycasts = false;
        switching = false;
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            fade.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
            yield return null;
        }
        fade.alpha = to;
    }

    // A black screen drawn over everything, kept with this object across scenes.
    void BuildFade()
    {
        var canvas = new GameObject("Fade", typeof(RectTransform)).AddComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        var image = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(canvas.transform, false);
        image.color = Color.black;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        fade = canvas.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0f;
        fade.blocksRaycasts = false;
    }
}
