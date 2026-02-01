using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Configs/Systems/LevelConfig", order = 8)]
    public class LevelConfig : ScriptableObject
    {
        [field: SerializeField] public int BaseXpToLevel { get; private set; } = 10;
        [field: SerializeField] public int XpIncreasePerLevel { get; private set; } = 5;
    }
}
