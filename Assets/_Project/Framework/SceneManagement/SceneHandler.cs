using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Scenes
{
    /// <summary>
    /// Базовый обработчик сцены. Размещается на root GameObject сцены.
    /// ScenesLoader находит его и вызывает Init() после загрузки.
    /// </summary>
    public abstract class SceneHandler : MonoBehaviour
    {
        private bool _started;

        protected abstract UniTask HandleInit(CancellationToken ct);

        internal async UniTask Init(CancellationToken ct)
        {
            // Ждем пока Unity вызовет Start()
            while (!_started)
                await UniTask.Yield(ct);

            await HandleInit(ct);
        }

        private void Start() => _started = true;
    }
}
