# Implementation Plan: DOTS Audit & Refactor

**Branch**: `001-dots-audit-refactor` | **Date**: 2026-03-01 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-dots-audit-refactor/spec.md`

## Summary

Аудит и рефакторинг DOTS-кодовой базы (87 файлов) для полного соответствия Unity Entities 1.4 best practices. Глубокий аудит выявил 5 категорий улучшений, из которых 4 подлежат исправлению (22 системы затронуты) и 1 документируется как обоснованное отклонение. Компоненты, authoring, hybrid-мосты и asmdef полностью соответствуют best practices — изменений не требуют.

## Technical Context

**Language/Version**: C# (.NET Standard 2.1) / Unity 2022.3 LTS
**Primary Dependencies**: Unity.Entities 1.4.x, Unity.Burst, Unity.Collections, Unity.Mathematics
**Storage**: BlobAssetReference<MapBlob> для данных карты, IComponentData для состояния
**Testing**: Ручное тестирование с seed (42, 12345, 999999) + Burst compilation check
**Target Platform**: Desktop (Editor + Standalone)
**Project Type**: 2D roguelike game (hybrid ECS + GameObject rendering)
**Performance Goals**: 60 fps, Burst-compiled simulation
**Constraints**: Без DOTS Graphics, без managed полей в Runtime, детерминизм по seed
**Scale/Scope**: 24 системы, 33+ компонентов, карта 200x200, до 50+ врагов

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Принцип | Статус | Проверка |
|---------|--------|----------|
| I. DOTS-First архитектура | PASS | Все системы — ISystem, компоненты — struct IComponentData |
| II. Разделение Simulation/Presentation | PASS | 4 asmdef, однонаправленный поток данных |
| III. Детерминизм симуляции | PASS | Только Unity.Mathematics.Random, фиксированный порядок систем |
| IV. Burst-производительность | **ISSUE** | [BurstCompile] отсутствует на методах OnUpdate у 22 систем |
| V. Дисциплина порядка систем | PASS | Все системы имеют корректные UpdateInGroup/After/Before |
| VI. Простота (YAGNI) | PASS | Минимальные изменения, без новых абстракций |

**Вывод Gate**: Принцип IV нарушен (Burst-атрибуты на struct, но не на методах). Данная фича УСТРАНЯЕТ это нарушение.

**Post-Phase 1 Re-check**: После реализации все 6 принципов будут PASS.

## Project Structure

### Documentation (this feature)

```text
specs/001-dots-audit-refactor/
├── plan.md              # Этот файл
├── research.md          # Результаты аудита (Phase 0)
├── data-model.md        # Список затрагиваемых файлов (Phase 1)
├── quickstart.md        # Инструкции по применению (Phase 1)
└── tasks.md             # Задачи (/speckit.tasks)
```

### Source Code (repository root)

```text
Assets/_Project/Dots/
├── Runtime/
│   ├── Components/          # БЕЗ ИЗМЕНЕНИЙ (100% соответствие)
│   ├── Systems/
│   │   ├── Initialization/  # 3 системы: SpawnPlayer, SpawnInitialEnemies, MapGeneration*
│   │   ├── Simulation/      # 5 систем: InputRead, RestartRequest, Restart, PerkApply, Teleport
│   │   ├── Fixed/           # 14 систем: все FixedStep (основная работа)
│   │   └── Presentation/    # 1 система: RenderInterpolation
│   ├── Map/                 # БЕЗ ИЗМЕНЕНИЙ
│   └── Utilities/           # БЕЗ ИЗМЕНЕНИЙ
├── Hybrid/                  # БЕЗ ИЗМЕНЕНИЙ
├── Authoring/               # БЕЗ ИЗМЕНЕНИЙ
└── Baking/                  # БЕЗ ИЗМЕНЕНИЙ

Docs/
└── DOTS_Migration_Plan.md   # Обновление раздела 11
```

**Structure Decision**: Существующая структура сохраняется. Изменения ТОЛЬКО в файлах систем (22 шт.) и документации (1 файл).

## Найденные проблемы и решения

Подробности — в [research.md](research.md).

### HIGH: [BurstCompile] на методах OnUpdate (22 системы)

**Проблема**: `[BurstCompile]` стоит на struct, но отсутствует на методах OnUpdate/OnCreate/OnDestroy. В Entities 1.4 атрибут нужен на КАЖДОМ методе для гарантии Burst-компиляции.

**Решение**: Добавить `[BurstCompile]` на все public методы ISystem (OnCreate, OnUpdate, OnDestroy).

**Ограничение**: Системы, использующие EntityManager в OnUpdate, НЕ могут иметь [BurstCompile] на OnUpdate. Сначала необходимо заменить EntityManager вызовы на SystemAPI (Проблема 2, 3, 4), и только потом добавлять атрибут.

**Порядок применения**:
1. Сначала — системы без EntityManager в OnUpdate (чистые SystemAPI): ~13 систем
2. Затем — после замены EntityManager → SystemAPI: ещё ~9 систем
3. Spawn-системы (SpawnPlayer, SpawnInitialEnemies, EnemySpawner) — [BurstCompile] только на struct, OnUpdate остаётся без атрибута (EntityManager.Instantiate несовместим с Burst)

### HIGH: EntityManager.GetBuffer → SystemAPI.GetSingletonBuffer (9 систем)

**Проблема**: 9 систем используют `state.EntityManager.GetBuffer<CellOccupant>(entity)` — это несовместимо с Burst.

**Решение**: Заменить на `SystemAPI.GetSingletonBuffer<CellOccupant>()` — буфер находится на singleton-entity RunState, единственный в мире.

**Системы**: DeathSystem, PlayerAttackSystem, EnemyPathfindSystem, MovementResolveSystem, TeleportSystem, EnemySpawnerSystem, SpawnPlayerSystem, SpawnInitialEnemiesSystem, RestartSystem.

### MEDIUM: EntityManager.Exists → SystemAPI (1 система)

**Проблема**: EnemyAttackSystem использует `state.EntityManager.Exists(entity)`.
**Решение**: Заменить на `SystemAPI.HasComponent<Health>(entity)` — семантически эквивалентно (проверяем жив ли таргет).

### MEDIUM: RestartSystem — 3 прямых вызова EntityManager

**Проблема**: HasBuffer, GetBuffer.Clear(), SetComponentEnabled через EntityManager.
**Решение**: Заменить на SystemAPI эквиваленты.

### LOW: Spawn-системы — документирование отклонения

**Проблема**: SpawnPlayerSystem, SpawnInitialEnemiesSystem, EnemySpawnerSystem используют EntityManager.Instantiate/CreateEntity.
**Решение**: Документировать в DOTS_Migration_Plan.md раздел 11 как обоснованное отклонение. EntityManager.Instantiate необходим для спавна, ECB не даёт преимуществ для однопоточных систем.

## Стратегия реализации

### Фаза 1: Замена EntityManager → SystemAPI (prerequisite для Burst)

1. Заменить `state.EntityManager.GetBuffer<CellOccupant>(...)` → `SystemAPI.GetSingletonBuffer<CellOccupant>()` в 9 системах
2. Заменить `state.EntityManager.Exists()` → `SystemAPI.HasComponent<Health>()` в EnemyAttackSystem
3. Заменить 3 прямых вызова EntityManager в RestartSystem на SystemAPI
4. Проверить компиляцию после каждой замены

### Фаза 2: Добавление [BurstCompile] на методы

1. Добавить `[BurstCompile]` на OnUpdate для систем без EntityManager в OnUpdate
2. Добавить `[BurstCompile]` на OnCreate/OnDestroy где применимо
3. Проверить Burst compilation в Unity Editor (Burst Inspector)
4. Для spawn-систем — [BurstCompile] только на struct, не на OnUpdate

### Фаза 3: Документация

1. Обновить раздел 11 DOTS_Migration_Plan.md
2. Добавить пункты об обоснованных отклонениях spawn-систем
3. Зафиксировать что проблемы 1-4 устранены

### Фаза 4: Верификация

1. Запуск с seed 42, 12345, 999999
2. Проверка консоли Unity на ошибки и Burst warnings
3. Проверка Burst Inspector — все ожидаемые системы Burst-compiled

## Complexity Tracking

| Отклонение от конституции | Обоснование | Почему нельзя проще |
|--------------------------|-------------|---------------------|
| Spawn-системы без [BurstCompile] на OnUpdate | EntityManager.Instantiate/CreateEntity несовместимы с Burst | ECB для Instantiate потребует отложенной инициализации компонентов — усложнение без выгоды для однопоточных систем, вызываемых редко |
| RunBootstrapSystem без [BurstCompile] | BlobBuilder + EntityManager в OnCreate | Система вызывается один раз; перенос в ECB/Job невозможен для BlobBuilder |
| MapGenerationSystem без [BurstCompile] | BlobBuilder + EntityManager для создания карты | Система вызывается один раз за забег; внутренние методы Burst-совместимы |
