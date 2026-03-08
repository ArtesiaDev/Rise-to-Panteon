namespace Framework.Scenes
{
    /// <summary>
    /// Описание сцены для загрузчика.
    /// </summary>
    public interface IScene
    {
        /// <summary>Имя Unity-сцены (совпадает с файлом).</summary>
        string Name { get; }

        /// <summary>Не выгружается при переключении сцен (Single mode).</summary>
        bool IsProtected { get; }

        /// <summary>true = загружается через Addressables, false = через SceneManager.</summary>
        bool IsAddressableScene { get; }
    }
}
