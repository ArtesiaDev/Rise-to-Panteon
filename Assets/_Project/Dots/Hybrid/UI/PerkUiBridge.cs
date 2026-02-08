using RuntimeRoguelike.Dots;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class PerkUiBridge : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button[] buttons;
        [SerializeField] private Text[] titleTexts;
        [SerializeField] private Text[] valueTexts;

        private EntityManager _entityManager;
        private EntityQuery _offerQuery;
        private EntityQuery _commandQuery;
        private bool _initialized;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = world.EntityManager;
            _offerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<PerkOfferState>());
            _commandQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<RunCommand>());

            HookButtons();
        }

        private void OnDestroy()
        {
            if (_offerQuery.IsCreated)
            {
                _offerQuery.Dispose();
            }

            if (_commandQuery.IsCreated)
            {
                _commandQuery.Dispose();
            }
        }

        private void Update()
        {
            if (_offerQuery.IsEmpty)
            {
                SetVisible(false);
                return;
            }

            var offer = _offerQuery.GetSingleton<PerkOfferState>();
            if (!offer.IsVisible)
            {
                SetVisible(false);
                return;
            }

            var singletonEntity = _offerQuery.GetSingletonEntity();
            var options = _entityManager.GetBuffer<PerkOption>(singletonEntity);
            var perks = _entityManager.GetBuffer<PerkData>(singletonEntity);

            SetVisible(true);

            for (var i = 0; i < buttons.Length; i++)
            {
                if (i >= options.Length || i >= perks.Length)
                {
                    SetButton(i, false, string.Empty, string.Empty);
                    continue;
                }

                var perk = perks[options[i].PerkId];
                var title = perk.Type.ToString();
                var value = FormatValue(perk);
                SetButton(i, true, title, value);
            }
        }

        private void HookButtons()
        {
            if (_initialized || buttons == null)
            {
                return;
            }

            for (var i = 0; i < buttons.Length; i++)
            {
                var index = i;
                if (buttons[i] != null)
                {
                    buttons[i].onClick.AddListener(() => OnPerkSelected(index));
                }
            }

            _initialized = true;
        }

        private void OnPerkSelected(int index)
        {
            if (_commandQuery.IsEmpty)
            {
                return;
            }

            var commandEntity = _commandQuery.GetSingletonEntity();
            var command = _entityManager.GetComponentData<RunCommand>(commandEntity);
            command.PerkChosen = true;
            command.ChosenPerkIndex = index;
            _entityManager.SetComponentData(commandEntity, command);
        }

        private void SetVisible(bool visible)
        {
            if (panel != null)
            {
                panel.SetActive(visible);
            }
        }

        private void SetButton(int index, bool active, string title, string value)
        {
            if (buttons == null || index >= buttons.Length || buttons[index] == null)
            {
                return;
            }

            buttons[index].gameObject.SetActive(active);

            if (titleTexts != null && index < titleTexts.Length && titleTexts[index] != null)
            {
                titleTexts[index].text = title;
            }

            if (valueTexts != null && index < valueTexts.Length && valueTexts[index] != null)
            {
                valueTexts[index].text = value;
            }
        }

        private static string FormatValue(PerkData perk)
        {
            return perk.Type switch
            {
                PerkType.MaxHp => $"+{Mathf.RoundToInt(perk.Value)} Max HP",
                PerkType.Damage => $"+{Mathf.RoundToInt(perk.Value)} Damage",
                PerkType.MoveSpeed => $"+{perk.Value:0.0} Move Speed",
                _ => perk.Value.ToString("0.0")
            };
        }
    }
}
