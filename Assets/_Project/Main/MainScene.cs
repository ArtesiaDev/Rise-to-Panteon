using Framework.Scenes;

namespace Entry.Scenes
{
    /// <summary>
    /// Основная сцена игры — ECS, геймплей.
    /// Protected: не выгружается при переключении сцен.
    /// </summary>
    public sealed class MainScene : IScene
    {
        public string Name => "Main";
        public bool IsProtected => true;
        public bool IsAddressableScene => false;
    }
}
