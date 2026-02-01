using RuntimeRoguelike.Ecs;

namespace RuntimeRoguelike.UI.Mvp
{
    public class HudPresenter
    {
        private HudModel _model;
        private HudView _view;
        private EcsCommandService _commands;

        public void Bind(HudModel model, HudView view, EcsCommandService commands)
        {
            _model = model;
            _view = view;
            _commands = commands;

            _model.OnChanged += HandleModelChanged;
            _view.OnRestartClicked += HandleRestartClicked;
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
                _view.OnRestartClicked -= HandleRestartClicked;
            }
        }

        private void HandleModelChanged()
        {
            if (_model == null || _view == null)
            {
                return;
            }

            _view.SetHp(_model.CurrentHp, _model.MaxHp);
            _view.SetProgress(_model.Xp, _model.XpToNext, _model.Level);
            _view.SetGold(_model.Gold);
            _view.SetSeed(_model.Seed);
        }

        private void HandleRestartClicked()
        {
            _commands?.RequestRestart();
        }
    }
}
