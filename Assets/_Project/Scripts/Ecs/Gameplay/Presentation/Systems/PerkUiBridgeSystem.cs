using RuntimeRoguelike.UI.Mvp;

namespace RuntimeRoguelike.Ecs
{
    public class PerkUiBridgeSystem : IEcsUpdateSystem
    {
        private readonly PerkSelectionModel _model;

        public PerkUiBridgeSystem(PerkSelectionModel model)
        {
            _model = model;
        }

        public void Update(EcsWorld world, EcsCommandBuffer commandBuffer, float deltaTime)
        {
            if (world.TryGetResource<PerkOfferState>(out var offer))
            {
                _model.SetOffer(offer.Options, offer.IsVisible);
                return;
            }

            _model.SetOffer(null, false);
        }
    }
}
