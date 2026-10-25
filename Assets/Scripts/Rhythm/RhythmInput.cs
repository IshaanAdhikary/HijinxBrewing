using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Brewing.Rhythm
{
    /// <summary>
    /// Turns button presses and releases into timestamped events. Buttons are numbered by their position in
    /// <see cref="buttonActionNames"/>: 0 is left and 1 is right. With no asset assigned it creates default
    /// keyboard bindings (Left/A and Right/D). Assign an asset to rebind or add gamepad support.
    /// </summary>
    public class RhythmInput : MonoBehaviour
    {
        [Tooltip("Optional. Leave empty to use the built-in keyboard bindings.")]
        [SerializeField] InputActionAsset actionsAsset;
        [SerializeField] string actionMapName = "Rhythm";
        [SerializeField] string[] buttonActionNames = { "LeftPaw", "RightPaw" };

        InputActionMap map;
        InputAction[] actions;
        Action<InputAction.CallbackContext>[] pressHandlers;
        Action<InputAction.CallbackContext>[] releaseHandlers;
        bool warnedAboutTimestamps;

        /// <summary>Raised on every press or release. The double is a Time.realtimeSinceStartup timestamp.</summary>
        public event Action<RhythmAction, double> ButtonInput;

        public bool IsHeld(int button) => actions != null && button < actions.Length && actions[button].IsPressed();

        void Awake()
        {
            BuildActions();
        }

        void BuildActions()
        {
            if (actionsAsset != null)
            {
                map = actionsAsset.FindActionMap(actionMapName, true);
            }
            else
            {
                map = new InputActionMap(actionMapName);
                var left = map.AddAction("LeftPaw", InputActionType.Button);
                left.AddBinding("<Keyboard>/leftArrow");
                left.AddBinding("<Keyboard>/a");
                var right = map.AddAction("RightPaw", InputActionType.Button);
                right.AddBinding("<Keyboard>/rightArrow");
                right.AddBinding("<Keyboard>/d");
            }

            actions = new InputAction[buttonActionNames.Length];
            pressHandlers = new Action<InputAction.CallbackContext>[buttonActionNames.Length];
            releaseHandlers = new Action<InputAction.CallbackContext>[buttonActionNames.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                int button = i;
                actions[i] = map.FindAction(buttonActionNames[i], true);
                pressHandlers[i] = ctx => Raise(button, InputKind.Press, ctx.time);
                releaseHandlers[i] = ctx => Raise(button, InputKind.Release, ctx.time);
            }
        }

        void OnEnable()
        {
            if (actions == null) BuildActions();
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i].performed += pressHandlers[i];
                actions[i].canceled += releaseHandlers[i];
            }
            map.Enable();
        }

        void OnDisable()
        {
            if (actions == null) return;
            map.Disable();
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i].performed -= pressHandlers[i];
                actions[i].canceled -= releaseHandlers[i];
            }
        }

        void Raise(int button, InputKind kind, double eventTime)
        {
            // The Input System stamps events on the Time.realtimeSinceStartup timeline. If a timestamp ever
            // looks implausible (in the future, or more than a quarter second old) fall back to "now".
            double now = Time.realtimeSinceStartupAsDouble;
            double age = now - eventTime;
            if (age < 0.0 || age > 0.25)
            {
                if (!warnedAboutTimestamps)
                {
                    warnedAboutTimestamps = true;
                    Debug.LogWarning($"RhythmInput: unexpected input event timestamp (age {age:F3}s), using frame time instead.");
                }
                eventTime = now;
            }

            ButtonInput?.Invoke(new RhythmAction(button, kind), eventTime);
        }
    }
}
