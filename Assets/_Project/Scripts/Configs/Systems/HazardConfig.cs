using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "HazardConfig", menuName = "Configs/Systems/HazardConfig", order = 6)]
    public class HazardConfig : ScriptableObject
    {
        [field: SerializeField] public float HazardChance { get; private set; } = 0.03f;
        [field: SerializeField] public float SpikeChance { get; private set; } = 0.6f;
        [field: SerializeField, Space(10)] public float SpikeTickInterval { get; private set; } = 0.8f;
        [field: SerializeField] public int SpikeDamage { get; private set; } = 1;
        [field: SerializeField, Space(10)] public float PoisonDuration { get; private set; } = 4f;
        [field: SerializeField] public float PoisonDps { get; private set; } = 1.2f;
        [field: SerializeField] public float PoisonTickInterval { get; private set; } = 0.5f;
    }
}
