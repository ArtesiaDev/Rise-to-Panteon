# Quickstart: Редизайн генерации карты

**Branch**: `002-map-generation-redesign` | **Date**: 2026-03-01

## Зависимости

### Готовые (уже в проекте)
- Unity 2022.3 LTS + Entities 1.4.x + Burst + Collections + Mathematics
- URP 2D + Tilemap (UnityEngine.Tilemaps)
- Существующий MapBlob, MapGenerationSystem, TilemapRenderBridge

### Необходимо подготовить
- Pixel art тайлсет (ассет-пак) содержащий:
  - 16 стеновых тайлов (4-bit bitmask варианты)
  - 3+ тайла пола (вариации)
  - 3+ декоративных тайла (факелы, знамёна, дверные проёмы)
  - Размер тайла: совместим с текущим CellSize (1.0 unit, например 16x16 или 32x32 px)

## Порядок реализации

```
P1: Генерация карты
  1. Расширить MapBlob (новые BlobArray-поля)
  2. Расширить MapGenerationConfigData (новые параметры)
  3. Переписать MapGenerationSystem (новый алгоритм)
  4. Проверить: все 10 consumer-систем работают без изменений
     ↓
P2: Визуальное оформление
  5. Подготовить/импортировать тайлсет
  6. Переписать TilemapRenderBridge (bitmask → Tile[])
  7. Добавить декоративный слой
     ↓
P3: Туман войны
  8. Создать FogOfWarSystem (shadowcasting)
  9. Создать VisibilityConfigData + Authoring
  10. Создать FogRenderBridge (Tilemap tint/alpha)
  11. Интегрировать с TilemapRenderBridge
      ↓
P4: Миникарта
  12. Создать MinimapBridge (Texture2D UI)
  13. Интегрировать с InputBridge (toggle)
  14. Добавить маркеры сущностей
```

## Критические файлы

| Файл | Действие |
|------|----------|
| `Runtime/Map/MapBlob.cs` | Расширить struct (3 новых поля) |
| `Runtime/Map/MapTypes.cs` | Добавить RoomType, FogState enums |
| `Runtime/Systems/Initialization/MapGenerationSystem.cs` | Полная переработка |
| `Runtime/Components/Config/MapGenerationConfigData.cs` | Расширить (6 новых полей) |
| `Authoring/Components/MapGenerationConfigAuthoring.cs` | Расширить Baker |
| `Hybrid/Rendering/TilemapRenderBridge.cs` | Переработка рендеринга |
| `Runtime/Systems/Fixed/FogOfWarSystem.cs` | **Новый** |
| `Runtime/Components/Config/VisibilityConfigData.cs` | **Новый** |
| `Hybrid/Rendering/FogRenderBridge.cs` | **Новый** |
| `Hybrid/UI/MinimapBridge.cs` | **Новый** |
| `Docs/Mechanics/MapGeneration.md` | Обновить |
| `Docs/Mechanics/FogOfWar.md` | **Новый** |
| `Docs/Mechanics/Minimap.md` | **Новый** |

## Проверка после каждого этапа

- `Burst → Compile`: убедиться что [BurstCompile] не сломался
- `Play → Generate`: карта генерируется, игрок спаунится
- `Play → Move`: pathfinding, movement, hazards работают
- `Determinism`: дважды один seed → одинаковая карта
