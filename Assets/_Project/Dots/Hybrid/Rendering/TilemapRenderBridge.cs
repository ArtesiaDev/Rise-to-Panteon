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
        private readonly Dictionary<string, Tile> _tileCache = new();
        private int _lastRunId = -1;

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

            var size = map.Size;
            var cellCount = size.x * size.y;
            var bounds = new BoundsInt(0, 0, 0, size.x, size.y, 1);

            // Массовая установка тайлов через SetTilesBlock — намного быстрее поштучного SetTile
            var groundTiles = new TileBase[cellCount];
            var wallTiles = new TileBase[cellCount];
            var hazardTiles = new TileBase[cellCount];

            var floorTile = GetTile("floor", _floorColor);
            var wallTile = GetTile("wall", _wallColor);
            var obstacleTile = GetTile("obstacle", _obstacleColor);
            var poisonTile = GetTile("poison", _poisonColor);
            var spikeTile = GetTile("spike", _spikeColor);

            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var index = y * size.x + x;

                    if (map.BaseLayer[index] == MapCellType.Floor)
                    {
                        groundTiles[index] = floorTile;
                    }
                    else if (map.BaseLayer[index] == MapCellType.Wall)
                    {
                        wallTiles[index] = wallTile;
                    }

                    if (map.ObstacleLayer[index] != ObstacleType.None)
                    {
                        wallTiles[index] = obstacleTile;
                    }

                    var hazard = map.HazardLayer[index];
                    if (hazard == HazardType.Poison)
                    {
                        hazardTiles[index] = poisonTile;
                    }
                    else if (hazard == HazardType.Spike)
                    {
                        hazardTiles[index] = spikeTile;
                    }
                }
            }

            ground.SetTilesBlock(bounds, groundTiles);
            walls.SetTilesBlock(bounds, wallTiles);
            hazards.SetTilesBlock(bounds, hazardTiles);

            ground.CompressBounds();
            walls.CompressBounds();
            hazards.CompressBounds();
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

        private Tile GetTile(string key, Color color)
        {
            if (_tileCache.TryGetValue(key, out var tile))
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
            _tileCache[key] = tile;
            return tile;
        }
    }
}
