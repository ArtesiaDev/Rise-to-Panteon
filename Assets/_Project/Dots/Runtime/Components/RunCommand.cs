using Unity.Entities;

namespace RuntimeRoguelike.Dots
{
    public struct RunCommand : IComponentData
    {
        public bool Restart;
        public bool PerkChosen;
        public int ChosenPerkIndex;
    }
}
