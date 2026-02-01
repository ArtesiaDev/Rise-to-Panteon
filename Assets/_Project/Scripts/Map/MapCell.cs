namespace RuntimeRoguelike
{
    public readonly struct MapCell
    {
        public CellType CellType { get; }
        public ObstacleType Obstacle { get; }
        public HazardType Hazard { get; }
        public bool Walkable { get; }
        public bool BlocksVision { get; }
        public int OnEnterDamage { get; }

        public MapCell(CellType cellType, ObstacleType obstacle, HazardType hazard, bool walkable, bool blocksVision, int onEnterDamage)
        {
            CellType = cellType;
            Obstacle = obstacle;
            Hazard = hazard;
            Walkable = walkable;
            BlocksVision = blocksVision;
            OnEnterDamage = onEnterDamage;
        }
    }
}
