using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.Views
{
	public class CheatFieldLineWithResultView : MonoBehaviour
	{
		[SerializeField] private TMP_InputField inputField;
		[SerializeField] private TMP_Text label;
		[SerializeField] private Button button;
		[SerializeField] private TMP_Text result;

		public Action<string, TMP_Text> Clicked;

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

		public string Result
		{
			get => result.text;
			set => result.text = value;
		}

		private void Awake()
		{
			button.onClick.AddListener(() => { Clicked?.Invoke(Input, result); });
		}
	}
}