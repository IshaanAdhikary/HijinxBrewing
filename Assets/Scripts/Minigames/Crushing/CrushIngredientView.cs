using System.Collections.Generic;
using UnityEngine;
using Brewing.Minigames;
using Brewing.Rhythm;

namespace Brewing.Crushing
{
    /// <summary>
    /// The ingredient being crushed. It appears at the crush spot as each response starts, using the same
    /// sprite as the book, and gets a little flatter with every hit. If the round succeeds, the last hit turns
    /// it into powder of the ingredient's color, which is then thrown into the bowl. If it fails, the whole
    /// ingredient is tossed backwards off screen. It disappears either way.
    /// </summary>
    public class CrushIngredientView : MonoBehaviour
    {
        [SerializeField] CrushingGame game;

        [Header("Places")]
        [SerializeField, Tooltip("Where the ingredient sits while it is crushed. Its bottom rests on this point.")] Transform crushSpot;
        [SerializeField, Tooltip("Where a successful ingredient lands and disappears.")] Transform bowl;

        [Header("Look")]
        [SerializeField, Tooltip("Used for rounds with no ingredient of their own.")] Sprite placeholderIngredient;
        [SerializeField, Tooltip("Gray powder, tinted with each ingredient's powder color.")] Sprite powder;
        [SerializeField, Tooltip("Width of the ingredient in world units, before any flattening.")] float size = 1.1f;
        [SerializeField] int sortingOrder = 4;
        [SerializeField, Tooltip("Beats before the response starts that the ingredient appears.")] float appearLeadBeats = 0.5f;
        [SerializeField, Tooltip("Seconds the ingredient takes to pop in.")] float appearSeconds = 0.15f;

        [Header("Flattening")]
        [SerializeField, Range(0f, 0.5f), Tooltip("Height lost on each hit, as a fraction of the original.")] float flattenPerHit = 0.12f;
        [SerializeField, Range(0f, 0.5f), Tooltip("Width gained on each hit, as a fraction of the original.")] float widenPerHit = 0.06f;
        [SerializeField, Range(0.1f, 1f), Tooltip("It never gets flatter than this fraction of its height.")] float minimumHeight = 0.35f;
        [SerializeField, Tooltip("Extra squash right as a hit lands, which springs back.")] float squishAmount = 0.25f;
        [SerializeField] float squishSeconds = 0.15f;

        [Header("Success and failure")]
        [SerializeField, Tooltip("Seconds the powder rests after the last hit before it is thrown into the bowl.")] float powderHoldSeconds = 0.3f;
        [SerializeField] float throwSeconds = 0.55f;
        [SerializeField, Tooltip("How high the throw into the bowl arcs, in world units.")] float bowlArcHeight = 2.5f;
        [SerializeField, Tooltip("Where a failed ingredient flies to, relative to the crush spot. X is flipped to point away from the bowl.")]
        Vector2 failThrow = new Vector2(12f, 3f);
        [SerializeField] float failArcHeight = 3f;
        [SerializeField, Tooltip("Degrees a failed ingredient spins on its way off screen.")] float failSpin = 720f;

        class Piece
        {
            public int round;
            public Ingredient ingredient;
            public Transform root;
            public SpriteRenderer renderer;
            public int hits;
            public float spawnTime;
            public float squishTime = float.NegativeInfinity;
            public float bowlThrowTime = float.PositiveInfinity;
            public bool flying;
            public bool intoBowl;
            public float throwTime;
            public Vector3 from;
            public Vector3 to;
            public Vector3 throwScale;
        }

        readonly List<Piece> pieces = new List<Piece>();
        int lastSpawnedRound = -1;

        RhythmSession Session => game.Session;

        void OnEnable()
        {
            game.ChartStarted.AddListener(Clear);
            game.NoteJudged += OnNoteJudged;
            game.RoundFinished += OnRoundFinished;
        }

        void OnDisable()
        {
            game.ChartStarted.RemoveListener(Clear);
            game.NoteJudged -= OnNoteJudged;
            game.RoundFinished -= OnRoundFinished;
        }

        void Clear()
        {
            foreach (var piece in pieces) Destroy(piece.root.gameObject);
            pieces.Clear();
            lastSpawnedRound = -1;
        }

        void Update()
        {
            if (Session.IsRunning && game.Chart != null) SpawnIfDue(Session.Conductor.SongBeat);

            for (int i = pieces.Count - 1; i >= 0; i--)
            {
                var piece = pieces[i];
                if (!piece.flying && Time.time >= piece.bowlThrowTime) Throw(piece, true);
                if (piece.flying ? Fly(piece) : Sit(piece)) continue;
                Destroy(piece.root.gameObject);
                pieces.RemoveAt(i);
            }
        }

        void SpawnIfDue(double beat)
        {
            int round = game.Chart.RoundAt(beat + appearLeadBeats);
            if (round <= lastSpawnedRound) return;
            if (beat + appearLeadBeats < game.Chart.layout.ResponseStart(round)) return;

            lastSpawnedRound = round;
            var ingredient = game.Chart.rounds[round].ingredient;
            var sprite = ingredient != null && ingredient.sprite != null ? ingredient.sprite : placeholderIngredient;
            if (sprite == null) return;

            var root = new GameObject($"Ingredient {round + 1}").transform;
            root.SetParent(transform, false);
            root.position = crushSpot.position;

            var renderer = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(root, false);
            renderer.sortingOrder = sortingOrder;
            SetSprite(renderer, sprite, Color.white);

            pieces.Add(new Piece
            {
                round = round,
                ingredient = ingredient,
                root = root,
                renderer = renderer,
                spawnTime = Time.time,
            });
        }

        void TurnToPowder(Piece piece)
        {
            if (powder == null) return;
            SetSprite(piece.renderer, powder, piece.ingredient != null ? piece.ingredient.powderColor : Color.white);
            piece.squishTime = Time.time;
        }

        /// <summary>
        /// Shows a sprite at the ingredient's width, offset so the root marks its bottom center. That keeps it
        /// resting on the counter as it flattens, and keeps the powder where the ingredient was.
        /// </summary>
        void SetSprite(SpriteRenderer renderer, Sprite sprite, Color color)
        {
            renderer.sprite = sprite;
            renderer.color = color;
            var bounds = sprite.bounds;
            float scale = size / Mathf.Max(0.0001f, bounds.size.x);
            renderer.transform.localScale = Vector3.one * scale;
            renderer.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, 0f) * scale;
        }

        void OnNoteJudged(JudgeResult result)
        {
            var piece = Sitting(result.Event.Group);
            if (piece == null) return;

            if (result.Judgement == Judgement.Miss) return;
            piece.hits++;
            piece.squishTime = Time.time;
        }

        void OnRoundFinished(int round, bool crushed)
        {
            var piece = Sitting(round);
            if (piece == null) return;

            if (!crushed)
            {
                Throw(piece, false);
                return;
            }
            // This is the round's last note: grind it to powder, let that register, then throw it in the bowl.
            TurnToPowder(piece);
            piece.bowlThrowTime = Time.time + powderHoldSeconds;
        }

        Piece Sitting(int round)
        {
            foreach (var piece in pieces)
                if (!piece.flying && piece.round == round) return piece;
            return null;
        }

        void Throw(Piece piece, bool intoBowl)
        {
            piece.flying = true;
            piece.intoBowl = intoBowl;
            piece.throwTime = Time.time;
            piece.from = piece.root.position;
            piece.throwScale = piece.root.localScale;
            if (intoBowl)
            {
                piece.to = bowl.position;
            }
            else
            {
                float away = bowl != null && bowl.position.x > piece.from.x ? -1f : 1f;
                piece.to = piece.from + new Vector3(failThrow.x * away, failThrow.y, 0f);
            }
        }

        /// <summary>Squashes the sitting ingredient by its hits so far. Always keeps it.</summary>
        bool Sit(Piece piece)
        {
            float appear = appearSeconds > 0f ? Mathf.Clamp01((Time.time - piece.spawnTime) / appearSeconds) : 1f;
            float squish = 0f;
            if (squishSeconds > 0f)
            {
                float since = (Time.time - piece.squishTime) / squishSeconds;
                if (since >= 0f && since < 1f) squish = squishAmount * (1f - since) * (1f - since);
            }

            float height = Mathf.Max(minimumHeight, 1f - flattenPerHit * piece.hits) * (1f - squish);
            float width = (1f + widenPerHit * piece.hits) * (1f + squish * 0.6f);
            piece.root.localScale = new Vector3(width, height, 1f) * EaseOutBack(appear);
            return true;
        }

        /// <summary>Moves a thrown ingredient along its arc. Returns false once it has landed or left.</summary>
        bool Fly(Piece piece)
        {
            float t = throwSeconds > 0f ? (Time.time - piece.throwTime) / throwSeconds : 1f;
            if (t >= 1f) return false;

            float arc = piece.intoBowl ? bowlArcHeight : failArcHeight;
            piece.root.position = Vector3.Lerp(piece.from, piece.to, t) + Vector3.up * (arc * 4f * t * (1f - t));

            if (piece.intoBowl)
            {
                // Keeps its flattened shape, then shrinks away over the last quarter as it drops into the bowl.
                piece.root.localScale = piece.throwScale * Mathf.Clamp01((1f - t) / 0.25f);
            }
            else
            {
                float away = piece.to.x < piece.from.x ? 1f : -1f;
                piece.root.rotation = Quaternion.Euler(0f, 0f, failSpin * t * away);
            }
            return true;
        }

        static float EaseOutBack(float t)
        {
            const float overshoot = 1.7f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }
    }
}
