using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    [CreateAssetMenu(menuName = "Config/Visibility")]
    public class VisibilityConfigSO : ScriptableObject, IConfigApplier
    {
        [Range(1, 20)] public int ViewRadius = 8;
        public bool Enabled = true;

        public void Apply(Entity entity, EntityManager em)
        {
            em.AddComponentData(entity, new VisibilityConfigData
            {
                ViewRadius = ViewRadius,
                Enabled = Enabled
            });
        }
    }
}
