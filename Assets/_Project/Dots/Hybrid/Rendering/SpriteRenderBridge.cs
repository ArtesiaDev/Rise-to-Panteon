using System.Collections.Generic;
using RuntimeRoguelike.Dots;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class SpriteRenderBridge : MonoBehaviour
    {
        [SerializeField] private Transform entitiesRoot;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private float spriteScale = 1f;
        [SerializeField] private Color playerColor = new Color(0.9f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color enemyColor = new Color(0.85f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color lootColor = new Color(0.2f, 0.8f, 0.2f, 1f);

        private EntityManager _entityManager;
        private EntityQuery _query;
        private EntityQuery _gridQuery;
        private readonly Dictionary<Entity, SpriteRenderer> _views = new Dictionary<Entity, SpriteRenderer>();
        private readonly Dictionary<DotsSpriteKey, Stack<SpriteRenderer>> _pool = new Dictionary<DotsSpriteKey, Stack<SpriteRenderer>>();
        private readonly Dictionary<DotsSpriteKey, Sprite> _spriteCache = new Dictionary<DotsSpriteKey, Sprite>();
        private readonly HashSet<Entity> _alive = new HashSet<Entity>();
        private readonly List<Entity> _toRemove = new List<Entity>();

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = world.EntityManager;
            _query = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SpriteKeyComponent>(),
                ComponentType.ReadOnly<RenderPosition>());
            _gridQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfigData>());

            if (entitiesRoot == null)
            {
                entitiesRoot = transform;
            }

            if (poolRoot == null)
            {
                poolRoot = transform;
            }
        }

        private void OnDestroy()
        {
            if (_query.IsCreated)
            {
                _query.Dispose();
            }

            if (_gridQuery.IsCreated)
            {
                _gridQuery.Dispose();
            }
        }

        private void Update()
        {
            if (!_query.IsCreated)
            {
                return;
            }

            var cellSize = 1f;
            if (_gridQuery.TryGetSingleton<GridConfigData>(out var gridConfig))
            {
                cellSize = Mathf.Max(0.01f, gridConfig.CellSize);
            }
            
            using var entities = _query.ToEntityArray(Allocator.Temp);
            using var keys = _query.ToComponentDataArray<SpriteKeyComponent>(Allocator.Temp);
            using var positions = _query.ToComponentDataArray<RenderPosition>(Allocator.Temp);

            _alive.Clear();
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                _alive.Add(entity);

                if (!_views.TryGetValue(entity, out var renderer))
                {
                    renderer = GetView(keys[i].Value);
                    _views[entity] = renderer;
                }

                var position = positions[i].Value;
                renderer.transform.position = new Vector3(
                    (position.x + 0.5f) * cellSize,
                    (position.y + 0.5f) * cellSize,
                    0f);
            }

            _toRemove.Clear();
            foreach (var pair in _views)
            {
                if (!_alive.Contains(pair.Key))
                {
                    _toRemove.Add(pair.Key);
                }
            }

            for (var i = 0; i < _toRemove.Count; i++)
            {
                var entity = _toRemove[i];
                if (_views.TryGetValue(entity, out var renderer))
                {
                    ReleaseView(renderer);
                }

                _views.Remove(entity);
            }
        }

        private SpriteRenderer GetView(DotsSpriteKey key)
        {
            if (!_pool.TryGetValue(key, out var stack))
            {
                stack = new Stack<SpriteRenderer>();
                _pool[key] = stack;
            }

            if (stack.Count > 0)
            {
                var renderer = stack.Pop();
                renderer.gameObject.SetActive(true);
                renderer.transform.SetParent(entitiesRoot, false);
                return renderer;
            }

            var go = new GameObject($"DotsView_{key}");
            go.transform.SetParent(entitiesRoot, false);
            go.transform.localScale = Vector3.one * spriteScale;
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetSprite(key);
            return spriteRenderer;
        }

        private void ReleaseView(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            var key = GetKeyForColor(renderer.sprite);
            if (!_pool.TryGetValue(key, out var stack))
            {
                stack = new Stack<SpriteRenderer>();
                _pool[key] = stack;
            }

            renderer.gameObject.SetActive(false);
            renderer.transform.SetParent(poolRoot, false);
            stack.Push(renderer);
        }

        private Sprite GetSprite(DotsSpriteKey key)
        {
            if (_spriteCache.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            var color = key switch
            {
                DotsSpriteKey.Player => playerColor,
                DotsSpriteKey.Enemy => enemyColor,
                _ => lootColor
            };

            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, color);
            texture.Apply();

            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _spriteCache[key] = sprite;
            return sprite;
        }

        private DotsSpriteKey GetKeyForColor(Sprite sprite)
        {
            foreach (var pair in _spriteCache)
            {
                if (pair.Value == sprite)
                {
                    return pair.Key;
                }
            }

            return DotsSpriteKey.Loot;
        }
    }
}
