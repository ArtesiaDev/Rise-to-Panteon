using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "CameraFollowConfig", menuName = "Configs/Rendering/CameraFollowConfig", order = 12)]
    public class CameraFollowConfig : ScriptableObject
    {
        [field: SerializeField] public float SmoothTime { get; private set; } = 0.15f;
    }
}
