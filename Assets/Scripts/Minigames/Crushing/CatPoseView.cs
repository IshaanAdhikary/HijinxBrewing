using UnityEngine;
using Brewing.Rhythm;

namespace Brewing.Crushing
{
    /// <summary>
    /// Swaps the cat's sprite to match what is happening: a paw on each key press, frazzled (and shaking, with
    /// a random cry) on a miss, a jump after a crushed ingredient, and sitting otherwise. The cat stays where it was placed
    /// in the scene, and every other pose is lined up with the idle pose's bottom center, so sprites of
    /// different sizes stand on the same spot. Put this on a child of the object that pulses, so the shake
    /// and the pulse do not fight over the transform.
    /// </summary>
    public class CatPoseView : MonoBehaviour
    {
        [SerializeField] CrushingGame game;
        [SerializeField] SpriteRenderer body;

        [Header("Sprites")]
        [SerializeField] Sprite idle;
        [SerializeField] Sprite pawLeft;
        [SerializeField] Sprite pawRight;
        [SerializeField] Sprite frazzled;
        [SerializeField, Tooltip("Shown after an ingredient is crushed.")] Sprite cheer;

        [Header("Timing (seconds)")]
        [SerializeField] float pawSeconds = 0.25f;
        [SerializeField] float frazzledSeconds = 0.4f;
        [SerializeField] float cheerSeconds = 0.6f;

        [Header("Shake while frazzled")]
        [SerializeField, Tooltip("In world units.")] float shakeAmount = 0.06f;
        [SerializeField] float shakeSpeed = 45f;

        [Header("Hop when cheering")]
        [SerializeField, Tooltip("Peak height of the hop, in world units.")] float hopHeight = 0.35f;
        [SerializeField, Tooltip("Seconds from leaving the ground to landing. The cheer pose holds a little after.")] float hopSeconds = 0.35f;

        [Header("Cry on a miss")]
        [SerializeField, Tooltip("One is picked at random for each miss, never the same one twice in a row.")] AudioClip[] missSounds = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] float missVolume = 0.8f;
        [SerializeField, Tooltip("Shortest gap between cries, in seconds, so quick misses in a row don't pile up.")] float minSecondsBetweenCries = 0.5f;

        // Higher priorities are not interrupted by lower ones until they end.
        const int PawPriority = 1, CheerPriority = 1, MissPriority = 2;

        Sprite pose;
        int priority;
        float poseUntil;
        float poseStart;
        bool shaking;
        Vector3 restPosition;
        AudioSource voice;
        int lastCry = -1;
        float lastCryTime = float.NegativeInfinity;

        void Awake()
        {
            restPosition = body.transform.localPosition;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
        }

        void OnEnable()
        {
            game.Session.ButtonInput += OnButtonInput;
            game.NoteJudged += OnNoteJudged;
            game.RoundFinished += OnRoundFinished;
        }

        void OnDisable()
        {
            game.Session.ButtonInput -= OnButtonInput;
            game.NoteJudged -= OnNoteJudged;
            game.RoundFinished -= OnRoundFinished;
        }

        void OnButtonInput(RhythmAction action)
        {
            if (action.kind != InputKind.Press) return;
            Show(action.button == CallResponseBuilder.LeftButton ? pawLeft : pawRight, pawSeconds, PawPriority, false);
        }

        void OnNoteJudged(JudgeResult result)
        {
            if (result.Judgement != Judgement.Miss) return;
            Show(frazzled, frazzledSeconds, MissPriority, true);
            Cry();
        }

        void Cry()
        {
            if (missSounds.Length == 0 || Time.time - lastCryTime < minSecondsBetweenCries) return;

            int pick = Random.Range(0, missSounds.Length);
            if (missSounds.Length > 1 && pick == lastCry) pick = (pick + Random.Range(1, missSounds.Length)) % missSounds.Length;
            if (missSounds[pick] == null) return;

            voice.PlayOneShot(missSounds[pick], missVolume);
            lastCry = pick;
            lastCryTime = Time.time;
        }

        void OnRoundFinished(int round, bool crushed)
        {
            if (crushed) Show(cheer, cheerSeconds, CheerPriority, false);
        }


        void Show(Sprite sprite, float seconds, int newPriority, bool shake)
        {
            if (sprite == null) return;
            if (Time.time < poseUntil && newPriority < priority) return;
            pose = sprite;
            priority = newPriority;
            poseStart = Time.time;
            poseUntil = Time.time + seconds;
            shaking = shake;
        }

        void Update()
        {
            if (Time.time >= poseUntil)
            {
                pose = idle;
                priority = 0;
                shaking = false;
            }

            body.sprite = pose != null ? pose : idle;
            if (body.sprite == null) return;

            var offset = restPosition;
            if (idle != null) offset += BottomCenter(idle) - BottomCenter(body.sprite);
            if (shaking)
            {
                float t = Time.time * shakeSpeed;
                offset += new Vector3(Mathf.Sin(t), Mathf.Sin(t * 1.7f) * 0.5f, 0f) * (shakeAmount / Mathf.Max(0.0001f, body.transform.lossyScale.x));
            }
            if (pose == cheer && hopSeconds > 0f)
            {
                float t = (Time.time - poseStart) / hopSeconds;
                if (t < 1f) offset.y += hopHeight * 4f * t * (1f - t) / Mathf.Max(0.0001f, body.transform.lossyScale.y);
            }
            body.transform.localPosition = offset;
        }

        static Vector3 BottomCenter(Sprite sprite)
        {
            var bounds = sprite.bounds;
            return new Vector3(bounds.center.x, bounds.min.y, 0f);
        }
    }
}
