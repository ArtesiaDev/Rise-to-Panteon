using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    public class InputBridge : MonoBehaviour
    {
        [SerializeField] private KeyCode _attackKey = KeyCode.Space;
        [SerializeField] private KeyCode _restartKey = KeyCode.R;
        [SerializeField] private KeyCode _toggleGizmosKey = KeyCode.G;

        private EntityManager _entityManager;
        private EntityQuery _inputQuery;
        private World _world;

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null)
            {
                enabled = false;
                return;
            }

            _entityManager = _world.EntityManager;
            _inputQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<InputState>());
        }

        private void Update()
        {
            if (_world is not { IsCreated: true })
                return;

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

            var attackPressed = Input.GetKeyDown(_attackKey);
            var restartPressed = Input.GetKeyDown(_restartKey);
            var toggleGizmos = Input.GetKeyDown(_toggleGizmosKey);

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
