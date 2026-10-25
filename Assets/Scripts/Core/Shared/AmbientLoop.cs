using UnityEngine;

namespace Brewing.Minigames
{
    /// <summary>
    /// A background sound that loops while a run is in progress: it starts with the chart (lead-in included),
    /// stops when the run finishes, stops on a boil-over so the failure plays alone, and starts again on retry.
    /// </summary>
    public class AmbientLoop : MonoBehaviour
    {
        [SerializeField] MinigameRun game;
        [SerializeField] AudioClip clip;
        [SerializeField, Range(0f, 1f)] float volume = 0.5f;

        AudioSource source;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.clip = clip;
        }

        void OnEnable()
        {
            game.ChartStarted.AddListener(Play);
            game.Finished += Stop;
            if (game is FailureRun failing) failing.Failed += Stop;
        }

        void OnDisable()
        {
            game.ChartStarted.RemoveListener(Play);
            game.Finished -= Stop;
            if (game is FailureRun failing) failing.Failed -= Stop;
            Stop();
        }

        void Play()
        {
            if (clip == null) return;
            source.volume = volume;
            if (!source.isPlaying) source.Play();
        }

        void Stop() => source.Stop();
    }
}
