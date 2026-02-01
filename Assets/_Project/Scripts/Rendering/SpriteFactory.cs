using System.Collections.Generic;
using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class SpriteFactory
    {
        private readonly SpriteFactoryConfig _config;
        private readonly Dictionary<SpriteKey, Sprite> _cache = new Dictionary<SpriteKey, Sprite>();
        private readonly Dictionary<SpriteKey, SpritePalette> _paletteLookup = new Dictionary<SpriteKey, SpritePalette>();

        public SpriteFactory(SpriteFactoryConfig config)
        {
            _config = config;
            BuildPaletteLookup();
        }

        public Sprite GetSprite(SpriteKey key)
        {
            if (_cache.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            var size = Mathf.Max(2, _config.SpriteSize);
            var texture = CreateTexture(key, size);
            var rect = new Rect(0, 0, size, size);
            sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), size);
            sprite.name = $"Runtime_{key}";
            _cache[key] = sprite;
            return sprite;
        }

        private void BuildPaletteLookup()
        {
            if (_config.Palettes == null)
            {
                return;
            }

            for (var i = 0; i < _config.Palettes.Length; i++)
            {
                var entry = _config.Palettes[i];
                _paletteLookup[entry.Key] = entry.Palette;
            }
        }

        private Texture2D CreateTexture(SpriteKey key, int size)
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
                pixels[i] = palette.Primary;
            }

            ApplyPattern(key, size, pixels, palette);

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private void ApplyPattern(SpriteKey key, int size, Color32[] pixels, SpritePalette palette)
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
                                pixels[index] = palette.Secondary;
                            }
                            break;
                        case SpriteKey.Wall:
                            if (IsBorder(x, y, size))
                            {
                                pixels[index] = palette.Secondary;
                            }
                            break;
                        case SpriteKey.Enemy:
                            if (x == y || x == size - 1 - y)
                            {
                                pixels[index] = palette.Secondary;
                            }
                            break;
                        case SpriteKey.Loot:
                            if (x == size / 2 || y == size / 2)
                            {
                                pixels[index] = palette.Secondary;
                            }
                            else if ((x + y) % 7 == 0)
                            {
                                pixels[index] = palette.Accent;
                            }
                            break;
                        case SpriteKey.Trap:
                            if (y == size - 2 || (y == size - 3 && x % 2 == 0))
                            {
                                pixels[index] = palette.Secondary;
                            }
                            break;
                        case SpriteKey.Player:
                            if (IsBorder(x, y, size) || x == size / 2 || y == size / 2)
                            {
                                pixels[index] = palette.Secondary;
                            }
                            break;
                        case SpriteKey.Hazard:
                            if ((x + y) % 3 == 0)
                            {
                                pixels[index] = palette.Secondary;
                            }
                            break;
                        case SpriteKey.Obstacle:
                            if (IsBorder(x, y, size))
                            {
                                pixels[index] = palette.Secondary;
                            }
                            else if (x % 3 == 0 && y % 3 == 0)
                            {
                                pixels[index] = palette.Accent;
                            }
                            break;
                    }
                }
            }
        }

        private bool IsBorder(int x, int y, int size)
        {
            return x == 0 || y == 0 || x == size - 1 || y == size - 1;
        }

        private SpritePalette GetPalette(SpriteKey key)
        {
            if (_paletteLookup.TryGetValue(key, out var palette))
            {
                return palette;
            }

            return _config.DefaultPalette;
        }
    }
}
