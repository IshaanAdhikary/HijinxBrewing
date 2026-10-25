using UnityEngine;

namespace Brewing.Mixing
{
    static class FailureSources
    {
        /// <summary>
        /// Returns the assigned component as an <see cref="IFailureSource"/>, or the first one found in the scene
        /// when nothing (or something unsuitable) is assigned.
        /// </summary>
        public static IFailureSource Resolve(MonoBehaviour assigned, Object context)
        {
            if (assigned is IFailureSource direct) return direct;
            if (assigned != null)
                Debug.LogWarning($"{assigned.GetType().Name} does not track failure (IFailureSource). Looking for one in the scene instead.", context);

            foreach (var candidate in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (candidate is IFailureSource found) return found;

            return null;
        }
    }
}
