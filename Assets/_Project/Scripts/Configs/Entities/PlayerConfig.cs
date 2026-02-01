using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Configs/Entities/PlayerConfig", order = 5)]
    public class PlayerConfig : ScriptableObject
    {
        [field: SerializeField] public int MaxHealth { get; private set; } = 12;
        [field: SerializeField] public float MoveSpeed { get; private set; } = 4f;
        [field: SerializeField] public Vector2 ColliderSize { get; private set; } = new Vector2(0.8f, 0.8f);
    }
}
