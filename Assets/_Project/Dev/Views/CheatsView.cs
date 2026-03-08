using Dev;
using Dev.Views;
using UnityEngine;

namespace Cheats.Views
{
    public class CheatsView : MonoBehaviour, ICheatViewBuilder
    {
        [SerializeField] private Transform contentTransform;
        [SerializeField] private CheatGroupView cheatGroupPrefab;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public ICheatGroupView CreateGroup(string caption = null)
        {
            CheatGroupView group = Instantiate(cheatGroupPrefab, contentTransform);
            group.Title = caption;
            return group;
        }
    }
}
