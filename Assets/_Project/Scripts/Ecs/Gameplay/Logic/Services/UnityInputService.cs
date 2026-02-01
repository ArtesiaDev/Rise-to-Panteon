using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike.Ecs
{
    public class UnityInputService : IInputService
    {
        private readonly RunConfig _runConfig;
        private readonly PlayerAttackConfig _attackConfig;
        private readonly DevToolsConfig _devToolsConfig;

        public UnityInputService(RunConfig runConfig, PlayerAttackConfig attackConfig, DevToolsConfig devToolsConfig)
        {
            _runConfig = runConfig;
            _attackConfig = attackConfig;
            _devToolsConfig = devToolsConfig;
        }

        public PlayerInputState Read()
        {
            var state = new PlayerInputState
            {
                MoveDirection = ReadMoveDirection(),
                AttackPressed = _attackConfig.AttackKey != KeyCode.None && Input.GetKeyDown(_attackConfig.AttackKey),
                RestartPressed = _runConfig.RestartKey != KeyCode.None && Input.GetKeyDown(_runConfig.RestartKey),
                ToggleGizmosPressed = _devToolsConfig.Enabled && Input.GetKeyDown(_devToolsConfig.ToggleGizmosKey),
                TeleportPressed = _devToolsConfig.Enabled && Input.GetKeyDown(_devToolsConfig.TeleportKey)
            };

            if (_devToolsConfig.Enabled && Input.GetKeyDown(_devToolsConfig.RestartKey))
            {
                state.RestartPressed = true;
            }

            return state;
        }

        private Int2 ReadMoveDirection()
        {
            var x = 0;
            var y = 0;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                x -= 1;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                x += 1;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                y += 1;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                y -= 1;
            }

            if (x != 0)
            {
                return new Int2(x, 0);
            }

            if (y != 0)
            {
                return new Int2(0, y);
            }

            return Int2.Zero;
        }
    }
}
