using UnityEngine;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Configs
{
    [CreateAssetMenu(fileName = "SpriteFactoryConfig", menuName = "Configs/Rendering/SpriteFactoryConfig", order = 10)]
    public class SpriteFactoryConfig : ScriptableObject
    {
        [field: SerializeField] public int SpriteSize { get; private set; } = 16;
        [field: SerializeField] public SpritePalette DefaultPalette { get; private set; } = new SpritePalette
        {
            Primary = new Color32(80, 80, 80, 255),
            Secondary = new Color32(110, 110, 110, 255),
            Accent = new Color32(140, 140, 140, 255)
        };
        [field: SerializeField] public SpritePaletteEntry[] Palettes { get; private set; } =
        {
            new SpritePaletteEntry
            {
                Key = SpriteKey.Floor,
                Palette = new SpritePalette
                {
                    Primary = new Color32(65, 65, 70, 255),
                    Secondary = new Color32(75, 75, 80, 255),
                    Accent = new Color32(90, 90, 95, 255)
                }
            },
            new SpritePaletteEntry
            {
                Key = SpriteKey.Wall,
                Palette = new SpritePalette
                {
                    Primary = new Color32(25, 25, 28, 255),
                    Secondary = new Color32(45, 45, 50, 255),
                    Accent = new Color32(65, 65, 70, 255)
                }
            },
            new SpritePaletteEntry
            {
                Key = SpriteKey.Enemy,
                Palette = new SpritePalette
                {
                    Primary = new Color32(120, 25, 25, 255),
                    Secondary = new Color32(200, 60, 60, 255),
                    Accent = new Color32(240, 90, 90, 255)
                }
            },
            new SpritePaletteEntry
            {
                Key = SpriteKey.Loot,
                Palette = new SpritePalette
                {
                    Primary = new Color32(120, 90, 20, 255),
                    Secondary = new Color32(230, 190, 60, 255),
                    Accent = new Color32(255, 230, 120, 255)
                }
            },
            new SpritePaletteEntry
            {
                Key = SpriteKey.Trap,
                Palette = new SpritePalette
                {
                    Primary = new Color32(80, 10, 10, 255),
                    Secondary = new Color32(200, 40, 40, 255),
                    Accent = new Color32(240, 80, 80, 255)
                }
            },
            new SpritePaletteEntry
            {
                Key = SpriteKey.Player,
                Palette = new SpritePalette
                {
                    Primary = new Color32(20, 80, 140, 255),
                    Secondary = new Color32(80, 160, 220, 255),
                    Accent = new Color32(140, 200, 255, 255)
                }
            },
            new SpritePaletteEntry
            {
                Key = SpriteKey.Hazard,
                Palette = new SpritePalette
                {
                    Primary = new Color32(60, 20, 80, 255),
                    Secondary = new Color32(140, 60, 180, 255),
                    Accent = new Color32(200, 120, 230, 255)
                }
            },
            new SpritePaletteEntry
            {
                Key = SpriteKey.Obstacle,
                Palette = new SpritePalette
                {
                    Primary = new Color32(50, 55, 45, 255),
                    Secondary = new Color32(90, 95, 75, 255),
                    Accent = new Color32(120, 125, 100, 255)
                }
            }
        };
    }
}
