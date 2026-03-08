using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cheats.Views
{
    public class CheatToggleLineView : MonoBehaviour
    {
        [SerializeField] private Toggle inputField;
        [SerializeField] private TMP_Text label;

        public event Action<bool> Changed;
        
        public bool IsOn
        {
            get => inputField.isOn;
            set => inputField.isOn = value;
        }

        public string Text
        {
            get => label.text;
            set => label.text = value;
        }

        private void Awake()
        {
            inputField.onValueChanged.AddListener(v=>Changed?.Invoke(v));
        }
    }
}