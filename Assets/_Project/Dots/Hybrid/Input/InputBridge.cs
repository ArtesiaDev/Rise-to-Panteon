using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class InputBridge : MonoBehaviour
    {
        [SerializeField] private KeyCode attackKey = KeyCode.Space;
        [SerializeField] private KeyCode restartKey = KeyCode.R;
        [SerializeField] private KeyCode toggleGizmosKey = KeyCode.G;

        private EntityManager _entityManager;
        private EntityQuery _inputQuery;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = world.EntityManager;
            _inputQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<InputState>());
        }

        private void OnDestroy()
        {
            if (_inputQuery.IsCreated)
            {
                _inputQuery.Dispose();
            }
        }

        private void Update()
        {
            if (!_inputQuery.IsCreated)
            {
                return;
            }

            var move = int2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                move = new int2(0, 1);
            }
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                move = new int2(0, -1);
            }
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                move = new int2(-1, 0);
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                move = new int2(1, 0);
            }

            var attackPressed = Input.GetKeyDown(attackKey);
            var restartPressed = Input.GetKeyDown(restartKey);
            var toggleGizmos = Input.GetKeyDown(toggleGizmosKey);

            if (_inputQuery.IsEmpty)
            {
                var entity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(entity, new InputState
                {
                    MoveDir = move,
                    AttackPressed = attackPressed,
                    RestartPressed = restartPressed,
                    ToggleGizmos = toggleGizmos
                });
                return;
            }

            var inputEntity = _inputQuery.GetSingletonEntity();
            var input = _entityManager.GetComponentData<InputState>(inputEntity);
            input.MoveDir = move;
            input.AttackPressed |= attackPressed;
            input.RestartPressed |= restartPressed;
            input.ToggleGizmos |= toggleGizmos;
            _entityManager.SetComponentData(inputEntity, input);
        }
    }
}
