using System;

namespace RuntimeRoguelike
{
    [Serializable]
    public struct PerkDefinition
    {
        public PerkType Type;
        public string Title;
        public string Description;
        public float Value;
    }
}
