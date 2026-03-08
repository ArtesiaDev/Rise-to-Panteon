# Data Model: Редизайн генерации карты

**Branch**: `002-map-generation-redesign` | **Date**: 2026-03-01

## Изменённые сущности

### MapBlob (расширенный BlobAsset)

```
MapBlob
├── Size: int2                           // (существующий) размер карты
├── StartCell: int2                      // (существующий) стартовая позиция
├── BaseLayer: BlobArray<MapCellType>    // (существующий) пол/стена
├── ObstacleLayer: BlobArray<ObstacleType> // (существующий) препятствия
├── HazardLayer: BlobArray<HazardType>   // (существующий) хазарды
├── WallMaskLayer: BlobArray<byte>       // (новый) 4-bit bitmask стен для auto-tiling
├── FloorVariantLayer: BlobArray<byte>   // (новый) индекс варианта пола (0..N-1)
├── RoomCount: int                       // (новый) количество комнат
└── Rooms: BlobArray<RoomBlob>           // (новый) данные комнат
```

**Индексация**: row-major, `index = cell.y * Size.x + cell.x`
**Иммутабельность**: BlobAsset — read-only после создания

### RoomBlob (вложенный в MapBlob)

```
RoomBlob
├── Bounds: int4          // (x, y, width, height) — bounding box комнаты
├── Center: int2          // центр bounding box: (x + width/2, y + height/2)
│                         // для составных (Г/Т) комнат — центр охватывающего BB
├── RoomType: byte        // 0=Start, 1=Normal, 2=Treasure, 3=HardEnemy
└── ConnectedCount: byte  // кол-во соединённых коридорами комнат
```

**Validation**: Bounds всегда внутри [0, Size). RoomType ∈ {0,1,2,3}.

### RoomType (новый enum)

```
enum RoomType : byte
├── Start = 0          // стартовая комната, безопасный радиус
├── Normal = 1         // обычная, стандартные вероятности спауна
├── Treasure = 2       // повышенный лут, пониженные враги
└── HardEnemy = 3      // усиленные/повышенное количество врагов
```

### FogState (новый enum)

```
enum FogState : byte
├── Unexplored = 0   // никогда не видели — полностью скрыто
├── Explored = 1     // видели ранее — затемнено, планировка видна
└── Visible = 2      // в текущем поле зрения — полностью видимо
```

## Новые компоненты

### MapGenerationConfigData (расширенный)

```
MapGenerationConfigData : IComponentData (singleton)
├── MinRoomSize: int       // (существующий, default 4)
├── MaxRoomSize: int       // (существующий, default 14)
├── SafeRadius: int        // (существующий, default 6)
├── FallbackRoomSize: int2 // (существующий, default 8,8)
├── CompositeRoomChance: float  // (новый) вероятность составной формы (0.0–1.0)
├── ExtraCorridorRatio: float   // (новый) доля дополнительных рёбер (0.0–1.0)
├── MinCorridorWidth: int       // (новый, default 1)
├── MaxCorridorWidth: int       // (новый, default 3)
├── DensityFactor: float        // (новый) коэфф. плотности заполнения (0.5–1.0)
└── FloorVariantCount: int      // (новый, default 3)
```

**Удалено**: `RoomAttempts` — заменён на автоматический расчёт из DensityFactor.

### VisibilityConfigData : IComponentData (singleton)

```
VisibilityConfigData
├── ViewRadius: int     // радиус видимости в клетках (default 8)
└── Enabled: bool       // включен ли туман войны (default true)
```

### MinimapToggleTag : IComponentData, IEnableableComponent

```
MinimapToggleTag
└── (пустой — состояние через Enabled/Disabled)
```

### FogRenderRequest : IComponentData, IEnableableComponent

```
FogRenderRequest
└── (пустой — триггер обновления)
```

## Изменённые компоненты

### RunState (минимальные добавления)

```
RunState : IComponentData (singleton)
├── Seed: uint               // (существующий)
├── MapSize: int2            // (существующий)
├── StartCell: int2          // (существующий)
├── SafeRadius: int          // (существующий)
├── RunId: int               // (существующий)
├── IsInitialized: bool      // (существующий)
└── FixedStepApplied: bool   // (существующий)
```

Без изменений — RunState кеширует MapBlob поля, паттерн сохраняется.

## WallMask битовая схема (4-bit)

```
Бит 0 (1): Север  — cell + (0, +1) — стена
Бит 1 (2): Восток — cell + (+1, 0) — стена
Бит 2 (4): Юг     — cell + (0, -1) — стена
Бит 3 (8): Запад  — cell + (-1, 0) — стена

Примеры:
0b0000 (0)  = изолированная стена (нет стен-соседей)
0b1111 (15) = стена окружена стенами со всех сторон
0b0110 (6)  = стена-коридор (восток + юг)
0b0101 (5)  = стена-угол (восток + юг)
```

## Связи между сущностями

```
MapBlob ←──contains──→ RoomBlob[N]
   │                       │
   │ read by               │ read by
   ↓                       ↓
FogOfWarSystem         SpawnInitialEnemiesSystem
   │                   EnemySpawnerSystem
   │                       │
   │ writes                │ reads RoomType
   ↓                       ↓
NativeArray<FogState>  Выбор контента по типу комнаты
   │
   │ read by
   ↓
FogRenderBridge ──→ TilemapRenderBridge (tint/alpha)
MinimapBridge   ──→ Texture2D UI overlay
```

## Миграционные заметки

- MapBlob структура расширяется, не ломается — все существующие системы продолжают читать Size, BaseLayer, ObstacleLayer, HazardLayer без изменений
- MapUtilities.IsWalkable() не меняется
- CellOccupant буфер не меняется
- MapGenerationConfigData расширяется — Baker добавляет новые поля с defaults
