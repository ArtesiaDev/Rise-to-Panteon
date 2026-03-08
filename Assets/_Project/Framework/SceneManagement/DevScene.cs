namespace Framework.Scenes
{
    /// <summary>
    /// Dev-сцена с читами. Protected: не выгружается.
    /// Загружается только в Editor и Development Build.
    /// </summary>
    public sealed class DevScene : IScene
    {
        public string Name => "DevScene";
        public bool IsProtected => true;
        public bool IsAddressableScene => false;
    }
}
