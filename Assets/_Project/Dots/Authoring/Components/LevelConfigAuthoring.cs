using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Authoring
{
    public class LevelConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private int _baseXpToLevel = 10;
        [SerializeField] private int _xpIncreasePerLevel = 5;

        private class Baker : Baker<LevelConfigAuthoring>
        {
            public override void Bake(LevelConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new LevelConfigData
                {
                    BaseXpToLevel = authoring._baseXpToLevel,
                    XpIncreasePerLevel = authoring._xpIncreasePerLevel
                });
            }
        }
    }
}
