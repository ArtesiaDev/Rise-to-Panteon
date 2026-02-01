using UnityEngine;

namespace RuntimeRoguelike
{
    public class HazardSystem : MonoBehaviour
    {
        [SerializeField] private float spikeTickInterval = 0.8f;
        [SerializeField] private int spikeDamage = 1;
        [SerializeField] private float poisonDuration = 4f;
        [SerializeField] private float poisonDps = 1.2f;

        private MapGrid grid;
        private PlayerController player;
        private Health health;
        private StatusEffects statusEffects;
        private HazardType currentHazard = HazardType.None;
        private float nextSpikeTick;

        public void Initialize(MapGrid mapGrid, PlayerController playerController)
        {
            grid = mapGrid;
            player = playerController;
            health = playerController.GetComponent<Health>();
            statusEffects = playerController.GetComponent<StatusEffects>();
            playerController.OnCellChanged += HandleCellChanged;
        }

        private void Update()
        {
            if (currentHazard != HazardType.Spike || health == null)
            {
                return;
            }

            if (Time.time >= nextSpikeTick)
            {
                nextSpikeTick = Time.time + spikeTickInterval;
                health.TakeDamage(spikeDamage);
            }
        }

        private void HandleCellChanged(Vector2Int cell)
        {
            var hazard = grid.Get(cell).hazard;
            currentHazard = hazard;
            if (hazard == HazardType.Spike)
            {
                nextSpikeTick = Time.time + spikeTickInterval;
                health?.TakeDamage(spikeDamage);
            }
            else if (hazard == HazardType.Poison)
            {
                statusEffects?.ApplyPoison(poisonDuration, poisonDps);
            }
        }
    }
}
