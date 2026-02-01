using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace RuntimeRoguelike
{
    public class PerkSelectionUI : MonoBehaviour
    {
        private UiFactory _uiFactory;

        private Canvas _canvas;
        private GameObject _panel;
        private Button[] _buttons;
        private Text[] _buttonTexts;
        private Text _titleText;
        private Action<PerkDefinition> _onSelected;

        [Inject]
        private void Construct(UiFactory uiFactory)
        {
            _uiFactory = uiFactory;
        }

        public void Initialize()
        {
            _canvas = CreateCanvas("PerkCanvas", 10);
            _canvas.transform.SetParent(transform, false);

            _panel = new GameObject("PerkPanel");
            _panel.transform.SetParent(_canvas.transform, false);
            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.3f);
            panelRect.anchorMax = new Vector2(0.7f, 0.7f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            _titleText = _uiFactory.CreateText(_panel.transform, "Title", 20, TextAnchor.UpperCenter, Color.white);
            _titleText.text = "Choose a Perk";
            var titleRect = _titleText.rectTransform;
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

                var text = button.GetComponentInChildren<Text>();
                _buttonTexts[i] = text;
                _buttons[i] = button;
            }

            _panel.SetActive(false);
        }

        public void Show(PerkDefinition[] perks, Action<PerkDefinition> onSelectedCallback)
        {
            _onSelected = onSelectedCallback;
            _panel.SetActive(true);

            if (perks == null)
            {
                return;
            }

            var count = Mathf.Min(_buttons.Length, perks.Length);
            for (var i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].onClick.RemoveAllListeners();
                if (i < count)
                {
                    var perk = perks[i];
                    _buttonTexts[i].text = $"{perk.Title}\n{perk.Description}";
                    _buttons[i].onClick.AddListener(() => SelectPerk(perk));
                    _buttons[i].gameObject.SetActive(true);
                }
                else
                {
                    _buttons[i].gameObject.SetActive(false);
                }
            }
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        private void SelectPerk(PerkDefinition perk)
        {
            _onSelected?.Invoke(perk);
        }

        private Canvas CreateCanvas(string name, int sortingOrder)
        {
            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }
    }
}
