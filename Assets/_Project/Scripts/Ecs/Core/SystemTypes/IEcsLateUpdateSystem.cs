namespace RuntimeRoguelike.Ecs
{
    public interface IEcsLateUpdateSystem
    {
        void LateUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime);
    }
}
