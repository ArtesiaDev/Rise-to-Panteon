using System.Collections.Generic;
using RuntimeRoguelike.Dots;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class TilemapRenderBridge : MonoBehaviour
    {
        [SerializeField] private Transform worldRoot;
        [SerializeField] private Color floorColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color wallColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color obstacleColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        [SerializeField] private Color poisonColor = new Color(0.2f, 0.5f, 0.2f, 1f);
        [SerializeField] private Color spikeColor = new Color(0.6f, 0.2f, 0.2f, 1f);

        private EntityManager _entityManager;
        private EntityQuery _mapQuery;
        private EntityQuery _gridQuery;
        private Entity _mapEntity;
        private GameObject _gridRoot;
        private readonly Dictionary<string, Tile> _tileCache = new Dictionary<string, Tile>();
        private int _lastRunId = -1;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = world.EntityManager;
            _mapQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MapBlobReference>(),
                ComponentType.ReadOnly<MapRenderRequest>());
            _gridQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfigData>());
        }

        private void OnDestroy()
        {
            if (_mapQuery.IsCreated)
            {
                _mapQuery.Dispose();
            }

            if (_gridQuery.IsCreated)
            {
                _gridQuery.Dispose();
            }
        }

        private void Update()
        {
            if (!_mapQuery.IsCreated || _mapQuery.IsEmpty)
            {
                return;
            }

            _mapEntity = _mapQuery.GetSingletonEntity();
            if (!_entityManager.IsComponentEnabled<MapRenderRequest>(_mapEntity))
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

            Render(mapRef.Value.Value, gridSize);
            _lastRunId = request.RunId;
            _entityManager.SetComponentEnabled<MapRenderRequest>(_mapEntity, false);
        }

        private void Render(in MapBlob map, float cellSize)
        {
            if (_gridRoot != null)
            {
                Destroy(_gridRoot);
            }

            var parent = worldRoot != null ? worldRoot : transform;
            _gridRoot = new GameObject("DotsGrid");
            _gridRoot.transform.SetParent(parent, false);

            var grid = _gridRoot.AddComponent<Grid>();
            grid.cellSize = Vector3.one * cellSize;

            var ground = CreateTilemap(_gridRoot.transform, "Ground", 0, false);
            var walls = CreateTilemap(_gridRoot.transform, "Walls", 1, true);
            var hazards = CreateTilemap(_gridRoot.transform, "Hazards", 2, false);

            var size = map.Size;
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var index = y * size.x + x;
                    var position = new Vector3Int(x, y, 0);

                    if (map.BaseLayer[index] == MapCellType.Floor)
                    {
                        ground.SetTile(position, GetTile("floor", floorColor));
                    }
                    else if (map.BaseLayer[index] == MapCellType.Wall)
                    {
                        walls.SetTile(position, GetTile("wall", wallColor));
                    }

                    if (map.ObstacleLayer[index] != ObstacleType.None)
                    {
                        walls.SetTile(position, GetTile("obstacle", obstacleColor));
                    }

                    var hazard = map.HazardLayer[index];
                    if (hazard == HazardType.Poison)
                    {
                        hazards.SetTile(position, GetTile("poison", poisonColor));
                    }
                    else if (hazard == HazardType.Spike)
                    {
                        hazards.SetTile(position, GetTile("spike", spikeColor));
                    }
                }
            }

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
                var composite = tilemapObject.AddComponent<CompositeCollider2D>();
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
