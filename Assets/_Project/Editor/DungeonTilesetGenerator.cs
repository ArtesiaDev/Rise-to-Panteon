using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RuntimeRoguelike.Editor
{
    /// <summary>
    /// Процедурный генератор pixel art тайлсета для подземелья.
    /// Палитра и стиль — из референсных скриншотов (Docs/References/MapGeneration/).
    /// Запуск: Tools → Generate Dungeon Tileset.
    /// </summary>
    public static class DungeonTilesetGenerator
    {
        private const int S = 16; // Размер тайла в пикселях
        private const string BasePath = "Assets/_Project/Art/Tiles";

        // ─── Палитра (тёмное подземелье из референса) ───
        private static readonly Color32 FloorBase  = new(45, 48, 62, 255);
        private static readonly Color32 FloorGrout = new(30, 32, 42, 255);
        private static readonly Color32 FloorCrack = new(36, 38, 50, 255);
        private static readonly Color32 FloorHigh  = new(54, 57, 72, 255);

        private static readonly Color32 WallInner  = new(22, 20, 30, 255);
        private static readonly Color32 WallFace   = new(52, 48, 44, 255);
        private static readonly Color32 WallHigh   = new(70, 64, 56, 255);
        private static readonly Color32 WallShade  = new(16, 14, 20, 255);

        private static readonly Color32 DoorFrame  = new(60, 50, 40, 255);
        private static readonly Color32 DoorHigh   = new(76, 66, 52, 255);
        private static readonly Color32 DoorShade  = new(30, 26, 20, 255);

        // ─── Точка входа ───

        [MenuItem("Tools/Generate Dungeon Tileset")]
        public static void Generate()
        {
            EnsureFolder(BasePath);
            EnsureFolder(BasePath + "/Floor");
            EnsureFolder(BasePath + "/Walls");
            EnsureFolder(BasePath + "/Decor");

            // Полы (4 варианта)
            var floors = new Tile[4];
            floors[0] = SaveTile(GenFloorPlain(),  "Floor", "floor_0");
            floors[1] = SaveTile(GenFloorCrack(),  "Floor", "floor_1");
            floors[2] = SaveTile(GenFloorCross(),  "Floor", "floor_2");
            floors[3] = SaveTile(GenFloorGrate(),  "Floor", "floor_3");

            // Стены (16 bitmask-вариантов)
            var walls = new Tile[16];
            for (var mask = 0; mask < 16; mask++)
                walls[mask] = SaveTile(GenWall(mask), "Walls", $"wall_{mask:D2}");

            // Декор (дверные проёмы)
            var doorH = SaveTile(GenDoorH(), "Decor", "door_h");
            var doorV = SaveTile(GenDoorV(), "Decor", "door_v");

            // Авто-привязка к TilemapRenderBridge в сцене
            AutoAssign(floors, walls, doorH, doorV);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TilesetGenerator] {floors.Length} floor + {walls.Length} wall + 2 decor tiles → {BasePath}");
        }

        // ─── Генерация полов ───

        private static Texture2D GenFloorPlain()
        {
            var tex = NewTex();
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var c = AddNoise(FloorBase, x, y, 0);
                // Швы по краям тайла
                if (x == 0 || y == 0) c = Darken(c, 0.65f);
                else if (x == 1 || y == 1) c = Darken(c, 0.82f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D GenFloorCrack()
        {
            var tex = NewTex();
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var c = AddNoise(FloorBase, x, y, 1);
                if (x == 0 || y == 0) c = Darken(c, 0.65f);
                else if (x == 1 || y == 1) c = Darken(c, 0.82f);
                // Горизонтальная трещина
                if (y == 7) c = FloorCrack;
                if (y == 8 && (x + Hash(x, y, 1)) % 3 != 0) c = FloorCrack;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D GenFloorCross()
        {
            var tex = NewTex();
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var c = AddNoise(FloorBase, x, y, 2);
                if (x == 0 || y == 0) c = Darken(c, 0.65f);
                else if (x == 1 || y == 1) c = Darken(c, 0.82f);
                // Крестообразный шов
                if ((x == 7 || x == 8) && y >= 3 && y <= 12) c = FloorGrout;
                if ((y == 7 || y == 8) && x >= 3 && x <= 12) c = FloorGrout;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D GenFloorGrate()
        {
            var tex = NewTex();
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var c = AddNoise(FloorBase, x, y, 3);
                if (x == 0 || y == 0) c = Darken(c, 0.65f);
                else if (x == 1 || y == 1) c = Darken(c, 0.82f);
                // Решётка в центре 6x6
                if (x >= 5 && x <= 10 && y >= 5 && y <= 10)
                {
                    if ((x - 5) % 2 == 0 || (y - 5) % 2 == 0)
                        c = FloorGrout;
                    else
                        c = Darken(FloorBase, 0.55f);
                }
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        // ─── Генерация стен (bitmask: N=1, E=2, S=4, W=8) ───

        private static Texture2D GenWall(int mask)
        {
            var hasN = (mask & 1) != 0;
            var hasE = (mask & 2) != 0;
            var hasS = (mask & 4) != 0;
            var hasW = (mask & 8) != 0;

            var tex = NewTex();
            const int face = 3; // Толщина грани в пикселях

            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                // Базовая заливка — тёмный интерьер стены
                Color32 c = AddNoise(WallInner, x, y, mask + 100);

                // Определяем, попадает ли пиксель в зону грани
                var inN = y >= S - face && !hasN;
                var inS = y < face && !hasS;
                var inE = x >= S - face && !hasE;
                var inW = x < face && !hasW;

                // Грань сверху (видимая, когда нет стены-соседа на севере)
                if (inN)
                {
                    var d = S - 1 - y; // 0 = крайний ряд, 1, 2 = внутрь
                    c = d == 0 ? WallHigh : d == 1 ? WallFace : Lerp32(WallFace, WallShade, 0.4f);
                }
                // Грань снизу
                if (inS)
                {
                    c = y == 0 ? WallShade : y == 1 ? WallFace : Lerp32(WallFace, WallHigh, 0.3f);
                }
                // Грань справа
                if (inE && !inN && !inS)
                {
                    var d = S - 1 - x;
                    c = d == 0 ? WallShade : d == 1 ? WallFace : Lerp32(WallFace, WallShade, 0.3f);
                }
                // Грань слева
                if (inW && !inN && !inS)
                {
                    c = x == 0 ? WallHigh : x == 1 ? WallFace : Lerp32(WallFace, WallShade, 0.3f);
                }

                // Углы: пересечение двух граней
                if (inN && inW) c = WallHigh;
                if (inN && inE) c = Lerp32(WallHigh, WallShade, 0.4f);
                if (inS && inW) c = Lerp32(WallFace, WallHigh, 0.3f);
                if (inS && inE) c = WallShade;

                // Добавляем шум для текстуры
                c = AddNoise(c, x, y, mask + 200);
                tex.SetPixel(x, y, c);
            }

            tex.Apply();
            return tex;
        }

        // ─── Генерация декора (двери) ───

        private static Texture2D GenDoorH()
        {
            // Горизонтальный дверной проём (стены сверху и снизу)
            var tex = NewTex();
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                Color32 c = new(0, 0, 0, 0); // Прозрачный по умолчанию
                // Верхняя арка (y=12..15)
                if (y >= 12)
                {
                    if (y == 15) c = DoorHigh;
                    else if (y == 14) c = DoorFrame;
                    else if (y == 13 && x >= 2 && x <= 13) c = DoorFrame;
                    else if (y == 12 && x >= 4 && x <= 11) c = DoorShade;
                }
                // Нижний порог (y=0..2)
                if (y <= 2)
                {
                    if (y == 0) c = DoorShade;
                    else if (y == 1 && x >= 1 && x <= 14) c = DoorFrame;
                    else if (y == 2 && x >= 3 && x <= 12) c = Lerp32(DoorFrame, DoorShade, 0.5f);
                }
                // Боковые столбы (x=0..1 и x=14..15, средняя часть)
                if (y >= 3 && y <= 11)
                {
                    if (x <= 1) c = x == 0 ? DoorHigh : DoorFrame;
                    else if (x >= 14) c = x == 15 ? DoorShade : DoorFrame;
                }
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D GenDoorV()
        {
            // Вертикальный дверной проём (стены слева и справа)
            var tex = NewTex();
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                Color32 c = new(0, 0, 0, 0);
                // Левая арка (x=0..3)
                if (x <= 3)
                {
                    if (x == 0) c = DoorHigh;
                    else if (x == 1) c = DoorFrame;
                    else if (x == 2 && y >= 2 && y <= 13) c = DoorFrame;
                    else if (x == 3 && y >= 4 && y <= 11) c = DoorShade;
                }
                // Правая арка (x=12..15)
                if (x >= 12)
                {
                    if (x == 15) c = DoorShade;
                    else if (x == 14) c = DoorFrame;
                    else if (x == 13 && y >= 2 && y <= 13) c = DoorFrame;
                    else if (x == 12 && y >= 4 && y <= 11) c = Lerp32(DoorFrame, DoorShade, 0.5f);
                }
                // Верхняя/нижняя перемычки
                if (x >= 4 && x <= 11)
                {
                    if (y <= 1) c = y == 0 ? DoorShade : DoorFrame;
                    else if (y >= 14) c = y == 15 ? DoorHigh : DoorFrame;
                }
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        // ─── Сохранение и создание Tile-ассета ───

        private static Tile SaveTile(Texture2D tex, string sub, string name)
        {
            var texPath = $"{BasePath}/{sub}/{name}.png";
            var fullPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!, texPath);

            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

            // Настройка импорта: sprite, PPU=16, Point filter, без компрессии
            var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = S;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;

            var tilePath = $"{BasePath}/{sub}/{name}.asset";
            AssetDatabase.CreateAsset(tile, tilePath);
            return tile;
        }

        // ─── Авто-привязка к TilemapRenderBridge в сцене ───

        private static void AutoAssign(Tile[] floors, Tile[] walls, Tile doorH, Tile doorV)
        {
            var bridge = Object.FindObjectOfType<Dots.Hybrid.TilemapRenderBridge>();
            if (bridge == null)
            {
                Debug.LogWarning("[TilesetGenerator] TilemapRenderBridge не найден в сцене — назначьте тайлы вручную.");
                return;
            }

            var so = new SerializedObject(bridge);

            // Стены (16 штук)
            var wallsProp = so.FindProperty("_wallTilesByMask");
            if (wallsProp != null && wallsProp.isArray)
            {
                wallsProp.arraySize = 16;
                for (var i = 0; i < 16; i++)
                    wallsProp.GetArrayElementAtIndex(i).objectReferenceValue = walls[i];
            }

            // Полы (вариации)
            var floorProp = so.FindProperty("_floorVariantTiles");
            if (floorProp != null && floorProp.isArray)
            {
                floorProp.arraySize = floors.Length;
                for (var i = 0; i < floors.Length; i++)
                    floorProp.GetArrayElementAtIndex(i).objectReferenceValue = floors[i];
            }

            // Декор (двери)
            var doorHProp = so.FindProperty("_doorHorizontalTile");
            if (doorHProp != null) doorHProp.objectReferenceValue = doorH;

            var doorVProp = so.FindProperty("_doorVerticalTile");
            if (doorVProp != null) doorVProp.objectReferenceValue = doorV;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bridge);
            Debug.Log("[TilesetGenerator] Тайлы назначены на TilemapRenderBridge.");
        }

        // ─── Утилиты ───

        private static Texture2D NewTex()
        {
            return new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private static int Hash(int x, int y, int seed)
        {
            return ((x * 73856093) ^ (y * 19349663) ^ (seed * 83492791)) & 0x7FFFFFFF;
        }

        private static Color32 AddNoise(Color32 c, int x, int y, int seed)
        {
            var h = Hash(x, y, seed) % 7 - 3; // -3..+3
            return new Color32(
                (byte)Mathf.Clamp(c.r + h, 0, 255),
                (byte)Mathf.Clamp(c.g + h, 0, 255),
                (byte)Mathf.Clamp(c.b + h, 0, 255),
                c.a);
        }

        private static Color32 Darken(Color32 c, float factor)
        {
            return new Color32(
                (byte)(c.r * factor),
                (byte)(c.g * factor),
                (byte)(c.b * factor),
                c.a);
        }

        private static Color32 Lerp32(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)(a.r + (b.r - a.r) * t),
                (byte)(a.g + (b.g - a.g) * t),
                (byte)(a.b + (b.b - a.b) * t),
                (byte)(a.a + (b.a - a.a) * t));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            var folderName = Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
