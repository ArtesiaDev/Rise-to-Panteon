using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cheats.Views
{
    public class CheatButtonLineView: MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;
        
        public Action Clicked;
        
        public string Text
        {
            get => label.text; 
            set => label.text = value;
        }

        private void Awake()
        {
            button.onClick.AddListener(()=>{Clicked?.Invoke();});
        }
    }
}