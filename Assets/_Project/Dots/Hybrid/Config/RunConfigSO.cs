using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Run")]
    public class RunConfigSO : ScriptableObject, IConfigApplier
    {
        public bool RandomizeSeedOnStart = true;
        public int InitialSeed;

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new RunConfigData
            {
                RandomizeSeedOnStart = RandomizeSeedOnStart,
                InitialSeed = InitialSeed
            });
        }
    }
}
