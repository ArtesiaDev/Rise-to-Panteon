using System.Collections.Generic;
using UnityEngine;

namespace RuntimeRoguelike
{
    public enum SpriteKey
    {
        Floor,
        Wall,
        Enemy,
        Loot,
        Trap,
        Player,
        Hazard,
        Obstacle
    }

    public static class SpriteFactory
    {
        private const int Size = 16;
        private static readonly Dictionary<SpriteKey, Sprite> Cache = new Dictionary<SpriteKey, Sprite>();

        public static Sprite Get(SpriteKey key)
        {
            if (Cache.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            var texture = CreateTexture(key, Size);
            var rect = new Rect(0, 0, Size, Size);
            sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), Size);
            sprite.name = $"Runtime_{key}";
            Cache[key] = sprite;
            return sprite;
        }

        private static Texture2D CreateTexture(SpriteKey key, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            var palette = GetPalette(key);

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = palette.primary;
            }

            ApplyPattern(key, size, pixels, palette);

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static void ApplyPattern(SpriteKey key, int size, Color32[] pixels, Palette palette)
        {
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;

                    switch (key)
                    {
                        case SpriteKey.Floor:
                            if ((x + y) % 5 == 0)
                            {
                                pixels[index] = palette.secondary;
                            }
                            break;
                        case SpriteKey.Wall:
                            if (IsBorder(x, y, size))
                            {
                                pixels[index] = palette.secondary;
                            }
                            break;
                        case SpriteKey.Enemy:
                            if (x == y || x == size - 1 - y)
                            {
                                pixels[index] = palette.secondary;
                            }
                            break;
                        case SpriteKey.Loot:
                            if (x == size / 2 || y == size / 2)
                            {
                                pixels[index] = palette.secondary;
                            }
                            else if ((x + y) % 7 == 0)
                            {
                                pixels[index] = palette.accent;
                            }
                            break;
                        case SpriteKey.Trap:
                            if (y == size - 2 || (y == size - 3 && x % 2 == 0))
                            {
                                pixels[index] = palette.secondary;
                            }
                            break;
                        case SpriteKey.Player:
                            if (IsBorder(x, y, size) || x == size / 2 || y == size / 2)
                            {
                                pixels[index] = palette.secondary;
                            }
                            break;
                        case SpriteKey.Hazard:
                            if ((x + y) % 3 == 0)
                            {
                                pixels[index] = palette.secondary;
                            }
                            break;
                        case SpriteKey.Obstacle:
                            if (IsBorder(x, y, size))
                            {
                                pixels[index] = palette.secondary;
                            }
                            else if (x % 3 == 0 && y % 3 == 0)
                            {
                                pixels[index] = palette.accent;
                            }
                            break;
                    }
                }
            }
        }

        private static bool IsBorder(int x, int y, int size)
        {
            return x == 0 || y == 0 || x == size - 1 || y == size - 1;
        }

        private static Palette GetPalette(SpriteKey key)
        {
            switch (key)
            {
                case SpriteKey.Floor:
                    return new Palette(new Color32(65, 65, 70, 255), new Color32(75, 75, 80, 255), new Color32(90, 90, 95, 255));
                case SpriteKey.Wall:
                    return new Palette(new Color32(25, 25, 28, 255), new Color32(45, 45, 50, 255), new Color32(65, 65, 70, 255));
                case SpriteKey.Enemy:
                    return new Palette(new Color32(120, 25, 25, 255), new Color32(200, 60, 60, 255), new Color32(240, 90, 90, 255));
                case SpriteKey.Loot:
                    return new Palette(new Color32(120, 90, 20, 255), new Color32(230, 190, 60, 255), new Color32(255, 230, 120, 255));
                case SpriteKey.Trap:
                    return new Palette(new Color32(80, 10, 10, 255), new Color32(200, 40, 40, 255), new Color32(240, 80, 80, 255));
                case SpriteKey.Player:
                    return new Palette(new Color32(20, 80, 140, 255), new Color32(80, 160, 220, 255), new Color32(140, 200, 255, 255));
                case SpriteKey.Hazard:
                    return new Palette(new Color32(60, 20, 80, 255), new Color32(140, 60, 180, 255), new Color32(200, 120, 230, 255));
                case SpriteKey.Obstacle:
                    return new Palette(new Color32(50, 55, 45, 255), new Color32(90, 95, 75, 255), new Color32(120, 125, 100, 255));
                default:
                    return new Palette(new Color32(80, 80, 80, 255), new Color32(110, 110, 110, 255), new Color32(140, 140, 140, 255));
            }
        }

        private readonly struct Palette
        {
            public readonly Color32 primary;
            public readonly Color32 secondary;
            public readonly Color32 accent;

            public Palette(Color32 primary, Color32 secondary, Color32 accent)
            {
                this.primary = primary;
                this.secondary = secondary;
                this.accent = accent;
            }
        }
    }
}
