# Data Model: DOTS Audit & Refactor

**Date**: 2026-03-01
**Branch**: `001-dots-audit-refactor`

## Затрагиваемые сущности

Данная фича НЕ добавляет новых компонентов и НЕ меняет структуру данных.
Все изменения — в системах (ISystem) и документации.

### Системы, подлежащие изменению

#### Группа A: Добавление [BurstCompile] на методы (22 системы)

Минимальное изменение — добавить атрибут `[BurstCompile]` на OnUpdate (и OnCreate/OnDestroy если присутствуют):

| Система | Группа | Файл |
|---------|--------|------|
| CooldownTickSystem | FixedStep | Systems/Fixed/CooldownTickSystem.cs |
| DifficultyTickSystem | FixedStep | Systems/Fixed/DifficultyTickSystem.cs |
| EnemyTargetAcquireSystem | FixedStep | Systems/Fixed/EnemyTargetAcquireSystem.cs |
| EnemyMoveIntentSystem | FixedStep | Systems/Fixed/EnemyMoveIntentSystem.cs |
| EnemyPathfindSystem | FixedStep | Systems/Fixed/EnemyPathfindSystem.cs |
| MovementResolveSystem | FixedStep | Systems/Fixed/MovementResolveSystem.cs |
| PlayerAttackSystem | FixedStep | Systems/Fixed/PlayerAttackSystem.cs |
| EnemyAttackSystem | FixedStep | Systems/Fixed/EnemyAttackSystem.cs |
| HazardSystem | FixedStep | Systems/Fixed/HazardSystem.cs |
| PoisonTickSystem | FixedStep | Systems/Fixed/PoisonTickSystem.cs |
| LootPickupSystem | FixedStep | Systems/Fixed/LootPickupSystem.cs |
| DeathSystem | FixedStep | Systems/Fixed/DeathSystem.cs |
| LevelProgressSystem | FixedStep | Systems/Fixed/LevelProgressSystem.cs |
| EnemySpawnerSystem | FixedStep | Systems/Fixed/EnemySpawnerSystem.cs |
| InputReadSystem | Simulation | Systems/Simulation/InputReadSystem.cs |
| RestartRequestSystem | Simulation | Systems/Simulation/RestartRequestSystem.cs |
| RestartSystem | Simulation | Systems/Simulation/RestartSystem.cs |
| PerkApplySystem | Simulation | Systems/Simulation/PerkApplySystem.cs |
| TeleportSystem | Simulation | Systems/Simulation/TeleportSystem.cs |
| SpawnPlayerSystem | Initialization | Systems/Initialization/SpawnPlayerSystem.cs |
| SpawnInitialEnemiesSystem | Initialization | Systems/Initialization/SpawnInitialEnemiesSystem.cs |
| RenderInterpolationSystem | Presentation | Systems/Presentation/RenderInterpolationSystem.cs |

#### Группа B: Замена EntityManager.GetBuffer на SystemAPI (9 систем)

Замена паттерна:
```
// Было:
state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>())

// Стало:
SystemAPI.GetSingletonBuffer<CellOccupant>()
```

Системы: DeathSystem, PlayerAttackSystem, EnemyPathfindSystem, MovementResolveSystem, TeleportSystem, EnemySpawnerSystem, SpawnPlayerSystem, SpawnInitialEnemiesSystem, RestartSystem

#### Группа C: Точечные исправления (2 системы)

1. **EnemyAttackSystem**: `state.EntityManager.Exists(entity)` → `SystemAPI.HasComponent<Health>(entity)`
2. **RestartSystem**: 3 вызова EntityManager → SystemAPI эквиваленты

### Документация

| Файл | Изменение |
|------|-----------|
| Docs/DOTS_Migration_Plan.md | Обновить раздел 11 (Известные отклонения) |

### Файлы, которые НЕ меняются

- Все компоненты (Runtime/Components/) — 100% соответствие
- Все map-файлы (Runtime/Map/) — без изменений
- Все authoring/bakers (Authoring/) — без изменений
- Все hybrid-мосты (Hybrid/) — без изменений
- Все asmdef — без изменений
- RunBootstrapSystem — обоснованно без Burst
- MapGenerationSystem — обоснованно без Burst
