using System.Collections.Generic;
using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class PathfindingService
    {
        private MapGrid _grid;
        private GridOccupancy _occupancy;
        private AStarPathfinder _pathfinder;

        public void Configure(MapGrid grid, GridOccupancy occupancy)
        {
            _grid = grid;
            _occupancy = occupancy;
            _pathfinder = new AStarPathfinder(grid, occupancy);
        }

        public List<Int2> FindPath(Int2 start, Int2 goal)
        {
            if (_pathfinder == null || _grid == null || _occupancy == null)
            {
                return new List<Int2>();
            }

            var path = _pathfinder.FindPath(new Vector2Int(start.X, start.Y), new Vector2Int(goal.X, goal.Y));
            var result = new List<Int2>(path.Count);
            for (var i = 0; i < path.Count; i++)
            {
                result.Add(new Int2(path[i].x, path[i].y));
            }

            return result;
        }
    }
}
