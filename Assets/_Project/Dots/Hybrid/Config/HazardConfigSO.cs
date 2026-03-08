using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Hazard")]
    public class HazardConfigSO : ScriptableObject, IConfigApplier
    {
        public float HazardChance = 0.03f;
        public float SpikeChance = 0.6f;
        public float SpikeTickInterval = 0.8f;
        public int SpikeDamage = 1;
        public float PoisonDuration = 4f;
        public float PoisonDps = 1.2f;
        public float PoisonTickInterval = 0.5f;

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new HazardConfigData
            {
                HazardChance = HazardChance,
                SpikeChance = SpikeChance,
                SpikeTickInterval = SpikeTickInterval,
                SpikeDamage = SpikeDamage,
                PoisonDuration = PoisonDuration,
                PoisonDps = PoisonDps,
                PoisonTickInterval = PoisonTickInterval
            });
        }
    }
}
