namespace RuntimeRoguelike.Ecs
{
    public interface IEcsPreUpdateSystem
    {
        void PreUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime);
    }
}
