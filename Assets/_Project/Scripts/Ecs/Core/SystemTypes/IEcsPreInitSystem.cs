namespace RuntimeRoguelike.Ecs
{
    public interface IEcsPreInitSystem
    {
        void PreInit(EcsWorld world, EcsCommandBuffer commandBuffer);
    }
}
