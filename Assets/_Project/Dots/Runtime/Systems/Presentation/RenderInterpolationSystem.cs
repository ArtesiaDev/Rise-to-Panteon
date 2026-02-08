using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct RenderInterpolationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (grid, render) in SystemAPI.Query<RefRO<GridPosition>, RefRW<RenderPosition>>())
            {
                var cell = grid.ValueRO.Value;
                render.ValueRW.Value = new float2(cell.x, cell.y);
            }
        }
    }
}
