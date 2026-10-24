using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>Starts the mixing minigame: runs its chart on the rhythm session.</summary>
    public class MixingGame : MonoBehaviour
    {
        [SerializeField] RhythmSession session;
        [SerializeField] RhythmChart chart;
        [SerializeField] bool startOnLoad = true;
        [SerializeField] float leadInSeconds = 2f;

        void Start()
        {
            if (startOnLoad) Begin();
        }

        public void Begin()
        {
            session.Begin(chart, leadInSeconds);
        }
    }
}
