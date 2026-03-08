# Генерация карты

## Общие параметры

- **Размер карты**: конфигурируемый (default ~50×70 клеток).
- **Тип карты**: комнаты + коридоры (процедурная генерация).
- **Границы карты**: по периметру всегда стены (непроходимые).
- **Детерминизм**: один seed → идентичная карта.

## Алгоритм генерации

### Комнаты

- Количество комнат рассчитывается автоматически: `targetRooms = (mapArea / avgRoomArea) * densityFactor`.
- Комнаты размещаются random placement с проверкой на перекрытие (зазор 1 клетка).
- **Составные комнаты**: с вероятностью `compositeRoomChance` к комнате приклеивается второй
  прямоугольник, образуя Г/Т-образную форму.
- Размеры: от `minRoomSize` до `maxRoomSize` (задаётся в MapGenerationConfigAuthoring).

### Коридоры

- Строится **MST (Kruskal)** по центрам комнат — гарантирует связность.
- Дополнительно добавляются ~`extraCorridorRatio` рёбер из оставшегося графа — создаёт петли и развилки.
- Ширина коридоров: от `minCorridorWidth` до `maxCorridorWidth` (1–3 клетки).
- Каждый коридор — L-образный (горизонтальный + вертикальный сегмент).

### Типизация комнат

- Первая комната = **Start** (стартовая).
- Остальные — вероятностный выбор:
  - Normal (60%) — обычная комната.
  - HardEnemy (25%) — усиленные враги.
  - Treasure (15%) — сокровищница.
- Тип хранится в `BlobArray<RoomBlob>` внутри MapBlob.

## Слои карты (MapBlob)

| Слой | Тип | Описание |
|------|-----|----------|
| BaseLayer | MapCellType | Floor/Wall |
| ObstacleLayer | ObstacleType | None/Rock и т.д. |
| HazardLayer | HazardType | None/Spike/Poison |
| WallMaskLayer | byte (4-bit) | Bitmask стен для auto-tiling (N=1, E=2, S=4, W=8) |
| FloorVariantLayer | byte | Индекс варианта пола (xxHash от seed + cellIndex) |

## Данные комнат

Каждая комната хранится как `RoomBlob`:
- **Bounds** (int4): x, y, width, height
- **Center** (int2): центр bounding box
- **RoomType** (byte): Start/Normal/Treasure/HardEnemy
- **ConnectedCount** (byte): количество коридоров к этой комнате

## Стартовая зона (safe radius)

- Вокруг стартовой клетки гарантируется безопасный радиус `safeRadius`.
- В этой зоне клетки становятся проходимыми, нет ловушек/препятствий.
- Это защищает игрока от "моментальной смерти" на старте.

## Конфигурация

Параметры задаются в `MapGenerationConfigAuthoring` (Inspector):
- `MinRoomSize`, `MaxRoomSize` — диапазон размера комнат
- `SafeRadius` — радиус безопасной зоны
- `FallbackRoomSize` — размер fallback-комнаты
- `CompositeRoomChance` — вероятность составной комнаты (0–1)
- `ExtraCorridorRatio` — доля дополнительных коридоров (0–1)
- `MinCorridorWidth`, `MaxCorridorWidth` — диапазон ширины коридоров
- `DensityFactor` — плотность заполнения карты (0.3–1)
- `FloorVariantCount` — количество визуальных вариантов пола

## Визуальное оформление

### Auto-tiling стен
- 4-bit bitmask (N=1, E=2, S=4, W=8) определяет один из 16 тайлов для каждой стены.
- Рассчитывается в MapGenerationSystem.ComputeWallMasks().

### Вариации пола
- xxHash(seed, cellIndex) → индекс варианта (0..FloorVariantCount-1).
- Детерминированное распределение: один seed → одинаковые вариации.

### Декоративный слой
- **Дверные проёмы**: размещаются на floor-клетках, зажатых стенами по одной оси
  (горизонтальная или вертикальная щель между комнатой и коридором).
- Декоративный Tilemap-слой (sortingOrder=3) поверх основных слоёв.
- Тайлы настраиваются в TilemapRenderBridge Inspector.

## Реализация

- **Система**: `MapGenerationSystem` (ISystem, InitializationSystemGroup)
- **Хранение**: `BlobAssetReference<MapBlob>` — иммутабельные данные карты
- **Рендеринг**: `TilemapRenderBridge` (MonoBehaviour, Hybrid) — 4 Tilemap-слоя (Ground, Walls, Hazards, Decor)
- **Burst**: невозможен (BlobBuilder + EntityManager), но внутренние методы Burst-совместимы
