namespace RuntimeRoguelike.Ecs
{
    public interface IEcsInitSystem
    {
        void Init(EcsWorld world, EcsCommandBuffer commandBuffer);
    }
}
