namespace Brewing.Minigames
{
    /// <summary>
    /// Anything that tracks how close the brew is to boiling over. The bubbles and the failure debug bar read
    /// from this, so any minigame (mixing, pouring) can drive them.
    /// </summary>
    public interface IFailureSource
    {
        /// <summary>0 is calm, 1 is boiled over.</summary>
        float FailureLevel { get; }

        bool IsBoilingOver { get; }
    }
}
