using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Mixing
{
    /// <summary>
    /// Switches mixing's music between the song's calm and intense versions to match the tempo of the section
    /// being played. The cut happens as the section starts. During the lead-in it follows the first section,
    /// so a restart begins on whichever version the chart starts with. Needs a song with an intense clip.
    /// </summary>
    public class MixingMusicIntensity : MonoBehaviour
    {
        [SerializeField] MixingGame game;

        [Header("Play the intense version during")]
        [SerializeField] bool halfTime;
        [SerializeField] bool normalTime = true;
        [SerializeField] bool doubleTime = true;

        void Update()
        {
            var session = game.Session;
            if (!session.IsRunning || !(session.Chart is PatternChart chart)) return;

            var mode = TempoBlend.At(session.Conductor.SongBeat, chart.sections, 0.0).From;
            session.Conductor.Intense = IsIntense(mode);
        }

        bool IsIntense(TempoMode mode)
        {
            switch (mode)
            {
                case TempoMode.Half: return halfTime;
                case TempoMode.Double: return doubleTime;
                default: return normalTime;
            }
        }
    }
}
