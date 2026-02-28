# Tasks: DOTS Audit & Refactor

**Input**: Design documents from `/specs/001-dots-audit-refactor/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Не запрошены. Верификация — ручная (запуск с seed 42, 12345, 999999 + Burst Inspector).

**Organization**: Задачи сгруппированы по user stories. US1 = аудит завершён (research.md), реализация начинается с US2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Можно выполнять параллельно (разные файлы, нет зависимостей)
- **[Story]**: К какой user story относится задача (US1, US2, US3)
- Пути относительно `Assets/_Project/Dots/Runtime/`

## Path Conventions

- **Systems**: `Assets/_Project/Dots/Runtime/Systems/{Initialization|Simulation|Fixed|Presentation}/`
- **Docs**: `Docs/DOTS_Migration_Plan.md`

---

## Phase 1: Setup

**Purpose**: Подготовка к рефакторингу — фиксация текущего состояния

- [ ] T001 Убедиться что проект компилируется без ошибок перед началом рефакторинга
- [ ] T002 Проверить консоль Unity на наличие существующих ошибок/warnings

**Checkpoint**: Проект в рабочем состоянии, можно начинать изменения

---

## Phase 2: Foundational — Замена EntityManager → SystemAPI (prerequisite для Burst)

**Purpose**: Устранить все вызовы EntityManager в OnUpdate, которые блокируют добавление [BurstCompile]

**⚠️ CRITICAL**: Без завершения этой фазы нельзя добавлять [BurstCompile] на OnUpdate методы систем из Группы B

### FixedStep системы (занимают occupancy через GetBuffer)

- [ ] T003 [P] Заменить `state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>())` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` в `Assets/_Project/Dots/Runtime/Systems/Fixed/DeathSystem.cs`
- [ ] T004 [P] Заменить `state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>())` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` в `Assets/_Project/Dots/Runtime/Systems/Fixed/PlayerAttackSystem.cs`
- [ ] T005 [P] Заменить `state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>())` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` в `Assets/_Project/Dots/Runtime/Systems/Fixed/EnemyPathfindSystem.cs`
- [ ] T006 [P] Заменить `state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>())` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` в `Assets/_Project/Dots/Runtime/Systems/Fixed/MovementResolveSystem.cs`
- [ ] T007 [P] Заменить `state.EntityManager.GetBuffer<CellOccupant>(SystemAPI.GetSingletonEntity<RunState>())` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` в `Assets/_Project/Dots/Runtime/Systems/Fixed/EnemySpawnerSystem.cs`

### Simulation системы

- [ ] T008 Заменить `state.EntityManager.GetBuffer<CellOccupant>(...)` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` в `Assets/_Project/Dots/Runtime/Systems/Simulation/TeleportSystem.cs`
- [ ] T009 Заменить в RestartSystem 4 прямых вызова EntityManager на SystemAPI-эквиваленты в `Assets/_Project/Dots/Runtime/Systems/Simulation/RestartSystem.cs`: **Группа C** (3 точечных замены): (1) `EntityManager.HasBuffer<PerkOption>` → `SystemAPI.HasBuffer<PerkOption>`, (2) `EntityManager.GetBuffer<PerkOption>.Clear()` → `SystemAPI.GetBuffer<PerkOption>.Clear()`, (3) `EntityManager.SetComponentEnabled<MapRenderRequest>` → `SystemAPI.SetComponentEnabled<MapRenderRequest>`; **Группа B** (общий паттерн): (4) `EntityManager.GetBuffer<CellOccupant>` → `SystemAPI.GetSingletonBuffer<CellOccupant>()`

### Initialization системы

- [ ] T010 [P] Заменить `state.EntityManager.GetBuffer<CellOccupant>(...)` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` в `Assets/_Project/Dots/Runtime/Systems/Initialization/SpawnPlayerSystem.cs`
- [ ] T011 [P] Заменить `state.EntityManager.GetBuffer<CellOccupant>(...)` на `SystemAPI.GetSingletonBuffer<CellOccupant>()` в `Assets/_Project/Dots/Runtime/Systems/Initialization/SpawnInitialEnemiesSystem.cs`

### Точечные исправления

- [ ] T012 Заменить `state.EntityManager.Exists(target.ValueRO.Value)` на `SystemAPI.HasComponent<Health>(target.ValueRO.Value)` в `Assets/_Project/Dots/Runtime/Systems/Fixed/EnemyAttackSystem.cs`

- [ ] T013 Проверить компиляцию проекта после всех замен Phase 2

**Checkpoint**: Все EntityManager вызовы в OnUpdate заменены на SystemAPI (кроме spawn-систем с Instantiate/CreateEntity). Проект компилируется.

---

## Phase 3: User Story 2 — Исправление HIGH-приоритетных проблем (Priority: P2) 🎯 MVP

**Goal**: Добавить [BurstCompile] на методы OnUpdate/OnCreate/OnDestroy для всех систем, где это возможно

**Independent Test**: Проект компилируется, Burst Inspector показывает компиляцию всех ожидаемых систем, запуск с seed=42 — поведение не изменилось

### FixedStep системы (14 шт.) — [BurstCompile] на OnUpdate

- [ ] T014 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/CooldownTickSystem.cs`
- [ ] T015 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/DifficultyTickSystem.cs`
- [ ] T016 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/EnemyTargetAcquireSystem.cs`
- [ ] T017 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/EnemyMoveIntentSystem.cs`
- [ ] T018 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/EnemyPathfindSystem.cs`
- [ ] T019 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/MovementResolveSystem.cs`
- [ ] T020 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/PlayerAttackSystem.cs`
- [ ] T021 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/EnemyAttackSystem.cs`
- [ ] T022 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/HazardSystem.cs`
- [ ] T023 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/PoisonTickSystem.cs`
- [ ] T024 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/LootPickupSystem.cs`
- [ ] T025 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/DeathSystem.cs`
- [ ] T026 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/LevelProgressSystem.cs`
- [ ] T027 [P] [US2] Проверить что `[BurstCompile]` на struct присутствует, НЕ добавлять на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Fixed/EnemySpawnerSystem.cs` — OnUpdate остаётся без атрибута из-за EntityManager.Instantiate

### Simulation системы (5 шт.)

- [ ] T028 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Simulation/InputReadSystem.cs`
- [ ] T029 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Simulation/RestartRequestSystem.cs`
- [ ] T030 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Simulation/RestartSystem.cs`
- [ ] T031 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Simulation/PerkApplySystem.cs`
- [ ] T032 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Simulation/TeleportSystem.cs`

### Initialization системы (2 шт.) — только struct-level (OnUpdate использует EntityManager.Instantiate)

- [ ] T033 [P] [US2] Убедиться что `[BurstCompile]` на struct присутствует, НЕ добавлять на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Initialization/SpawnPlayerSystem.cs`
- [ ] T034 [P] [US2] Убедиться что `[BurstCompile]` на struct присутствует, НЕ добавлять на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Initialization/SpawnInitialEnemiesSystem.cs`

### Presentation системы (1 шт.)

- [ ] T035 [P] [US2] Добавить `[BurstCompile]` на OnUpdate в `Assets/_Project/Dots/Runtime/Systems/Presentation/RenderInterpolationSystem.cs`

### Верификация Phase 3

- [ ] T036 [US2] Промежуточная проверка: компиляция проекта и отсутствие Burst-ошибок в консоли Unity (валидация только Phase 3 — [BurstCompile] на методах)
- [ ] T037 [US2] Промежуточная проверка: запустить игру с seed=42 и проверить что поведение не изменилось после добавления [BurstCompile] (быстрый smoke-test)

**Checkpoint**: Все системы, где возможно, имеют [BurstCompile] на OnUpdate. Burst Inspector подтверждает компиляцию. Геймплей не изменён.

---

## Phase 4: User Story 3 — Документация и среднеприоритетные улучшения (Priority: P3)

**Goal**: Обновить миграционный план, задокументировать все обоснованные отклонения

**Independent Test**: Документация актуальна, все отклонения обоснованы

### Обновление документации

- [ ] T038 [US3] Обновить раздел 11 «Известные отклонения от плана» в `Docs/DOTS_Migration_Plan.md`: добавить пункт 8 — «[BurstCompile] добавлен на методы OnUpdate/OnCreate/OnDestroy всех систем где это возможно (ранее был только на struct)»
- [ ] T039 [US3] Обновить раздел 11 в `Docs/DOTS_Migration_Plan.md`: добавить пункт 9 — «EntityManager.GetBuffer<CellOccupant> заменён на SystemAPI.GetSingletonBuffer<CellOccupant> во всех системах кроме spawn-систем»
- [ ] T040 [US3] Обновить раздел 11 в `Docs/DOTS_Migration_Plan.md`: добавить пункт 10 — «EntityManager.Exists заменён на SystemAPI.HasComponent<Health> в EnemyAttackSystem»
- [ ] T041 [US3] Обновить раздел 11 в `Docs/DOTS_Migration_Plan.md`: уточнить пункт 2 — spawn-системы остаются с EntityManager.Instantiate (обоснование: main thread, нет выгоды от ECB)
- [ ] T042 [US3] Обновить статус шага 8 в `Docs/DOTS_Migration_Plan.md` с результатами аудита (шаг 8 — проверка паритета)

**Checkpoint**: Миграционный план актуален, все отклонения задокументированы с обоснованиями.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Финальная верификация

- [ ] T043 Финальная верификация: запустить игру с seed=42 и пройти ПОЛНЫЙ цикл (движение, атаки, лут, перки, рестарт) — проверка после ВСЕХ фаз (код + документация)
- [ ] T044 Финальная верификация: запустить игру с seed=12345 и проверить корректность генерации карты и спавна
- [ ] T045 Финальная верификация: запустить игру с seed=999999 и проверить корректность
- [ ] T046 Финальная проверка консоли Unity на отсутствие ошибок и Burst-fallback предупреждений после всех изменений (код + документация)
- [ ] T047 Финальная проверка кодовой базы: все файлы изменены корректно, нет оставшихся EntityManager вызовов в FixedStep системах (кроме EnemySpawnerSystem.Instantiate)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Нет зависимостей — начинаем сразу
- **Foundational (Phase 2)**: Зависит от Phase 1 — БЛОКИРУЕТ добавление BurstCompile
- **US2 (Phase 3)**: Зависит от Phase 2 — основная работа по [BurstCompile]
- **US3 (Phase 4)**: Зависит от Phase 3 — документация результатов
- **Polish (Phase 5)**: Зависит от Phase 3 и 4 — финальная верификация

### User Story Dependencies

- **US1 (P1)**: ЗАВЕРШЕНА в phase 0 (research.md содержит полный аудит)
- **US2 (P2)**: Зависит от Phase 2 (замена EntityManager). Основной объём работы
- **US3 (P3)**: Зависит от US2 (нужно знать финальное состояние для документации)

### Within Each Phase

- Phase 2: T003-T011 параллельны (разные файлы), T012 параллелен, T013 — после всех
- Phase 3: T014-T035 параллельны (разные файлы, одинаковое изменение), T036-T037 — после всех
- Phase 4: T038-T042 последовательны (один файл)

### Parallel Opportunities

- **Phase 2**: T003-T012 — 10 задач параллельно (каждая в отдельном файле)
- **Phase 3**: T014-T035 — 22 задачи параллельно (каждая в отдельном файле)
- **Phase 4**: Последовательно (один файл DOTS_Migration_Plan.md)
- **Phase 5**: T043-T045 — 3 seed тестируемы параллельно

---

## Parallel Example: Phase 2 (Foundational)

```
# Все замены EntityManager → SystemAPI параллельно (разные файлы):
Task T003: DeathSystem.cs — GetBuffer → GetSingletonBuffer
Task T004: PlayerAttackSystem.cs — GetBuffer → GetSingletonBuffer
Task T005: EnemyPathfindSystem.cs — GetBuffer → GetSingletonBuffer
Task T006: MovementResolveSystem.cs — GetBuffer → GetSingletonBuffer
Task T007: EnemySpawnerSystem.cs — GetBuffer → GetSingletonBuffer
Task T008: TeleportSystem.cs — GetBuffer → GetSingletonBuffer
Task T010: SpawnPlayerSystem.cs — GetBuffer → GetSingletonBuffer
Task T011: SpawnInitialEnemiesSystem.cs — GetBuffer → GetSingletonBuffer
Task T012: EnemyAttackSystem.cs — Exists → HasComponent
```

## Parallel Example: Phase 3 (US2 — BurstCompile)

```
# Все добавления [BurstCompile] параллельно (разные файлы):
Task T014-T035: Добавить [BurstCompile] на OnUpdate в 22 системах
```

---

## Implementation Strategy

### MVP First (US2 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational замена EntityManager (T003-T013)
3. Complete Phase 3: US2 — добавление [BurstCompile] (T014-T037)
4. **STOP and VALIDATE**: Burst Inspector + seed=42
5. Рефакторинг завершён, геймплей не изменён

### Full Delivery

1. MVP (выше)
2. Phase 4: US3 — документация (T038-T042)
3. Phase 5: Финальная верификация (T043-T047)

---

## Notes

- [P] задачи = разные файлы, нет зависимостей
- [US2/US3] — привязка к user story
- US1 завершена на этапе планирования (research.md = аудит-отчёт)
- Commit после каждой фазы или логической группы задач
- НЕ менять геймплейную логику — только качество DOTS-кода
- Spawn-системы (SpawnPlayerSystem, SpawnInitialEnemiesSystem, EnemySpawnerSystem) — EntityManager.Instantiate остаётся, [BurstCompile] на OnUpdate НЕ добавлять
- RunBootstrapSystem и MapGenerationSystem — не затрагиваются (обоснованно без Burst)
