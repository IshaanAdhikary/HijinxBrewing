using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Maps cue names from the chart to UnityEvents, so you can hook things up in the Inspector. Cue names
    /// are matched ignoring case. Use one listener per session: a cue with no matching binding logs a warning.
    /// </summary>
    public class ChartCueListener : MonoBehaviour
    {
        [Serializable]
        public class CueBinding
        {
            public string cueName;
            public UnityEvent onCue = new UnityEvent();
        }

        [SerializeField] RhythmSession session;
        [SerializeField] List<CueBinding> bindings = new List<CueBinding>();
        [SerializeField, Tooltip("Log a warning when a cue fires that has no binding here.")] bool warnOnUnmatched = true;

        public IReadOnlyList<CueBinding> Bindings => bindings;

        /// <summary>Adds a binding from code. Returns it so the caller can add listeners to its event.</summary>
        public CueBinding AddBinding(string cueName)
        {
            var binding = new CueBinding { cueName = cueName };
            bindings.Add(binding);
            return binding;
        }

        void Awake()
        {
            if (session == null) session = FindAnyObjectByType<RhythmSession>();
        }

        void OnEnable()
        {
            if (session != null) session.CueTriggered += OnCue;
        }

        void OnDisable()
        {
            if (session != null) session.CueTriggered -= OnCue;
        }

        void OnCue(ChartCue cue)
        {
            bool matched = false;
            for (int i = 0; i < bindings.Count; i++)
            {
                if (!string.Equals(bindings[i].cueName, cue.name, StringComparison.OrdinalIgnoreCase)) continue;
                matched = true;
                bindings[i].onCue.Invoke();
            }

            if (!matched && warnOnUnmatched)
                Debug.LogWarning($"ChartCueListener: no binding for cue '{cue.name}' (beat {cue.beat}).", this);
        }
    }
}
