using System;
using UnityEngine;
using UnityEngine.UI;
using RuntimeRoguelike;

namespace RuntimeRoguelike.UI.Mvp
{
    public class HudView : MonoBehaviour
    {
        public event Action OnRestartClicked;

        private UiFactory _uiFactory;
        private Text _hpText;
        private Text _xpText;
        private Text _levelText;
        private Text _goldText;
        private Text _seedText;

        public void Initialize(UiFactory uiFactory)
        {
            _uiFactory = uiFactory;
            BuildUi();
        }

        public void SetHp(int current, int max)
        {
            if (_hpText != null)
            {
                _hpText.text = $"HP: {current}/{max}";
            }
        }

        public void SetProgress(int xp, int xpToNext, int level)
        {
            if (_xpText != null)
            {
                _xpText.text = $"XP: {xp}/{xpToNext}";
            }

            if (_levelText != null)
            {
                _levelText.text = $"Level: {level}";
            }
        }

        public void SetGold(int gold)
        {
            if (_goldText != null)
            {
                _goldText.text = $"Gold: {gold}";
            }
        }

        public void SetSeed(int seed)
        {
            if (_seedText != null)
            {
                _seedText.text = $"Seed: {seed}";
            }
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("HudCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("HudPanel");
            panel.transform.SetParent(canvasObject.transform, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.4f);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(220f, 140f);
            panelRect.anchoredPosition = new Vector2(10f, -10f);

            _hpText = CreateHudText(panel.transform, "HpText", new Vector2(10f, -10f));
            _xpText = CreateHudText(panel.transform, "XpText", new Vector2(10f, -35f));
            _levelText = CreateHudText(panel.transform, "LevelText", new Vector2(10f, -60f));
            _goldText = CreateHudText(panel.transform, "GoldText", new Vector2(10f, -85f));
            _seedText = CreateHudText(panel.transform, "SeedText", new Vector2(10f, -110f));

            var restartButton = _uiFactory.CreateButton(panel.transform, "RestartButton", "Restart", 14, new Color(0.2f, 0.2f, 0.2f, 0.9f), Color.white);
            var buttonRect = restartButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.sizeDelta = new Vector2(80f, 28f);
            buttonRect.anchoredPosition = new Vector2(-10f, -10f);
            restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        }

        private Text CreateHudText(Transform parent, string name, Vector2 anchoredPosition)
        {
            var text = _uiFactory.CreateText(parent, name, 14, TextAnchor.UpperLeft, Color.white);
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(200f, 20f);
            return text;
        }
    }
}
