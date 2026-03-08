using RuntimeRoguelike.Dots.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeRoguelike.Dots.Hybrid
{
    /// <summary>
    /// UI-оверлей миникарты: Runtime Texture2D (пиксель = клетка карты).
    /// Читает NativeArray<FogState> из FogOfWarSystem + MapBlob + GridPosition сущностей.
    /// Toggle через InputBridge → MinimapToggleTag.
    /// </summary>
    public class MinimapBridge : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RawImage _minimapImage;

        [Header("Цвета")]
        [SerializeField] private Color _unexploredColor = new(0f, 0f, 0f, 0f);
        [SerializeField] private Color _exploredColor = new(0.6f, 0.55f, 0.45f, 1f);
        [SerializeField] private Color _visibleColor = new(0.85f, 0.8f, 0.7f, 1f);
        [SerializeField] private Color _wallColor = new(0.3f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color _playerMarker = new(0f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color _enemyMarker = new(0.9f, 0.1f, 0.1f, 1f);

        private World _world;
        private EntityManager _entityManager;
        private EntityQuery _mapQuery;
        private EntityQuery _enemyQuery;
        private Texture2D _texture;
        private Color[] _pixels;
        private int2 _textureSize;

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

            if (_minimapImage != null)
            {
                _minimapImage.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (_world is not { IsCreated: true })
                return;

            if (_mapQuery.IsEmpty)
                return;

            var mapEntity = _mapQuery.GetSingletonEntity();

            // Проверяем toggle состояние
            if (!_entityManager.HasComponent<MinimapToggleTag>(mapEntity))
                return;

            var isEnabled = _entityManager.IsComponentEnabled<MinimapToggleTag>(mapEntity);

            if (_minimapImage != null)
            {
                _minimapImage.gameObject.SetActive(isEnabled);
            }

            if (!isEnabled)
                return;

            // Проверяем триггер обновления (FogRenderRequest)
            if (!_entityManager.HasComponent<FogRenderRequest>(mapEntity))
                return;

            UpdateMinimap(mapEntity);
        }

        private void UpdateMinimap(Entity mapEntity)
        {
            var mapRef = _entityManager.GetComponentData<MapBlobReference>(mapEntity);
            if (!mapRef.Value.IsCreated)
                return;

            ref var map = ref mapRef.Value.Value;

            // Получаем FogOfWarSystem
            var fogSystemHandle = _world.Unmanaged.GetExistingUnmanagedSystem<FogOfWarSystem>();
            if (fogSystemHandle == SystemHandle.Null)
                return;

            ref var fogSystem = ref _world.Unmanaged.GetUnsafeSystemRef<FogOfWarSystem>(fogSystemHandle);
            if (!fogSystem.IsAllocated)
                return;

            var fogState = fogSystem.GetFogState();
            var size = map.Size;

            EnsureTexture(size);
            RenderMap(ref map, fogState, size);
            RenderMarkers(fogState, size);

            _texture.SetPixels(_pixels);
            _texture.Apply();

            if (_minimapImage != null && _minimapImage.texture != _texture)
            {
                _minimapImage.texture = _texture;
            }
        }

        private void EnsureTexture(int2 size)
        {
            if (_texture != null && math.all(_textureSize == size))
                return;

            if (_texture != null)
            {
                Destroy(_texture);
            }

            _texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _textureSize = size;
            _pixels = new Color[size.x * size.y];
        }

        private void RenderMap(ref MapBlob map, NativeArray<FogState> fogState, int2 size)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var index = y * size.x + x;
                    var state = fogState[index];

                    if (state == FogState.Unexplored)
                    {
                        _pixels[index] = _unexploredColor;
                        continue;
                    }

                    var isWall = map.BaseLayer[index] == MapCellType.Wall;
                    if (isWall)
                    {
                        _pixels[index] = state == FogState.Visible ? _wallColor : _wallColor * 0.6f;
                    }
                    else
                    {
                        _pixels[index] = state == FogState.Visible ? _visibleColor : _exploredColor;
                    }
                }
            }
        }

        private void RenderMarkers(NativeArray<FogState> fogState, int2 size)
        {
            // Маркер игрока (всегда видимый)
            var playerQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<GridPosition>());

            if (!playerQuery.IsEmpty)
            {
                using var playerPositions = playerQuery.ToComponentDataArray<GridPosition>(Allocator.Temp);
                if (playerPositions.Length > 0)
                {
                    var cell = playerPositions[0].Value;
                    if (cell.x >= 0 && cell.x < size.x && cell.y >= 0 && cell.y < size.y)
                    {
                        _pixels[cell.y * size.x + cell.x] = _playerMarker;
                    }
                }
            }

            // Маркеры врагов (только Visible)
            if (!_enemyQuery.IsEmpty)
            {
                using var enemyPositions = _enemyQuery.ToComponentDataArray<GridPosition>(Allocator.Temp);
                for (var i = 0; i < enemyPositions.Length; i++)
                {
                    var cell = enemyPositions[i].Value;
                    if (cell.x < 0 || cell.x >= size.x || cell.y < 0 || cell.y >= size.y)
                        continue;

                    var index = cell.y * size.x + cell.x;
                    if (fogState[index] == FogState.Visible)
                    {
                        _pixels[index] = _enemyMarker;
                    }
                }
            }
        }
    }
}
