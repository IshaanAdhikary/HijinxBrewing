using System.Collections.Generic;
using UnityEngine;

namespace Brewing.Mixing
{
    /// <summary>
    /// Bubbles on the surface of the cauldron. The closer the brew is to boiling over, the more bubbles there
    /// are. Each one swells in at a random spot, wobbles, and pops.
    ///
    /// To use the artist's sprites, drop them into <see cref="bubbleSprites"/>. Each bubble picks one at
    /// random, so a few different drawings give variety. Nothing else needs to change. Put this object at the
    /// center of the cauldron.
    /// </summary>
    public class CauldronBubbles : MonoBehaviour
    {
        [SerializeField] MixingGame game;

        [Header("Sprites")]
        [SerializeField, Tooltip("Placeholder for now. Add or swap in hand-drawn bubble sprites here.")] Sprite[] bubbleSprites;
        [SerializeField, Tooltip("Multiplied with the sprite color. Use white to show the sprites as drawn.")] Color tint = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] int sortingOrder = 3;

        [Header("Amount")]
        [SerializeField] int maxBubbles = 40;
        [SerializeField, Range(0f, 1f), Tooltip("Failure level below which there are no bubbles.")] float startDanger = 0.05f;
        [SerializeField, Tooltip("1 is linear. Above 1 the count stays low for longer and ramps up late.")] float amountPower = 1f;
        [SerializeField, Tooltip("Limits how fast new bubbles appear, so a sudden jump in danger fills in over a moment.")] float maxSpawnsPerSecond = 30f;

        [Header("Look")]
        [SerializeField, Tooltip("Radius of the area bubbles appear in, in world units.")] float areaRadius = 2.4f;
        [SerializeField] Vector2 sizeRange = new Vector2(0.25f, 0.6f);
        [SerializeField, Tooltip("Seconds a bubble lasts before popping.")] Vector2 lifetimeRange = new Vector2(1.2f, 2.4f);
        [SerializeField] float wobbleAmount = 0.06f;
        [SerializeField] float wobbleSpeed = 3f;

        class Bubble
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 basePosition;
            public float size;
            public float age;
            public float lifetime;
            public float phase;
        }

        readonly List<Bubble> active = new List<Bubble>();
        readonly Stack<Bubble> pool = new Stack<Bubble>();
        float spawnBudget;

        /// <summary>Number of bubbles currently on screen.</summary>
        public int ActiveCount => active.Count;

        void Awake()
        {
            if (game == null) game = FindAnyObjectByType<MixingGame>();
        }

        void Update()
        {
            float danger = game != null ? game.FailureLevel : 0f;
            int target = BubbleMath.TargetCount(danger, startDanger, maxBubbles, amountPower);

            spawnBudget = Mathf.Min(spawnBudget + maxSpawnsPerSecond * Time.deltaTime, 3f);
            while (active.Count < target && spawnBudget >= 1f)
            {
                Spawn();
                spawnBudget -= 1f;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var bubble = active[i];
                bubble.age += Time.deltaTime;
                float life = bubble.age / bubble.lifetime;
                if (life >= 1f)
                {
                    Recycle(i);
                    continue;
                }

                BubbleMath.Evaluate(life, out float scale, out float alpha);
                float wobble = Time.time * wobbleSpeed + bubble.phase;
                bubble.transform.localPosition = bubble.basePosition + new Vector2(Mathf.Sin(wobble), Mathf.Cos(wobble * 1.3f)) * wobbleAmount;
                bubble.transform.localScale = Vector3.one * (bubble.size * scale);

                Color color = tint;
                color.a *= alpha;
                bubble.renderer.color = color;
            }
        }

        void Spawn()
        {
            if (bubbleSprites == null || bubbleSprites.Length == 0) return;

            var bubble = pool.Count > 0 ? pool.Pop() : CreateBubble();
            bubble.renderer.sprite = bubbleSprites[Random.Range(0, bubbleSprites.Length)];
            bubble.basePosition = Random.insideUnitCircle * areaRadius;
            bubble.size = Random.Range(sizeRange.x, sizeRange.y);
            bubble.lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
            bubble.phase = Random.value * Mathf.PI * 2f;
            bubble.age = 0f;
            bubble.transform.localScale = Vector3.zero;
            bubble.transform.gameObject.SetActive(true);
            active.Add(bubble);
        }

        Bubble CreateBubble()
        {
            var go = new GameObject("Bubble");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return new Bubble { transform = go.transform, renderer = renderer };
        }

        void Recycle(int index)
        {
            var bubble = active[index];
            active.RemoveAt(index);
            bubble.transform.gameObject.SetActive(false);
            pool.Push(bubble);
        }
    }
}
