using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct PlayerStats : IComponentData
    {
        public int Gold;
        public int Xp;
        public int Level;
        public int XpToNext;
        public float MoveSpeedMult;
        public float BonusDamage;
    }
}
