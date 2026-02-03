namespace RuntimeRoguelike.Ecs
{
    public interface IEcsFixedUpdateSystem
    {
        void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime);
    }
}
