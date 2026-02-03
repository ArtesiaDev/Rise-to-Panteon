using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RuntimeRoguelike
{
    public class TilemapWorldRenderer
    {
        private readonly SpriteFactory _spriteFactory;
        private readonly GridPositionConverter _gridPositionConverter;
        private readonly Dictionary<SpriteKey, Tile> _tileCache = new Dictionary<SpriteKey, Tile>();

        public TilemapWorldRenderer(SpriteFactory spriteFactory, GridPositionConverter gridPositionConverter)
        {
            _spriteFactory = spriteFactory;
            _gridPositionConverter = gridPositionConverter;
        }

        public void Render(MapGrid grid, Transform parent)
        {
            var gridRoot = new GameObject("Grid");
            gridRoot.transform.SetParent(parent, false);
            var gridComponent = gridRoot.AddComponent<Grid>();
            gridComponent.cellSize = Vector3.one * _gridPositionConverter.CellSize;

            var ground = CreateTilemap(gridRoot.transform, "Ground", 0, false);
            var walls = CreateTilemap(gridRoot.transform, "Walls", 1, true);
            var hazards = CreateTilemap(gridRoot.transform, "Hazards", 2, false);

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var cell = grid.Get(x, y);
                    var position = new Vector3Int(x, y, 0);

                    if (cell.CellType == CellType.Floor)
                    {
                        ground.SetTile(position, GetTile(SpriteKey.Floor));
                    }
                    else if (cell.CellType == CellType.Wall)
                    {
                        walls.SetTile(position, GetTile(SpriteKey.Wall));
                    }

                    if (cell.Obstacle != ObstacleType.None)
                    {
                        walls.SetTile(position, GetTile(SpriteKey.Obstacle));
                    }

                    if (cell.Hazard != HazardType.None)
                    {
                        var key = cell.Hazard == HazardType.Poison ? SpriteKey.Hazard : SpriteKey.Trap;
                        hazards.SetTile(position, GetTile(key));
                    }
                }
            }

            ground.CompressBounds();
            walls.CompressBounds();
            hazards.CompressBounds();
        }

        private Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, bool withColliders)
        {
            var tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(parent, false);

            var tilemap = tilemapObject.AddComponent<Tilemap>();
            var renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;

            if (withColliders)
            {
                // Some Unity setups can fail to add 2D physics components at runtime.
                // Make this robust: render should still work even without colliders.
                var body = tilemapObject.AddComponent<Rigidbody2D>();
                if (body == null)
                {
                    Debug.LogError("Failed to add Rigidbody2D for Tilemap collisions. Colliders will be skipped.");
                    return tilemap;
                }

                body.bodyType = RigidbodyType2D.Static;

                var collider = tilemapObject.AddComponent<TilemapCollider2D>();
                if (collider == null)
                {
                    Debug.LogError("Failed to add TilemapCollider2D for Tilemap collisions. Colliders will be skipped.");
                    return tilemap;
                }

                var composite = tilemapObject.AddComponent<CompositeCollider2D>();
                if (composite == null)
                {
                    Debug.LogError("Failed to add CompositeCollider2D for Tilemap collisions. Colliders will be skipped.");
                    return tilemap;
                }

                collider.usedByComposite = true;
            }

            return tilemap;
        }

        private Tile GetTile(SpriteKey key)
        {
            if (_tileCache.TryGetValue(key, out var tile))
            {
                return tile;
            }

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = _spriteFactory.GetSprite(key);
            _tileCache[key] = tile;
            return tile;
        }
    }
}
