using System.Collections.Generic;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    /// <summary>
    /// Профиль конфигурации — агрегирует набор SO-конфигов.
    /// Разные профили позволяют переключаться между пресетами (Easy, Hard, Debug и т.д.).
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Profile")]
    public class ConfigProfileSO : ScriptableObject
    {
        [Tooltip("Список конфигов, каждый SO должен реализовывать IConfigApplier")]
        [SerializeField] private List<ScriptableObject> _configs = new();

        public IReadOnlyList<ScriptableObject> Configs => _configs;
    }
}
