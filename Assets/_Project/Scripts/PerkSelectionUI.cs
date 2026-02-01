using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeRoguelike
{
    public class PerkSelectionUI : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject panel;
        private Button[] buttons;
        private Text[] buttonTexts;
        private Text titleText;
        private Action<PerkDefinition> onSelected;

        public void Initialize()
        {
            canvas = CreateCanvas("PerkCanvas", 10);
            canvas.transform.SetParent(transform, false);
            panel = new GameObject("PerkPanel");
            panel.transform.SetParent(canvas.transform, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.3f);
            panelRect.anchorMax = new Vector2(0.7f, 0.7f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            titleText = UIRuntimeUtils.CreateText(panel.transform, "Title", 20, TextAnchor.UpperCenter, Color.white);
            titleText.text = "Choose a Perk";
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.75f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(10f, -10f);
            titleRect.offsetMax = new Vector2(-10f, -10f);

            buttons = new Button[3];
            buttonTexts = new Text[3];
            for (var i = 0; i < 3; i++)
            {
                var button = UIRuntimeUtils.CreateButton(panel.transform, $"PerkButton{i}", "Perk", 16, new Color(0.2f, 0.2f, 0.2f, 0.9f), Color.white);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.1f + i * 0.2f);
                rect.anchorMax = new Vector2(0.9f, 0.25f + i * 0.2f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var text = button.GetComponentInChildren<Text>();
                buttonTexts[i] = text;
                buttons[i] = button;
            }

            panel.SetActive(false);
        }

        public void Show(PerkDefinition[] perks, Action<PerkDefinition> onSelectedCallback)
        {
            onSelected = onSelectedCallback;
            panel.SetActive(true);

            for (var i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.RemoveAllListeners();
                var perk = perks[i];
                buttonTexts[i].text = $"{perk.Title}\n{perk.Description}";
                buttons[i].onClick.AddListener(() => SelectPerk(perk));
            }
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        private void SelectPerk(PerkDefinition perk)
        {
            onSelected?.Invoke(perk);
        }

        private static Canvas CreateCanvas(string name, int sortingOrder)
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
