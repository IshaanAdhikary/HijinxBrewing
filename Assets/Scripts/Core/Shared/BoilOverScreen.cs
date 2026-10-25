using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace Brewing.Minigames
{
    /// <summary>
    /// Plays a full-screen animation when the brew boils over, holds on its last frame, then restarts the chart
    /// when the player presses any key, mouse button or gamepad button. Turn off Auto Restart on the minigame
    /// so this is what restarts it. Input only counts once the animation has finished, so a press already in
    /// progress cannot skip it.
    /// </summary>
    public class BoilOverScreen : MonoBehaviour
    {
        [SerializeField] FailureRun game;
        [SerializeField, Tooltip("Shown and hidden as a whole. Keep this object (with the script) active.")] GameObject screen;
        [SerializeField] Image image;

        [Header("Animation")]
        [SerializeField, Tooltip("Played in this order. Repeat a sprite to hold it for longer.")] Sprite[] frames = new Sprite[0];
        [SerializeField] float framesPerSecond = 8f;
        [SerializeField, Tooltip("A black screen is shown after this many frames of the list have played. 0 for none.")] int blackAfterFrame;
        [SerializeField, Tooltip("Seconds the black screen holds.")] float blackSeconds = 1.5f;
        [SerializeField, Tooltip("Optional. Played when the frame at Sound At Frame shows.")] AudioClip sound;
        [SerializeField, Tooltip("Position in the Frames list (1 is the first) that the sound plays on.")] int soundAtFrame = 1;

        AudioSource audioSource;
        IDisposable anyButton;

        void Awake()
        {
            screen.SetActive(false);
            if (sound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        void OnEnable()
        {
            game.Failed += Play;
            game.ChartStarted.AddListener(Hide);
        }

        void OnDisable()
        {
            game.Failed -= Play;
            game.ChartStarted.RemoveListener(Hide);
            StopWaiting();
        }

        void Play()
        {
            StopAllCoroutines();
            StopWaiting();
            StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            screen.SetActive(true);

            float frameSeconds = framesPerSecond > 0f ? 1f / framesPerSecond : 0.125f;
            image.color = Color.white;
            for (int i = 0; i < frames.Length; i++)
            {
                image.sprite = frames[i];
                if (i + 1 == soundAtFrame && audioSource != null) audioSource.PlayOneShot(sound);
                yield return new WaitForSecondsRealtime(frameSeconds);

                if (i + 1 == blackAfterFrame)
                {
                    // An image with no sprite draws as a flat block of its color.
                    image.sprite = null;
                    image.color = Color.black;
                    yield return new WaitForSecondsRealtime(blackSeconds);
                    image.color = Color.white;
                }
            }

            anyButton = InputSystem.onAnyButtonPress.CallOnce(_ => Restart());
        }

        void Restart()
        {
            anyButton = null;
            game.Begin();
        }

        void Hide()
        {
            StopAllCoroutines();
            StopWaiting();
            screen.SetActive(false);
        }

        void StopWaiting()
        {
            anyButton?.Dispose();
            anyButton = null;
        }
    }
}
