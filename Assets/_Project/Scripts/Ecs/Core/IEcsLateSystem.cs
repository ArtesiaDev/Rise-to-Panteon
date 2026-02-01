namespace RuntimeRoguelike.Ecs
{
    public interface IEcsLateSystem
    {
        void LateUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime);
    }
}
