namespace RuntimeRoguelike.Ecs
{
    public readonly struct EcsEntity
    {
        public int Id { get; }
        public int Generation { get; }

        public EcsEntity(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }
    }
}
