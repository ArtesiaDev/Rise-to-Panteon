using System;
using Cheats.Views;
using TMPro;
using UnityEngine;

namespace Dev.Views
{
    public class CheatGroupView : MonoBehaviour, ICheatGroupView
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CheatButtonLineView buttonLinePrefab;
        [SerializeField] private CheatFieldLineView fieldLinePrefab;
        [SerializeField] private CheatDualFieldLineView dualFieldLinePrefab;
        [SerializeField] private CheatToggleLineView toggleLinePrefab;
        [SerializeField] private CheatFieldLineWithResultView fieldLineWithResultPrefab;

        public string Title
        {
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    label.gameObject.SetActive(false);
                }
                else
                {
                    label.gameObject.SetActive(true);
                    label.text = value;
                }
            }
        }

        public CheatGroupView AddCheatButton(string text, Action action, bool autoHide = true)
        {
            CheatButtonLineView button = Instantiate(buttonLinePrefab, transform);
            button.Text = text;
            button.Clicked = action;
            if (autoHide) button.Clicked += Hide;
            button.transform.SetAsLastSibling();
            return this;
        }

        public CheatGroupView AddCheatToggle(string text, bool isOn, Action<bool> action)
        {
            CheatToggleLineView line = Instantiate(toggleLinePrefab, transform);
            line.Text = text;
            line.IsOn = isOn;
            line.Changed += action;
            line.transform.SetAsLastSibling();
            return this;
        }

        public CheatGroupView AddCheatFieldWithResult(string text, Action<string, TMP_Text> action)
        {
            CheatFieldLineWithResultView line = Instantiate(fieldLineWithResultPrefab, transform);
            line.Text = text;
            line.Input = string.Empty;
            line.Result = string.Empty;
            line.Clicked += action;
            line.transform.SetAsLastSibling();
            return this;
        }

        public CheatGroupView AddCheatField(string text, TMP_InputField.ContentType contentType, Action<CheatFieldLineView> action, bool autoHide = true)
        {
            CheatFieldLineView field = Instantiate(fieldLinePrefab, transform);
            field.Text = text;
            field.ContentType = contentType;
            field.Clicked = action;
            if (autoHide) field.Clicked += Hide;
            field.Input = string.Empty;
            field.transform.SetAsLastSibling();
            return this;
        }

        public CheatGroupView AddCheatFieldAsInt(string text, Action<int> action, bool autoHide = true)
        {
            AddCheatField(text, TMP_InputField.ContentType.IntegerNumber, (f) =>
            {
                if (int.TryParse(f.Input, out int value))
                {
                    action(value);
                }
                else
                {
                    f.Input = string.Empty;
                }
            });
            return this;
        }

        public CheatGroupView AddCheatDualField(string text, string placeholder1, string placeholder2, Action<int, int> onAction, bool autoHide = true)
        {
            var field = Instantiate(dualFieldLinePrefab, transform);
            field.Text = text;
            field.Clicked = f =>
            {
                onAction(int.Parse(f.Field1.text), int.Parse(f.Field2.text));
            };
            if (autoHide) field.Clicked += (f) => Hide();
            field.Field1.text = string.Empty;
            field.Field1.placeholder.GetComponent<TMP_Text>().text = placeholder1;
            field.Field2.text = string.Empty;
            field.Field2.placeholder.GetComponent<TMP_Text>().text = placeholder2;
            field.transform.SetAsLastSibling();
            return this;
        }

        private void Hide(CheatFieldLineView sender)
        {
            Hide();
        }

        private void Hide()
        {
            label.canvas.gameObject.SetActive(false);
        }
    }
}
