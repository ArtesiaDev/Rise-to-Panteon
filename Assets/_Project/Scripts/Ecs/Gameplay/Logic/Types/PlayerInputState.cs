namespace RuntimeRoguelike.Ecs
{
    public struct PlayerInputState
    {
        public Int2 MoveDirection;
        public bool AttackPressed;
        public bool RestartPressed;
        public bool ToggleGizmosPressed;
        public bool TeleportPressed;
    }
}
