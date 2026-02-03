namespace RuntimeRoguelike.Ecs
{
    public interface IEcsPostUpdateSystem
    {
        void PostUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime);
    }
}
