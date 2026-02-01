namespace RuntimeRoguelike.Ecs
{
    public interface IEcsPool
    {
        void RemoveEntity(int entityId);
        void Clear();
    }
}
