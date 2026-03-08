using System;
using Cheats.Views;
using Dev.Views;
using TMPro;

namespace Dev
{
    public interface ICheat
    {
        void Initialize(ICheatViewBuilder view);
    }

    public interface ICheatViewBuilder
    {
        public ICheatGroupView CreateGroup(string caption = null);
    }

    public interface ICheatGroupView
    {
        public CheatGroupView AddCheatButton(string text, Action action, bool autoHide = true);
        public CheatGroupView AddCheatField(string text, TMP_InputField.ContentType contentType, Action<CheatFieldLineView> action, bool autoHide = true);
        public CheatGroupView AddCheatFieldAsInt(string text, Action<int> action, bool autoHide = true);
        public CheatGroupView AddCheatToggle(string text, bool isOn, Action<bool> action);
        public CheatGroupView AddCheatFieldWithResult(string text, Action<string, TMP_Text> action);
    }
}