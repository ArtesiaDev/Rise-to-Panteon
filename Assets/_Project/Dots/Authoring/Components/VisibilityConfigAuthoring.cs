using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class VisibilityConfigAuthoring : MonoBehaviour
    {
        [SerializeField, Range(1, 20)] private int _viewRadius = 8;
        [SerializeField] private bool _enabled = true;

        private class Baker : Baker<VisibilityConfigAuthoring>
        {
            public override void Bake(VisibilityConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new VisibilityConfigData
                {
                    ViewRadius = authoring._viewRadius,
                    Enabled = authoring._enabled
                });
            }
        }
    }
}
