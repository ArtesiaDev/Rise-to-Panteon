using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cheats.Views
{
    public class CheatDualFieldLineView: MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField1;
        [SerializeField] private TMP_InputField inputField2;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;
        
        public Action<CheatDualFieldLineView> Clicked;

        public string Text
        {
            get => label.text; 
            set => label.text = value;
        }

        public TMP_InputField Field1 => inputField1;
        public TMP_InputField Field2 => inputField2;

        private void Awake()
        {
            button.onClick.AddListener(()=>{Clicked?.Invoke(this);});
        }
    }
}