using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "RunConfig", menuName = "Configs/Run/RunConfig", order = 1)]
    public class RunConfig : ScriptableObject
    {
        [field: SerializeField] public bool RandomizeSeedOnStart { get; private set; } = true;
        [field: SerializeField] public int InitialSeed { get; private set; }
        [field: SerializeField] public Vector2Int MapSize { get; private set; } = new Vector2Int(200, 200);
        [field: SerializeField] public KeyCode RestartKey { get; private set; } = KeyCode.R;
    }
}
