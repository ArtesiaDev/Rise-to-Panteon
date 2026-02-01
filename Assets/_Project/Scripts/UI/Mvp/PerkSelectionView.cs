using System;
using UnityEngine;
using UnityEngine.UI;
using RuntimeRoguelike;

namespace RuntimeRoguelike.UI.Mvp
{
    public class PerkSelectionView : MonoBehaviour
    {
        public event Action<PerkDefinition> OnPerkSelected;

        private UiFactory _uiFactory;
        private GameObject _panel;
        private Button[] _buttons;
        private Text[] _buttonTexts;

        public void Initialize(UiFactory uiFactory)
        {
            _uiFactory = uiFactory;
            BuildUi();
        }

        public void SetOffer(PerkDefinition[] options, bool isVisible)
        {
            _panel.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            var count = options != null ? Mathf.Min(_buttons.Length, options.Length) : 0;
            for (var i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].onClick.RemoveAllListeners();
                if (i < count)
                {
                    var perk = options[i];
                    _buttonTexts[i].text = $"{perk.Title}\n{perk.Description}";
                    _buttons[i].onClick.AddListener(() => OnPerkSelected?.Invoke(perk));
                    _buttons[i].gameObject.SetActive(true);
                }
                else
                {
                    _buttons[i].gameObject.SetActive(false);
                }
            }
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("PerkCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("PerkPanel");
            _panel.transform.SetParent(canvasObject.transform, false);
            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.3f);
            panelRect.anchorMax = new Vector2(0.7f, 0.7f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var titleText = _uiFactory.CreateText(_panel.transform, "Title", 20, TextAnchor.UpperCenter, Color.white);
            titleText.text = "Choose a Perk";
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.75f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(10f, -10f);
            titleRect.offsetMax = new Vector2(-10f, -10f);

            _buttons = new Button[3];
            _buttonTexts = new Text[3];
            for (var i = 0; i < 3; i++)
            {
                var button = _uiFactory.CreateButton(_panel.transform, $"PerkButton{i}", "Perk", 16, new Color(0.2f, 0.2f, 0.2f, 0.9f), Color.white);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.1f + i * 0.2f);
                rect.anchorMax = new Vector2(0.9f, 0.25f + i * 0.2f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                _buttons[i] = button;
                _buttonTexts[i] = button.GetComponentInChildren<Text>();
            }

            _panel.SetActive(false);
        }
    }
}
