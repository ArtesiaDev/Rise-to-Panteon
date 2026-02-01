using UnityEngine;
using UnityEngine.UI;

namespace RuntimeRoguelike
{
    public class HudController : MonoBehaviour
    {
        private RunController runController;
        private PlayerStats stats;
        private Health health;
        private RunStats runStats;

        private Text hpText;
        private Text xpText;
        private Text levelText;
        private Text goldText;
        private Text seedText;

        public void Initialize(RunController controller, PlayerStats playerStats, Health playerHealth, RunStats statsData)
        {
            runController = controller;
            stats = playerStats;
            health = playerHealth;
            runStats = statsData;
            BuildUI();
        }

        private void Update()
        {
            if (health != null)
            {
                hpText.text = $"HP: {health.CurrentHp}/{health.MaxHp}";
            }

            if (stats != null)
            {
                xpText.text = $"XP: {stats.Xp}/{stats.XpToNextLevel}";
                levelText.text = $"Level: {stats.Level}";
                goldText.text = $"Gold: {stats.Gold}";
            }

            if (runStats != null)
            {
                seedText.text = $"Seed: {runStats.Seed}";
            }
        }

        private void BuildUI()
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

            hpText = CreateHudText(panel.transform, "HPText", new Vector2(10f, -10f));
            xpText = CreateHudText(panel.transform, "XPText", new Vector2(10f, -35f));
            levelText = CreateHudText(panel.transform, "LevelText", new Vector2(10f, -60f));
            goldText = CreateHudText(panel.transform, "GoldText", new Vector2(10f, -85f));
            seedText = CreateHudText(panel.transform, "SeedText", new Vector2(10f, -110f));

            var restartButton = UIRuntimeUtils.CreateButton(panel.transform, "RestartButton", "Restart", 14, new Color(0.2f, 0.2f, 0.2f, 0.9f), Color.white);
            var buttonRect = restartButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.sizeDelta = new Vector2(80f, 28f);
            buttonRect.anchoredPosition = new Vector2(-10f, -10f);
            restartButton.onClick.AddListener(() => runController.RestartRun());
        }

        private static Text CreateHudText(Transform parent, string name, Vector2 anchoredPosition)
        {
            var text = UIRuntimeUtils.CreateText(parent, name, 14, TextAnchor.UpperLeft, Color.white);
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
