using UnityEngine;

namespace Brewing.Minigames
{
    /// <summary>The numbers behind the cauldron bubbles, kept separate so they can be tested.</summary>
    public static class BubbleMath
    {
        const float GrowEnd = 0.2f;
        const float PopStart = 0.85f;
        const float PopGrowth = 0.35f;

        /// <summary>
        /// How many bubbles to show for a danger level. None until danger passes <paramref name="startDanger"/>,
        /// then at least one, rising to <paramref name="maxBubbles"/> at full danger. A power above 1 keeps the
        /// count low for longer and ramps up late; below 1 ramps up early.
        /// </summary>
        public static int TargetCount(float danger, float startDanger, int maxBubbles, float power)
        {
            if (maxBubbles <= 0 || danger <= startDanger) return 0;

            float t = Mathf.Clamp01((danger - startDanger) / Mathf.Max(1e-4f, 1f - startDanger));
            int count = Mathf.CeilToInt(maxBubbles * Mathf.Pow(t, power));
            return Mathf.Clamp(count, 1, maxBubbles);
        }

        /// <summary>
        /// A bubble's look over its life, with time from 0 to 1: it swells in, holds, then pops by growing
        /// slightly and fading out.
        /// </summary>
        public static void Evaluate(float lifeFraction, out float scale, out float alpha)
        {
            float t = Mathf.Clamp01(lifeFraction);
            if (t < GrowEnd)
            {
                float u = t / GrowEnd;
                scale = 1f - (1f - u) * (1f - u);
                alpha = u;
            }
            else if (t < PopStart)
            {
                scale = 1f;
                alpha = 1f;
            }
            else
            {
                float u = (t - PopStart) / (1f - PopStart);
                scale = 1f + PopGrowth * u;
                alpha = 1f - u;
            }
        }
    }
}
