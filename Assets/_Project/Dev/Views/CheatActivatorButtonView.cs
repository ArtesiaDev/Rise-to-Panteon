using UnityEngine;
using UnityEngine.EventSystems;

namespace Cheats.Views
{
    public class CheatActivatorButtonView: MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CheatsView viewCheats;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            viewCheats.Show();
        }
    }
}