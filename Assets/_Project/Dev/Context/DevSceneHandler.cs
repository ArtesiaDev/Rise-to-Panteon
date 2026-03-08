using System.Collections.Generic;
using Cheats.Views;
using Dev;
using VContainer.Unity;

namespace Dev.Scenes
{
    public class DevSceneHandler : IStartable
    {
        private readonly IReadOnlyList<ICheat> _cheats;
        private readonly CheatsView _cheatsView;

        public DevSceneHandler(IReadOnlyList<ICheat> cheats, CheatsView cheatsView)
        {
            _cheats = cheats;
            _cheatsView = cheatsView;
        }

        public void Start()
        {
            _cheatsView.gameObject.SetActive(false);
            foreach (var cheat in _cheats)
            {
                cheat.Initialize(_cheatsView);
            }
        }
    }
}
