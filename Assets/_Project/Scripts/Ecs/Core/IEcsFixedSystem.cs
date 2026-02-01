namespace RuntimeRoguelike.Ecs
{
    public interface IEcsFixedSystem
    {
        void FixedUpdate(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime);
    }
}
