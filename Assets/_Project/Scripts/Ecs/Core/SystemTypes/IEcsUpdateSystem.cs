namespace RuntimeRoguelike.Ecs
{
    public interface IEcsUpdateSystem
    {
        void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime);
    }
}
