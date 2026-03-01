---

description: "Task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: The examples below include test tasks. Tests are OPTIONAL - only include them if explicitly requested in the feature specification.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Runtime (ECS)**: `Assets/_Project/Dots/Runtime/` — компоненты и системы
  - `Components/` — struct IComponentData
  - `Components/Config/` — singleton-конфигурации
  - `Systems/Fixed/` — FixedStepSimulationSystemGroup
  - `Systems/Initialization/` — InitializationSystemGroup
  - `Systems/Presentation/` — PresentationSystemGroup
- **Hybrid**: `Assets/_Project/Dots/Hybrid/` — мосты (Input, UI, Rendering)
- **Authoring**: `Assets/_Project/Dots/Authoring/` — Baker-компоненты
- **Документация**: `Docs/Mechanics/` — описания механик (NON-NEGOTIABLE)
- **Тесты**: `Assets/Tests/` — EditMode + PlayMode

<!-- 
  ============================================================================
  IMPORTANT: The tasks below are SAMPLE TASKS for illustration purposes only.
  
  The /speckit.tasks command MUST replace these with actual tasks based on:
  - User stories from spec.md (with their priorities P1, P2, P3...)
  - Feature requirements from plan.md
  - Entities from data-model.md
  - Endpoints from contracts/
  
  Tasks MUST be organized by user story so each story can be:
  - Implemented independently
  - Tested independently
  - Delivered as an MVP increment
  
  DO NOT keep these sample tasks in the generated tasks.md file.
  ============================================================================
-->

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Подготовка компонентов и конфигураций

- [ ] T001 Создать IComponentData-компоненты в Runtime/Components/
- [ ] T002 [P] Создать singleton-конфигурации в Runtime/Components/Config/
- [ ] T003 [P] Создать Authoring + Baker (если нужны prefab-данные)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Базовые системы и мосты, от которых зависят user stories

**CRITICAL**: Без этой фазы user stories не могут начаться

- [ ] T004 Реализовать ISystem в Runtime/Systems/ с [BurstCompile]
- [ ] T005 [P] Добавить [UpdateInGroup], [UpdateAfter/Before] атрибуты
- [ ] T006 [P] RequireForUpdate для singleton-зависимостей в OnCreate
- [ ] T007 Создать Hybrid-мост (если нужен Input/UI/Rendering)
- [ ] T008 Проверить компиляцию Burst (read_console, без ошибок)
- [ ] T009 Проверить отсутствие managed-типов в Runtime assembly

**Checkpoint**: Компиляция чистая, Burst без ошибок — user stories можно начинать

---

## Phase 3: User Story 1 - [Title] (Priority: P1) 🎯 MVP

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 1 (OPTIONAL - only if tests requested)

> **NOTE: Write tests FIRST, ensure they FAIL before implementation**

- [ ] T010 [P] [US1] EditMode-тест для системы в Assets/Tests/
- [ ] T011 [P] [US1] PlayMode-тест для интеграции (если нужен)

### Implementation for User Story 1

- [ ] T012 [P] [US1] Создать IComponentData в Runtime/Components/
- [ ] T013 [P] [US1] Создать ISystem с [BurstCompile] в Runtime/Systems/
- [ ] T014 [US1] Добавить ordering: [UpdateInGroup], [UpdateAfter/Before]
- [ ] T015 [US1] Создать Hybrid-мост (если нужен UI/визуал)
- [ ] T016 [US1] Проверить Burst-компиляцию и ECB-дисциплину
- [ ] T017 [US1] Обновить Docs/Mechanics/ (если затронута механика)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 2 (OPTIONAL - only if tests requested)

- [ ] T018 [P] [US2] EditMode-тест для системы в Assets/Tests/
- [ ] T019 [P] [US2] PlayMode-тест для интеграции (если нужен)

### Implementation for User Story 2

- [ ] T020 [P] [US2] Создать IComponentData в Runtime/Components/
- [ ] T021 [US2] Создать ISystem с [BurstCompile] в Runtime/Systems/
- [ ] T022 [US2] Создать Hybrid-мост (если нужен)
- [ ] T023 [US2] Обновить Docs/Mechanics/ (если затронута механика)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - [Title] (Priority: P3)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 3 (OPTIONAL - only if tests requested)

- [ ] T024 [P] [US3] EditMode-тест для системы в Assets/Tests/
- [ ] T025 [P] [US3] PlayMode-тест для интеграции (если нужен)

### Implementation for User Story 3

- [ ] T026 [P] [US3] Создать IComponentData в Runtime/Components/
- [ ] T027 [US3] Создать ISystem с [BurstCompile] в Runtime/Systems/
- [ ] T028 [US3] Создать Hybrid-мост (если нужен)

**Checkpoint**: All user stories should now be independently functional

---

[Add more user story phases as needed, following the same pattern]

---

## Phase N: Верификация и документация

**Purpose**: Финальная проверка и документация (NON-NEGOTIABLE)

- [ ] TXXX Burst-компиляция: проверить read_console на ошибки
- [ ] TXXX Managed-типы: убедиться нет string/class в Runtime/
- [ ] TXXX ECB: структурные изменения только через EntityCommandBuffer
- [ ] TXXX [P] Обновить Docs/Mechanics/ для затронутых механик
- [ ] TXXX [P] Обновить индекс Docs/RuntimeRoguelike_CurrentMechanics.md
- [ ] TXXX Проверить ordering: [UpdateInGroup], [UpdateAfter/Before]
- [ ] TXXX Запустить Play Mode и убедиться в работоспособности

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - May integrate with US1/US2 but should be independently testable

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Components before systems (данные → логика)
- Systems before bridges (логика → визуализация)
- Burst-проверка после каждой системы
- Документация механик — до завершения story

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Параллельно создаём компоненты:
Task: "Создать HealthComponent в Runtime/Components/"
Task: "Создать DamageComponent в Runtime/Components/"

# После компонентов — система (зависит от компонентов):
Task: "Создать DamageSystem в Runtime/Systems/Fixed/"

# Параллельно с системой — Hybrid-мост (если нужен):
Task: "Создать DamageEffectBridge в Hybrid/Presentation/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
