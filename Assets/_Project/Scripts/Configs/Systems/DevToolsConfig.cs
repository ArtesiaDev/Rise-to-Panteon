using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "DevToolsConfig", menuName = "Configs/Systems/DevToolsConfig", order = 13)]
    public class DevToolsConfig : ScriptableObject
    {
        [field: SerializeField] public bool Enabled { get; private set; } = true;
        [field: SerializeField] public KeyCode RestartKey { get; private set; } = KeyCode.F5;
        [field: SerializeField] public KeyCode ToggleGizmosKey { get; private set; } = KeyCode.G;
        [field: SerializeField] public KeyCode TeleportKey { get; private set; } = KeyCode.T;
    }
}
