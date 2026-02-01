using System;
using RuntimeRoguelike;

namespace RuntimeRoguelike.UI.Mvp
{
    public class PerkSelectionModel
    {
        public event Action OnChanged;

        public bool IsVisible { get; private set; }
        public PerkDefinition[] Options { get; private set; } = new PerkDefinition[0];

        public void SetOffer(PerkDefinition[] options, bool isVisible)
        {
            Options = options ?? new PerkDefinition[0];
            IsVisible = isVisible;
            OnChanged?.Invoke();
        }
    }
}
