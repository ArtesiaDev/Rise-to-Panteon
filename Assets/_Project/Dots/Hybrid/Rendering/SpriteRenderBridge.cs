using System.Collections.Generic;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class SpriteRenderBridge : MonoBehaviour
    {
        [SerializeField] private Transform _entitiesRoot;
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private float _spriteScale = 1f;
        [SerializeField] private Color _playerColor = new(0.9f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color _enemyColor = new(0.85f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _lootColor = new(0.2f, 0.8f, 0.2f, 1f);

        private EntityManager _entityManager;
        private EntityQuery _query;
        private EntityQuery _gridQuery;
        private World _world;
        
        private readonly Dictionary<Entity, SpriteRenderer> _views = new();
        private readonly Dictionary<DotsSpriteKey, Stack<SpriteRenderer>> _pool = new();
        private readonly Dictionary<DotsSpriteKey, Sprite> _spriteCache = new();
        private readonly HashSet<Entity> _alive = new();
        private readonly List<Entity> _toRemove = new();

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = _world.EntityManager;
            _query = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SpriteKeyComponent>(),
                ComponentType.ReadOnly<RenderPosition>());
            _gridQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfigData>());

            if (_entitiesRoot == null)
            {
                _entitiesRoot = transform;
            }

            if (_poolRoot == null)
            {
                _poolRoot = transform;
            }
        }

        private void Update()
        {
            if (_world is not { IsCreated: true })
                return;

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

            foreach (var entity in _toRemove)
            {
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
                renderer.transform.SetParent(_entitiesRoot, false);
                return renderer;
            }

            var go = new GameObject($"DotsView_{key}");
            go.transform.SetParent(_entitiesRoot, false);
            go.transform.localScale = Vector3.one * _spriteScale;
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
            renderer.transform.SetParent(_poolRoot, false);
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
                DotsSpriteKey.Player => _playerColor,
                DotsSpriteKey.Enemy => _enemyColor,
                _ => _lootColor
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
