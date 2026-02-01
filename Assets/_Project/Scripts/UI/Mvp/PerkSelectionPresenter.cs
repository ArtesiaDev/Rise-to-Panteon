using RuntimeRoguelike;
using RuntimeRoguelike.Ecs;

namespace RuntimeRoguelike.UI.Mvp
{
    public class PerkSelectionPresenter
    {
        private PerkSelectionModel _model;
        private PerkSelectionView _view;
        private EcsCommandService _commands;

        public void Bind(PerkSelectionModel model, PerkSelectionView view, EcsCommandService commands)
        {
            _model = model;
            _view = view;
            _commands = commands;

            _model.OnChanged += HandleModelChanged;
            _view.OnPerkSelected += HandlePerkSelected;
            HandleModelChanged();
        }

        public void Unbind()
        {
            if (_model != null)
            {
                _model.OnChanged -= HandleModelChanged;
            }

            if (_view != null)
            {
                _view.OnPerkSelected -= HandlePerkSelected;
            }
        }

        private void HandleModelChanged()
        {
            if (_model == null || _view == null)
            {
                return;
            }

            _view.SetOffer(_model.Options, _model.IsVisible);
        }

        private void HandlePerkSelected(PerkDefinition perk)
        {
            _commands?.SelectPerk(perk);
        }
    }
}
