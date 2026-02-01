using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RuntimeRoguelike
{
    public sealed class TilemapWorldRenderer
    {
        private readonly Dictionary<SpriteKey, Tile> tileCache = new Dictionary<SpriteKey, Tile>();

        public void Render(MapGrid grid, Transform parent)
        {
            var gridRoot = new GameObject("Grid");
            gridRoot.transform.SetParent(parent, false);
            var gridComponent = gridRoot.AddComponent<Grid>();
            gridComponent.cellSize = Vector3.one;

            var ground = CreateTilemap(gridRoot.transform, "Ground", 0, false);
            var walls = CreateTilemap(gridRoot.transform, "Walls", 1, true);
            var hazards = CreateTilemap(gridRoot.transform, "Hazards", 2, false);

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var cell = grid.Get(x, y);
                    var position = new Vector3Int(x, y, 0);

                    if (cell.cellType == CellType.Floor)
                    {
                        ground.SetTile(position, GetTile(SpriteKey.Floor));
                    }
                    else if (cell.cellType == CellType.Wall)
                    {
                        walls.SetTile(position, GetTile(SpriteKey.Wall));
                    }

                    if (cell.obstacle != ObstacleType.None)
                    {
                        walls.SetTile(position, GetTile(SpriteKey.Obstacle));
                    }

                    if (cell.hazard != HazardType.None)
                    {
                        var key = cell.hazard == HazardType.Poison ? SpriteKey.Hazard : SpriteKey.Trap;
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
                var collider = tilemapObject.AddComponent<TilemapCollider2D>();
                collider.usedByComposite = true;
                tilemapObject.AddComponent<CompositeCollider2D>();
                var body = tilemapObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
            }

            return tilemap;
        }

        private Tile GetTile(SpriteKey key)
        {
            if (tileCache.TryGetValue(key, out var tile))
            {
                return tile;
            }

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = SpriteFactory.Get(key);
            tileCache[key] = tile;
            return tile;
        }
    }
}
