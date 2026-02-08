using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class HazardConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private float _hazardChance = 0.03f;
        [SerializeField] private float _spikeChance = 0.6f;
        [SerializeField] private float _spikeTickInterval = 0.8f;
        [SerializeField] private int _spikeDamage = 1;
        [SerializeField] private float _poisonDuration = 4f;
        [SerializeField] private float _poisonDps = 1.2f;
        [SerializeField] private float _poisonTickInterval = 0.5f;

        private class Baker : Baker<HazardConfigAuthoring>
        {
            public override void Bake(HazardConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new HazardConfigData
                {
                    HazardChance = authoring._hazardChance,
                    SpikeChance = authoring._spikeChance,
                    SpikeTickInterval = authoring._spikeTickInterval,
                    SpikeDamage = authoring._spikeDamage,
                    PoisonDuration = authoring._poisonDuration,
                    PoisonDps = authoring._poisonDps,
                    PoisonTickInterval = authoring._poisonTickInterval
                });
            }
        }
    }
}
