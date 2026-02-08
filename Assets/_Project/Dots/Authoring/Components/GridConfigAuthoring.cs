using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class GridConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;

        private class Baker : Baker<GridConfigAuthoring>
        {
            public override void Bake(GridConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GridConfigData
                {
                    CellSize = authoring.cellSize
                });
            }
        }
    }
}
