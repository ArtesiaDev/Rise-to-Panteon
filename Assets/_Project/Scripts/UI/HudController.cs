using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace RuntimeRoguelike
{
    public class HudController : MonoBehaviour
    {
        private UiFactory _uiFactory;

        private RunController _runController;
        private PlayerStats _stats;
        private Health _health;
        private RunStats _runStats;

        private Text _hpText;
        private Text _xpText;
        private Text _levelText;
        private Text _goldText;
        private Text _seedText;

        [Inject]
        private void Construct(UiFactory uiFactory)
        {
            _uiFactory = uiFactory;
        }

        private void Update()
        {
            if (_hpText == null || _xpText == null || _levelText == null || _goldText == null || _seedText == null)
            {
                return;
            }

            if (_health != null)
            {
                _hpText.text = $"HP: {_health.CurrentHp}/{_health.MaxHp}";
            }

            if (_stats != null)
            {
                _xpText.text = $"XP: {_stats.Xp}/{_stats.XpToNextLevel}";
                _levelText.text = $"Level: {_stats.Level}";
                _goldText.text = $"Gold: {_stats.Gold}";
            }

            if (_runStats != null)
            {
                _seedText.text = $"Seed: {_runStats.Seed}";
            }
        }

        public void Initialize(RunController controller, PlayerStats playerStats, Health playerHealth, RunStats statsData)
        {
            _runController = controller;
            _stats = playerStats;
            _health = playerHealth;
            _runStats = statsData;
            BuildUi();
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("HUDCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("HUDPanel");
            panel.transform.SetParent(canvasObject.transform, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.4f);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(220f, 140f);
            panelRect.anchoredPosition = new Vector2(10f, -10f);

            _hpText = CreateHudText(panel.transform, "HPText", new Vector2(10f, -10f));
            _xpText = CreateHudText(panel.transform, "XPText", new Vector2(10f, -35f));
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
            restartButton.onClick.AddListener(() => _runController.RestartRun());
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
