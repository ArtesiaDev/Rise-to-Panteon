using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cheats.Views
{
    public class CheatFieldLineView: MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;
        
        public Action<CheatFieldLineView> Clicked;

        public string Text
        {
            get => label.text; 
            set => label.text = value;
        }

        public string Input
        {
            get => inputField.text; 
            set => inputField.text = value;
        }
        
        public TMP_InputField.ContentType ContentType
        {
            get => inputField.contentType;
            set => inputField.contentType = value;
        }

        private void Awake()
        {
            button.onClick.AddListener(()=>{Clicked?.Invoke(this);});
        }
    }
}