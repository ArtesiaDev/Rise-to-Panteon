using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class HazardConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private float hazardChance = 0.03f;
        [SerializeField] private float spikeChance = 0.6f;
        [SerializeField] private float spikeTickInterval = 0.8f;
        [SerializeField] private int spikeDamage = 1;
        [SerializeField] private float poisonDuration = 4f;
        [SerializeField] private float poisonDps = 1.2f;
        [SerializeField] private float poisonTickInterval = 0.5f;

        private class Baker : Baker<HazardConfigAuthoring>
        {
            public override void Bake(HazardConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new HazardConfigData
                {
                    HazardChance = authoring.hazardChance,
                    SpikeChance = authoring.spikeChance,
                    SpikeTickInterval = authoring.spikeTickInterval,
                    SpikeDamage = authoring.spikeDamage,
                    PoisonDuration = authoring.poisonDuration,
                    PoisonDps = authoring.poisonDps,
                    PoisonTickInterval = authoring.poisonTickInterval
                });
            }
        }
    }
}
