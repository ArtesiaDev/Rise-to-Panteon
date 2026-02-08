using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct RenderInterpolationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (grid, prevGrid, render, cooldown, speed) in SystemAPI
                .Query<RefRO<GridPosition>, RefRW<PreviousGridPosition>, RefRW<RenderPosition>,
                    RefRO<MoveCooldown>, RefRO<MoveSpeed>>())
            {
                var current = grid.ValueRO.Value;
                var prev = prevGrid.ValueRO.Value;
                var interval = speed.ValueRO.CellsPerSecond > 0.01f ? 1f / speed.ValueRO.CellsPerSecond : 0.1f;
                var t = interval > 0.0001f ? math.saturate(1f - cooldown.ValueRO.Remaining / interval) : 1f;
                render.ValueRW.Value = math.lerp(new float2(prev.x, prev.y), new float2(current.x, current.y), t);
                if (t >= 1f)
                    prevGrid.ValueRW.Value = current;
            }

            foreach (var (grid, prevGrid, render) in SystemAPI
                .Query<RefRO<GridPosition>, RefRW<PreviousGridPosition>, RefRW<RenderPosition>>()
                .WithNone<MoveCooldown, MoveSpeed>())
            {
                var cell = grid.ValueRO.Value;
                render.ValueRW.Value = new float2(cell.x, cell.y);
                prevGrid.ValueRW.Value = cell;
            }

            foreach (var (grid, render) in SystemAPI
                .Query<RefRO<GridPosition>, RefRW<RenderPosition>>()
                .WithNone<PreviousGridPosition>())
            {
                var cell = grid.ValueRO.Value;
                render.ValueRW.Value = new float2(cell.x, cell.y);
            }
        }
    }
}
