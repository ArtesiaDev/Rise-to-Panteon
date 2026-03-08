using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework.Scenes
{
    /// <summary>
    /// VContainer LifetimeScope для сцены.
    /// Размещается на root GameObject сцены рядом с SceneHandler.
    /// ScenesLoader находит его и устанавливает parent scope.
    /// </summary>
    public class SceneScope : LifetimeScope
    {
        [SerializeField] private bool excludeFromScopeHierarchy;

        /// <summary>
        /// Исключена ли из иерархии скоупов.
        /// Если true, дочерние сцены не будут наследовать этот скоуп.
        /// </summary>
        public bool ExcludeFromScopeHierarchy => excludeFromScopeHierarchy;
    }
}
