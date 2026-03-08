using System.Collections.Generic;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class TilemapRenderBridge : MonoBehaviour
    {
        [SerializeField] private Transform _worldRoot;

        [Header("Тайлы стен (16 штук по 4-bit bitmask: N=1,E=2,S=4,W=8)")]
        [SerializeField] private Tile[] _wallTilesByMask = new Tile[16];

        [Header("Тайлы пола (вариации)")]
        [SerializeField] private Tile[] _floorVariantTiles;

        [Header("Декоративные тайлы (дверные проёмы, арки)")]
        [SerializeField] private Tile _doorHorizontalTile;
        [SerializeField] private Tile _doorVerticalTile;

        [Header("Тайлы ловушек")]
        [SerializeField] private Tile _poisonTile;
        [SerializeField] private Tile _spikeTile;
        [SerializeField] private Tile _obstacleTile;

        [Header("Fallback-цвета (если тайлы не назначены)")]
        [SerializeField] private Color _floorColor = new(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color _wallColor = new(0.12f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color _obstacleColor = new(0.18f, 0.18f, 0.18f, 1f);
        [SerializeField] private Color _poisonColor = new(0.2f, 0.5f, 0.2f, 1f);
        [SerializeField] private Color _spikeColor = new(0.6f, 0.2f, 0.2f, 1f);

        private World _world;
        private EntityManager _entityManager;
        private EntityQuery _mapQuery;
        private EntityQuery _gridQuery;
        private Entity _mapEntity;
        private GameObject _gridRoot;
        private readonly Dictionary<string, Tile> _fallbackTileCache = new();
        private int _lastRunId = -1;

        // Публичные ссылки на tilemaps для FogRenderBridge
        public Tilemap GroundTilemap { get; private set; }
        public Tilemap WallsTilemap { get; private set; }
        public Tilemap HazardsTilemap { get; private set; }
        public Tilemap DecorTilemap { get; private set; }

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = _world.EntityManager;
            _mapQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MapBlobReference>());
            _gridQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfigData>());
        }

        private void Update()
        {
            if (_world is not { IsCreated: true })
                return;

            if (_mapQuery.IsEmpty)
                return;

            _mapEntity = _mapQuery.GetSingletonEntity();
            if (!_entityManager.HasComponent<MapRenderRequest>(_mapEntity) ||
                !_entityManager.IsComponentEnabled<MapRenderRequest>(_mapEntity))
            {
                return;
            }

            var request = _entityManager.GetComponentData<MapRenderRequest>(_mapEntity);
            if (request.RunId == _lastRunId)
            {
                _entityManager.SetComponentEnabled<MapRenderRequest>(_mapEntity, false);
                return;
            }

            var mapRef = _entityManager.GetComponentData<MapBlobReference>(_mapEntity);
            if (!mapRef.Value.IsCreated)
            {
                return;
            }

            var gridSize = 1f;
            if (_gridQuery.TryGetSingleton<GridConfigData>(out var gridConfig))
            {
                gridSize = Mathf.Max(0.01f, gridConfig.CellSize);
            }

            Render(ref mapRef.Value.Value, gridSize);
            _lastRunId = request.RunId;
            _entityManager.SetComponentEnabled<MapRenderRequest>(_mapEntity, false);
        }

        private void Render(ref MapBlob map, float cellSize)
        {
            if (_gridRoot != null)
            {
                Destroy(_gridRoot);
            }

            var parent = _worldRoot != null ? _worldRoot : transform;
            _gridRoot = new GameObject("DotsGrid");
            _gridRoot.transform.SetParent(parent, false);

            var grid = _gridRoot.AddComponent<Grid>();
            grid.cellSize = Vector3.one * cellSize;

            var ground = CreateTilemap(_gridRoot.transform, "Ground", 0, false);
            var walls = CreateTilemap(_gridRoot.transform, "Walls", 1, true);
            var hazards = CreateTilemap(_gridRoot.transform, "Hazards", 2, false);
            var decor = CreateTilemap(_gridRoot.transform, "Decor", 3, false);

            GroundTilemap = ground;
            WallsTilemap = walls;
            HazardsTilemap = hazards;
            DecorTilemap = decor;

            var size = map.Size;
            var cellCount = size.x * size.y;
            var bounds = new BoundsInt(0, 0, 0, size.x, size.y, 1);

            // Массовая установка тайлов через SetTilesBlock — намного быстрее поштучного SetTile
            var groundTiles = new TileBase[cellCount];
            var wallTiles = new TileBase[cellCount];
            var hazardTiles = new TileBase[cellCount];

            // Fallback-тайлы если спрайты не назначены
            var fallbackFloor = GetFallbackTile("floor", _floorColor);
            var fallbackWall = GetFallbackTile("wall", _wallColor);
            var fallbackObstacle = GetFallbackTile("obstacle", _obstacleColor);
            var fallbackPoison = GetFallbackTile("poison", _poisonColor);
            var fallbackSpike = GetFallbackTile("spike", _spikeColor);

            var hasWallTiles = _wallTilesByMask != null && _wallTilesByMask.Length == 16;
            var hasFloorTiles = _floorVariantTiles != null && _floorVariantTiles.Length > 0;

            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var index = y * size.x + x;

                    if (map.BaseLayer[index] == MapCellType.Floor)
                    {
                        if (hasFloorTiles)
                        {
                            var variant = map.FloorVariantLayer[index] % _floorVariantTiles.Length;
                            groundTiles[index] = _floorVariantTiles[variant] != null
                                ? _floorVariantTiles[variant]
                                : fallbackFloor;
                        }
                        else
                        {
                            groundTiles[index] = fallbackFloor;
                        }
                    }
                    else if (map.BaseLayer[index] == MapCellType.Wall)
                    {
                        if (hasWallTiles)
                        {
                            var mask = map.WallMaskLayer[index] & 0x0F; // 4-bit маска
                            wallTiles[index] = _wallTilesByMask[mask] != null
                                ? _wallTilesByMask[mask]
                                : fallbackWall;
                        }
                        else
                        {
                            wallTiles[index] = fallbackWall;
                        }
                    }

                    if (map.ObstacleLayer[index] != ObstacleType.None)
                    {
                        wallTiles[index] = _obstacleTile != null ? _obstacleTile : fallbackObstacle;
                    }

                    var hazard = map.HazardLayer[index];
                    if (hazard == HazardType.Poison)
                    {
                        hazardTiles[index] = _poisonTile != null ? _poisonTile : fallbackPoison;
                    }
                    else if (hazard == HazardType.Spike)
                    {
                        hazardTiles[index] = _spikeTile != null ? _spikeTile : fallbackSpike;
                    }
                }
            }

            // Декоративный слой: дверные проёмы на переходах комната↔коридор
            var decorTiles = new TileBase[cellCount];
            PlaceDoorDecor(ref map, decorTiles, size);

            ground.SetTilesBlock(bounds, groundTiles);
            walls.SetTilesBlock(bounds, wallTiles);
            hazards.SetTilesBlock(bounds, hazardTiles);
            decor.SetTilesBlock(bounds, decorTiles);

            ground.CompressBounds();
            walls.CompressBounds();
            hazards.CompressBounds();
            decor.CompressBounds();
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, bool withColliders)
        {
            var tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(parent, false);

            var tilemap = tilemapObject.AddComponent<Tilemap>();
            var renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;

            if (withColliders)
            {
                var body = tilemapObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                var collider = tilemapObject.AddComponent<TilemapCollider2D>();
                tilemapObject.AddComponent<CompositeCollider2D>();
                collider.usedByComposite = true;
            }

            return tilemap;
        }

        /// <summary>
        /// Размещает декоративные тайлы (двери/арки) на переходах комната↔коридор.
        /// Дверь ставится на floor-клетке, у которой ровно 2 стены-соседа по одной оси
        /// (т.е. клетка зажата стенами сверху-снизу или слева-справа — дверной проём).
        /// </summary>
        private void PlaceDoorDecor(ref MapBlob map, TileBase[] decorTiles, Unity.Mathematics.int2 size)
        {
            var hasDoorH = _doorHorizontalTile != null;
            var hasDoorV = _doorVerticalTile != null;
            if (!hasDoorH && !hasDoorV)
                return;

            // Ищем клетки на границах комнат: floor-клетка, у которой есть стены
            // по одной оси (горизонтальная или вертикальная «щель» — дверной проём)
            for (var y = 1; y < size.y - 1; y++)
            {
                for (var x = 1; x < size.x - 1; x++)
                {
                    var index = y * size.x + x;
                    if (map.BaseLayer[index] != MapCellType.Floor)
                        continue;

                    // Соседи
                    var north = map.BaseLayer[(y + 1) * size.x + x];
                    var south = map.BaseLayer[(y - 1) * size.x + x];
                    var east = map.BaseLayer[y * size.x + (x + 1)];
                    var west = map.BaseLayer[y * size.x + (x - 1)];

                    // Горизонтальный проём: стены сверху и снизу, полы слева и справа
                    if (north == MapCellType.Wall && south == MapCellType.Wall
                        && east == MapCellType.Floor && west == MapCellType.Floor)
                    {
                        if (hasDoorH)
                            decorTiles[index] = _doorHorizontalTile;
                    }
                    // Вертикальный проём: стены слева и справа, полы сверху и снизу
                    else if (east == MapCellType.Wall && west == MapCellType.Wall
                             && north == MapCellType.Floor && south == MapCellType.Floor)
                    {
                        if (hasDoorV)
                            decorTiles[index] = _doorVerticalTile;
                    }
                }
            }
        }

        /// <summary>
        /// Fallback: генерирует одноцветный тайл если pixel art спрайты не назначены.
        /// </summary>
        private Tile GetFallbackTile(string key, Color color)
        {
            if (_fallbackTileCache.TryGetValue(key, out var tile))
            {
                return tile;
            }

            var texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            _fallbackTileCache[key] = tile;
            return tile;
        }
    }
}
