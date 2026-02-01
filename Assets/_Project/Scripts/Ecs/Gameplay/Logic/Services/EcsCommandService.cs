using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class EcsCommandService
    {
        private EcsWorld _world;

        public void SetWorld(EcsWorld world)
        {
            _world = world;
        }

        public void RequestRestart()
        {
            if (_world == null)
            {
                return;
            }

            if (_world.TryGetResource<RunCommands>(out var commands))
            {
                commands.RestartRequested = true;
            }
        }

        public void SelectPerk(PerkDefinition perk)
        {
            if (_world == null)
            {
                return;
            }

            if (_world.TryGetResource<RunCommands>(out var commands))
            {
                commands.ChosenPerk = perk;
                commands.PerkChosen = true;
            }
        }
    }
}
