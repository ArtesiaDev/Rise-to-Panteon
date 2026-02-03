namespace RuntimeRoguelike.Ecs
{
    public interface IEcsLateInitSystem
    {
        void LateInit(EcsWorld world, EcsCommandBuffer commandBuffer);
    }
}
