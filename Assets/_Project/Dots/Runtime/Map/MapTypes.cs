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

    /// <summary>
    /// Тип комнаты — влияет на вероятности спауна контента.
    /// </summary>
    public enum RoomType : byte
    {
        Start = 0,
        Normal = 1,
        Treasure = 2,
        HardEnemy = 3
    }

    /// <summary>
    /// Состояние видимости клетки (туман войны).
    /// </summary>
    public enum FogState : byte
    {
        Unexplored = 0,
        Explored = 1,
        Visible = 2
    }
}
