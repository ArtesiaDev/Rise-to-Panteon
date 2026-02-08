namespace RuntimeRoguelike.Dots.Runtime
{
    public enum MapCellType
    {
        Empty,
        Floor,
        Wall,
        Obstacle,
        Hazard
    }

    public enum ObstacleType
    {
        None,
        Rock
    }

    public enum HazardType
    {
        None,
        Spike,
        Poison
    }
}
