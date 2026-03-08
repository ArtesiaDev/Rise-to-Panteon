using RuntimeRoguelike.Dots.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    /// <summary>
    /// Мост тумана войны: управляет видимостью через единственную Texture2D + SpriteRenderer.
    /// Каждый пиксель текстуры = одна клетка карты.
    /// На основе NativeArray<FogState> из FogOfWarSystem.
    /// </summary>
    public class FogRenderBridge : MonoBehaviour
    {
        [Header("Цвета тумана")]
        [SerializeField] private Color _unexploredColor = new(0f, 0f, 0f, 1f);
        [SerializeField] private Color _exploredColor = new(0f, 0f, 0f, 0.7f);

        [Header("Ссылки")]
        [SerializeField] private TilemapRenderBridge _tilemapBridge;

        private World _world;
        private EntityManager _entityManager;
        private EntityQuery _mapQuery;
        private EntityQuery _enemyQuery;

        // Texture2D подход — один спрайт вместо 3500 тайлов
        private Texture2D _fogTexture;
        private SpriteRenderer _fogRenderer;
        private Color[] _pixels;
        private int2 _textureSize;
        private bool _fogInitialized;

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = _world.EntityManager;
            _mapQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapBlobReference>());
            _enemyQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<GridPosition>());
        }

        private void LateUpdate()
        {
            if (_world is not { IsCreated: true })
                return;

            if (_mapQuery.IsEmpty)
                return;

            var mapEntity = _mapQuery.GetSingletonEntity();

            // Проверяем триггер обновления
            if (!_entityManager.HasComponent<FogRenderRequest>(mapEntity) ||
                !_entityManager.IsComponentEnabled<FogRenderRequest>(mapEntity))
            {
                return;
            }

            UpdateFog(mapEntity);
            _entityManager.SetComponentEnabled<FogRenderRequest>(mapEntity, false);
        }

        private void UpdateFog(Entity mapEntity)
        {
            var mapRef = _entityManager.GetComponentData<MapBlobReference>(mapEntity);
            if (!mapRef.Value.IsCreated)
                return;

            ref var map = ref mapRef.Value.Value;

            // Получаем FogOfWarSystem через SystemHandle
            var fogSystemHandle = _world.Unmanaged.GetExistingUnmanagedSystem<FogOfWarSystem>();
            if (fogSystemHandle == SystemHandle.Null)
                return;

            ref var fogSystem = ref _world.Unmanaged.GetUnsafeSystemRef<FogOfWarSystem>(fogSystemHandle);
            if (!fogSystem.IsAllocated)
                return;

            var fogState = fogSystem.GetFogState();
            var size = map.Size;

            EnsureFogSprite(size);
            UpdateFogTexture(size, fogState);
            UpdateEnemyVisibility(fogState, size);
        }

        private void EnsureFogSprite(int2 size)
        {
            if (_fogInitialized && math.all(_textureSize == size))
                return;

            // Ищем Grid из TilemapRenderBridge для позиционирования
            var gridParent = _tilemapBridge != null && _tilemapBridge.GroundTilemap != null
                ? _tilemapBridge.GroundTilemap.transform.parent
                : null;

            if (gridParent == null)
                return;

            // Создаём текстуру: 1 пиксель = 1 клетка
            _fogTexture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _pixels = new Color[size.x * size.y];
            _textureSize = size;

            // Инициализируем всё чёрным (Unexplored)
            for (var i = 0; i < _pixels.Length; i++)
                _pixels[i] = _unexploredColor;
            _fogTexture.SetPixels(_pixels);
            _fogTexture.Apply();

            // Создаём GameObject со SpriteRenderer
            if (_fogRenderer == null)
            {
                var fogObject = new GameObject("FogOverlay");
                fogObject.transform.SetParent(gridParent, false);
                _fogRenderer = fogObject.AddComponent<SpriteRenderer>();
                _fogRenderer.sortingOrder = 10;
            }

            // Спрайт: PPU = 1, pivot в нижнем-левом углу (0,0) для выравнивания с Grid
            _fogRenderer.sprite = Sprite.Create(
                _fogTexture,
                new Rect(0, 0, size.x, size.y),
                Vector2.zero, // pivot в (0,0) — совпадает с Grid origin
                1f); // PPU=1: 1 пиксель = 1 unit = 1 клетка

            _fogInitialized = true;
        }

        private void UpdateFogTexture(int2 size, NativeArray<FogState> fogState)
        {
            if (_fogTexture == null)
                return;

            for (var i = 0; i < fogState.Length; i++)
            {
                _pixels[i] = fogState[i] switch
                {
                    FogState.Visible => Color.clear,
                    FogState.Explored => _exploredColor,
                    _ => _unexploredColor
                };
            }

            _fogTexture.SetPixels(_pixels);
            _fogTexture.Apply();
        }

        /// <summary>
        /// Скрывает SpriteRenderer врагов, находящихся вне видимости (Visible).
        /// </summary>
        private void UpdateEnemyVisibility(NativeArray<FogState> fogState, int2 size)
        {
            if (_enemyQuery.IsEmpty)
                return;

            using var entities = _enemyQuery.ToEntityArray(Allocator.Temp);
            using var positions = _enemyQuery.ToComponentDataArray<GridPosition>(Allocator.Temp);

            var spriteBridge = SpriteRenderBridge.Instance;
            if (spriteBridge == null)
                return;

            for (var i = 0; i < entities.Length; i++)
            {
                var cell = positions[i].Value;
                if (cell.x < 0 || cell.x >= size.x || cell.y < 0 || cell.y >= size.y)
                    continue;

                var index = cell.y * size.x + cell.x;
                var isVisible = fogState[index] == FogState.Visible;
                spriteBridge.SetEntityVisibility(entities[i], isVisible);
            }
        }
    }
}
