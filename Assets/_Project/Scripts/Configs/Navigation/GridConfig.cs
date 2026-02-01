using UnityEngine;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Configs/Navigation/GridConfig", order = 11)]
    public class GridConfig : ScriptableObject
    {
        [field: SerializeField] public float CellSize { get; private set; } = 1f;
    }
}
