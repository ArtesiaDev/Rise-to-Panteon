# Implementation Plan: Редизайн генерации карты

**Branch**: `002-map-generation-redesign` | **Date**: 2026-03-01 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-map-generation-redesign/spec.md`

## Summary

Полная переработка системы генерации карты: расширенный алгоритм с комнатами разных форм/размеров и разветвлёнными коридорами, визуальное оформление через pixel art тайлсет с auto-tiling (bitmask), система тумана войны (symmetric shadowcasting), оверлейная миникарта. Обратная совместимость MapBlob сохраняется — существующие 10 систем-потребителей не требуют изменений.

## Technical Context

**Language/Version**: C# (.NET Standard 2.1) / Unity 2022.3 LTS
**Primary Dependencies**: Unity.Entities 1.4.x, Unity.Burst,
Unity.Collections, Unity.Mathematics, URP 2D
**Storage**: BlobAssetReference<MapBlob> для карты,
IComponentData для состояния, singleton-компоненты для глобалов,
NativeArray<FogState> для изменяемого состояния тумана войны
**Testing**: Unity Test Runner (EditMode + PlayMode)
**Target Platform**: Unity 2D (PC/Mac)
**Project Type**: 2D roguelike game (ECS + Hybrid rendering)
**Performance Goals**: 60 FPS, детерминированная симуляция по seed,
генерация карты < 1 секунда
**Constraints**: Burst-совместимый код, без managed-типов в
компонентах, ECB для структурных изменений
**Scale/Scope**: Конфигурируемый размер карты (default ~50×70),
4 assembly (Runtime, Hybrid, Authoring, Baking)

**Key Technical Decisions** (из research.md):
- **FoW алгоритм**: Symmetric Shadowcasting — O(r^2), Burst-compatible
- **Auto-tiling**: 4-bit bitmask в BlobArray<byte>, индексация Tile[] в мосту
- **Генерация**: Random placement + MST коридоры + составные комнаты
- **Миникарта**: Runtime Texture2D через MinimapBridge (Hybrid/UI)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Принцип | Проверка | Статус |
|---|---------|----------|--------|
| I | DOTS-First | Генерация, FoW — ISystem. Миникарта, рендеринг — MonoBehaviour в Hybrid. Игровая логика не в Hybrid | ✅ |
| II | Simulation/Presentation | FogOfWarSystem (Runtime) → FogRenderBridge (Hybrid). MinimapBridge читает NativeArray. Hybrid зависит от Runtime, не наоборот | ✅ |
| III | Детерминизм | Генерация: Unity.Mathematics.Random от seed. Floor variants: xxHash от seed+cell. FoW: детерминирован позицией игрока | ✅ |
| IV | Burst | FogOfWarSystem — [BurstCompile]. MapGenerationSystem — без Burst (существующий паттерн, BlobBuilder + EntityManager, комментарий сохраняется) | ✅ |
| V | Порядок систем | FogOfWarSystem: [UpdateInGroup(FixedStep)], [UpdateAfter(MovementResolve)], RequireForUpdate<MapBlobReference> | ✅ |
| VI | YAGNI | Минимум: 4-bit bitmask (не 8-bit), 3 floor варианта, простой Texture2D для миникарты (без камеры) | ✅ |
| — | Документация механик | Обновить Docs/Mechanics/MapGeneration.md, создать FogOfWar.md, Minimap.md, обновить индекс | ✅ |

## Project Structure

### Documentation (this feature)

```text
specs/002-map-generation-redesign/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: алгоритмы LOS, auto-tiling, генерации
├── data-model.md        # Phase 1: расширенная модель данных
├── quickstart.md        # Phase 1: порядок реализации и зависимости
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code — изменения

```text
Assets/_Project/Dots/
├── Runtime/
│   ├── Components/Config/
│   │   ├── MapGenerationConfigData.cs   ← РАСШИРИТЬ (6 новых полей)
│   │   └── VisibilityConfigData.cs      ← НОВЫЙ
│   ├── Systems/Fixed/
│   │   └── FogOfWarSystem.cs            ← НОВЫЙ (shadowcasting)
│   ├── Systems/Initialization/
│   │   └── MapGenerationSystem.cs       ← ПЕРЕРАБОТАТЬ
│   └── Map/
│       ├── MapBlob.cs                   ← РАСШИРИТЬ (3 новых BlobArray + RoomBlob)
│       ├── MapTypes.cs                  ← ДОБАВИТЬ RoomType, FogState enums
│       └── MapUtilities.cs             ← ДОБАВИТЬ IsWall(), GetRoomAt()
├── Hybrid/
│   ├── Rendering/
│   │   ├── TilemapRenderBridge.cs       ← ПЕРЕРАБОТАТЬ (bitmask → Tile[])
│   │   └── FogRenderBridge.cs           ← НОВЫЙ
│   └── UI/
│       └── MinimapBridge.cs             ← НОВЫЙ
├── Authoring/
│   └── Components/
│       ├── MapGenerationConfigAuthoring.cs ← РАСШИРИТЬ
│       └── VisibilityConfigAuthoring.cs    ← НОВЫЙ
│
Docs/Mechanics/
├── MapGeneration.md                     ← ОБНОВИТЬ
├── FogOfWar.md                          ← НОВЫЙ
└── Minimap.md                           ← НОВЫЙ
Docs/RuntimeRoguelike_CurrentMechanics.md ← ОБНОВИТЬ индекс
```

### Зависимости между файлами (порядок создания)

```
MapTypes.cs (enums)
  → MapBlob.cs (struct расширение)
    → MapGenerationConfigData.cs (новые поля)
      → MapGenerationSystem.cs (новый алгоритм)
        → TilemapRenderBridge.cs (bitmask рендеринг)

VisibilityConfigData.cs
  → FogOfWarSystem.cs (shadowcasting)
    → FogRenderBridge.cs (fog tint)
      → MinimapBridge.cs (texture2d overlay)
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| MapGenerationSystem без [BurstCompile] | BlobBuilder + EntityManager.AddComponent | Существующий паттерн проекта, комментарий присутствует |
| NativeArray<FogState> в ISystem поле (не в компоненте) | Изменяемый массив размера карты, пересоздаётся при рестарте | BlobArray read-only; DynamicBuffer потребовал бы entity per cell. Паттерн идентичен EnemyPathfindSystem._gScore |
| Texture2D managed-объект в MinimapBridge | Unity UI RawImage требует Texture2D | Hybrid-слой допускает managed-типы по конституции |

## Архитектурный обзор

### Поток данных (дополнение к существующему)

```
[Initialization]
  MapGenerationSystem
    → создаёт MapBlob (+ WallMask, FloorVariant, Rooms)
    → инициализирует CellOccupant буфер
    → ставит MapRenderRequest

[FixedStepSimulation]
  ... существующие системы (без изменений) ...
  MovementResolveSystem
    → обновляет GridPosition игрока
    ↓
  FogOfWarSystem [НОВЫЙ]
    → читает GridPosition игрока + MapBlob.BaseLayer
    → shadowcasting: обновляет NativeArray<FogState>
    → ставит FogRenderRequest

[Hybrid Bridges]
  TilemapRenderBridge [ИЗМЕНЁН]
    → читает MapBlob.WallMaskLayer → Tile[] wallTilesByMask
    → читает MapBlob.FloorVariantLayer → Tile[] floorVariantTiles
    → добавляет декоративный Tilemap-слой

  FogRenderBridge [НОВЫЙ]
    → читает NativeArray<FogState> из FogOfWarSystem
    → управляет alpha/tint тайлов по состоянию видимости
    → скрывает/показывает SpriteRenderer врагов по FogState

  MinimapBridge [НОВЫЙ]
    → читает NativeArray<FogState> + MapBlob + GridPosition (всех сущностей)
    → рисует Texture2D: Explored=бежевый, маркеры=цветные пиксели
    → toggle через InputBridge → MinimapToggleTag
```

### Зависимости систем (Fixed step)

```
[FixedStepSimulationSystemGroup]
  ... (14 существующих систем, порядок сохраняется) ...
  ├── MovementResolveSystem
  │     ↓ [UpdateAfter]
  └── FogOfWarSystem [НОВЫЙ, позиция 15]
```
