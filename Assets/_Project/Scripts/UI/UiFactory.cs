using UnityEngine;
using UnityEngine.UI;

namespace RuntimeRoguelike
{
    public class UiFactory
    {
        private const string LEGACY_RUNTIME_FONT_PATH = "LegacyRuntime.ttf";
        private const string ARIAL_FONT_PATH = "Arial.ttf";

        private Font _defaultFont;

        public Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public Button CreateButton(Transform parent, string name, string label, int fontSize, Color background, Color textColor)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = background;
            var button = buttonObject.AddComponent<Button>();

            var labelText = CreateText(buttonObject.transform, "Label", fontSize, TextAnchor.MiddleCenter, textColor);
            labelText.text = label;
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        private Font GetDefaultFont()
        {
            if (_defaultFont == null)
            {
                // Unity 6+: Arial.ttf is not a valid built-in font anymore (use LegacyRuntime.ttf).
                // Keep a fallback for older Unity versions/projects.
                _defaultFont = Resources.GetBuiltinResource<Font>(LEGACY_RUNTIME_FONT_PATH);
                if (_defaultFont == null)
                {
                    _defaultFont = Resources.GetBuiltinResource<Font>(ARIAL_FONT_PATH);
                }
            }

            return _defaultFont;
        }
    }
}
