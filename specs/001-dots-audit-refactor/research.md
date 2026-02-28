# Research: DOTS Audit & Refactor

**Date**: 2026-03-01
**Branch**: `001-dots-audit-refactor`

## Методология аудита

Проведён полный аудит 87 файлов в `Assets/_Project/Dots/`:
- 24 системы (Runtime/Systems)
- 33+ компонента (Runtime/Components)
- 3 map-файла (Runtime/Map)
- 2 утилиты (Runtime/)
- 9 hybrid-мостов (Hybrid/)
- 13 authoring/baker (Authoring/)
- 4 asmdef

Каждая система проверена по чеклисту: `[BurstCompile]`, ISystem, SystemAPI vs EntityManager, ECB, system ordering, RequireForUpdate, NativeContainer lifecycle.

## Найденные проблемы

### Проблема 1: [BurstCompile] отсутствует на методах OnUpdate (22 системы)

**Решение**: Добавить `[BurstCompile]` на OnUpdate (и OnCreate/OnDestroy где применимо)
**Обоснование**: В Unity Entities 1.4 `[BurstCompile]` на struct НЕ достаточно — атрибут нужен на каждом методе для гарантии Burst-компиляции. Без него метод может fallback на Mono.
**Альтернативы**: Оставить как есть — отвергнуто, т.к. конституция требует Burst для всех систем симуляции.

**Затронутые системы** (все кроме RunBootstrapSystem и MapGenerationSystem):
- CooldownTickSystem, DifficultyTickSystem, EnemyTargetAcquireSystem
- EnemyMoveIntentSystem, PoisonTickSystem, LootPickupSystem
- RestartRequestSystem, RestartSystem, PerkApplySystem
- RenderInterpolationSystem, InputReadSystem, DeathSystem
- HazardSystem, LevelProgressSystem, PlayerAttackSystem
- SpawnInitialEnemiesSystem, EnemyAttackSystem, EnemySpawnerSystem
- SpawnPlayerSystem, EnemyPathfindSystem, TeleportSystem, MovementResolveSystem

### Проблема 2: state.EntityManager.GetBuffer в OnUpdate (9 систем)

**Решение**: Заменить `state.EntityManager.GetBuffer<CellOccupant>(entity)` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` — buffer находится на singleton-entity RunState и единственный в мире.
**Обоснование**: `SystemAPI.GetSingletonBuffer` — Burst-совместимый API. EntityManager.GetBuffer — нет.
**Альтернатива**: Использовать `BufferLookup<CellOccupant>` — отвергнуто, т.к. GetSingletonBuffer проще и точнее отражает семантику (один буфер на весь мир).

**Затронутые системы**:
- DeathSystem (line 32)
- PlayerAttackSystem (line 27)
- EnemyPathfindSystem (line 58)
- MovementResolveSystem (line 51)
- TeleportSystem (line 48)
- EnemySpawnerSystem (line 55)
- SpawnPlayerSystem (line 80)
- SpawnInitialEnemiesSystem (line 53)
- RestartSystem (line 73)

### Проблема 3: EntityManager.Exists в EnemyAttackSystem

**Решение**: Заменить `state.EntityManager.Exists(entity)` на проверку через `SystemAPI.HasComponent<Health>(entity)` или `ComponentLookup<Health>`.
**Обоснование**: EntityManager.Exists несовместим с Burst. HasComponent выполняет ту же проверку (entity с Health = живой враг/игрок).
**Альтернатива**: Оставить EntityManager.Exists — отвергнуто из-за Burst-несовместимости.

### Проблема 4: RestartSystem — прямые EntityManager вызовы

**Решение**: Заменить:
- `state.EntityManager.HasBuffer<PerkOption>(entity)` → `SystemAPI.HasBuffer<PerkOption>(entity)`
- `state.EntityManager.GetBuffer<PerkOption>(entity).Clear()` → `SystemAPI.GetSingletonBuffer<PerkOption>().Clear()` (с предварительной проверкой)
- `state.EntityManager.SetComponentEnabled<MapRenderRequest>(entity, true)` → `SystemAPI.SetComponentEnabled<MapRenderRequest>(entity, true)`
**Обоснование**: SystemAPI эквиваленты существуют и Burst-совместимы.

### Проблема 5: Spawn-системы — EntityManager.Instantiate/CreateEntity

**Решение**: Документировать как обоснованное отклонение. НЕ рефакторить.
**Обоснование**: SpawnPlayerSystem, SpawnInitialEnemiesSystem, EnemySpawnerSystem выполняются на main thread и делают структурные изменения (Instantiate + добавление компонентов). ECB тут не даёт преимуществ — системы однопоточные, Instantiate через ECB потребует отложенной инициализации компонентов (усложнение без выгоды). Burst для этих систем невозможен из-за EntityManager, но они вызываются редко (инициализация / спавн по таймеру).

## Компоненты — результат проверки

**Статус: 100% соответствие best practices**
- 0 managed полей
- Корректное использование IEnableableComponent (MoveIntent, AttackRequest, MapRenderRequest)
- Корректное использование IBufferElementData (PathStep, PerkOption, LootEntryData, PerkData, CellOccupant)
- InternalBufferCapacity(0) на CellOccupant — корректно (внешний буфер для 40000 ячеек)

## Assembly Definitions — результат проверки

**Статус: 100% соответствие**
- Зависимости корректны, без циклов
- Runtime: Unity.Entities, Unity.Burst, Unity.Collections, Unity.Mathematics
- Hybrid: + RuntimeRoguelike.Dots.Runtime
- Authoring: + Unity.Entities.Hybrid
- Baking: noEngineReferences: true

## Hybrid Bridges — результат проверки

**Статус: Соответствует**
- Все мосты main-thread only
- EntityManager кэшируется в Awake()
- Queries кэшируются

## Authoring/Bakers — результат проверки

**Статус: 100% соответствие**
- DependsOn корректно используется в PrefabConfigAuthoring
- TransformUsageFlags корректны
- Все baker-ы следуют единому паттерну

## Итоговая приоритизация

| # | Проблема | Приоритет | Систем | Burst-impact |
|---|----------|-----------|--------|--------------|
| 1 | [BurstCompile] на методах | High | 22 | Без атрибута — potential Mono fallback |
| 2 | EntityManager.GetBuffer → SystemAPI | High | 9 | Блокирует Burst для FixedStep систем |
| 3 | EntityManager.Exists | Medium | 1 | Блокирует Burst для EnemyAttackSystem |
| 4 | RestartSystem EM calls | Medium | 1 | Блокирует Burst для RestartSystem |
| 5 | Spawn systems EM calls | Low | 3 | Обоснованное отклонение, документировать |
