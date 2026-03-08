# Tasks: Редизайн генерации карты

**Input**: Design documents from `/specs/002-map-generation-redesign/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Runtime (ECS)**: `Assets/_Project/Dots/Runtime/` — компоненты и системы
  - `Components/` — struct IComponentData
  - `Components/Config/` — singleton-конфигурации
  - `Systems/Fixed/` — FixedStepSimulationSystemGroup
  - `Systems/Initialization/` — InitializationSystemGroup
- **Hybrid**: `Assets/_Project/Dots/Hybrid/` — мосты (Input, UI, Rendering)
- **Authoring**: `Assets/_Project/Dots/Authoring/` — Baker-компоненты
- **Документация**: `Docs/Mechanics/` — описания механик (NON-NEGOTIABLE)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Расширение базовых типов и компонентов, от которых зависят все user stories

- [X] T001 [P] Добавить RoomType enum (Start=0, Normal=1, Treasure=2, HardEnemy=3) и FogState enum (Unexplored=0, Explored=1, Visible=2) в `Assets/_Project/Dots/Runtime/Map/MapTypes.cs`
- [X] T002 [P] Добавить RoomBlob struct (Bounds: int4, Center: int2, RoomType: byte, ConnectedCount: byte), расширить MapBlob struct (+ WallMaskLayer: BlobArray<byte>, FloorVariantLayer: BlobArray<byte>, RoomCount: int, Rooms: BlobArray<RoomBlob>) в `Assets/_Project/Dots/Runtime/Map/MapBlob.cs`
- [X] T003 [P] Добавить static-методы IsWall(ref MapBlob, int2), GetRoomAt(ref MapBlob, int2) в `Assets/_Project/Dots/Runtime/Map/MapUtilities.cs`
- [X] T004 Расширить MapGenerationConfigData (+ CompositeRoomChance: float, ExtraCorridorRatio: float, MinCorridorWidth: int, MaxCorridorWidth: int, DensityFactor: float, FloorVariantCount: int; удалить RoomAttempts) в `Assets/_Project/Dots/Runtime/Components/Config/MapGenerationConfigData.cs`
- [X] T005 Расширить Baker в MapGenerationConfigAuthoring (новые поля с дефолтами: CompositeRoomChance=0.3, ExtraCorridorRatio=0.2, MinCorridorWidth=1, MaxCorridorWidth=3, DensityFactor=0.7, FloorVariantCount=3) в `Assets/_Project/Dots/Authoring/Components/MapGenerationConfigAuthoring.cs`
- [X] T006 Проверить компиляцию: read_console на отсутствие ошибок после расширения типов

**Checkpoint**: Базовые типы, структуры и конфигурации расширены. Все существующие системы компилируются без ошибок.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Переработка MapGenerationSystem — новый алгоритм генерации (комнаты + коридоры), заполнение новых BlobArray-полей

**CRITICAL**: Без этой фазы user stories US1–US4 не могут начаться

- [X] T007 Переработать алгоритм генерации в MapGenerationSystem: random placement комнат с auto-расчётом количества (targetRooms = mapArea / avgRoomArea * densityFactor), поддержка составных комнат (compositeChance), хранение в NativeList<RoomData> в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T008 Реализовать MST-коридоры (Prim/Kruskal по центрам комнат) + extra edges (extraCorridorRatio) с вариативной шириной (1–3 клетки) в MapGenerationSystem в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T009 Реализовать ComputeWallMasks() — расчёт 4-bit bitmask (N=1, E=2, S=4, W=8) для каждой стены, заполнение BlobArray<byte> WallMaskLayer в MapGenerationSystem в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T010 Реализовать ComputeFloorVariants() — xxHash(seed, cellIndex) для детерминированного выбора варианта пола (0..FloorVariantCount-1), заполнение BlobArray<byte> FloorVariantLayer в MapGenerationSystem в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T011 Реализовать заполнение BlobArray<RoomBlob> Rooms и RoomCount: запись bounds, center, roomType (Start для первой, вероятностный выбор Normal 60%/Treasure 15%/HardEnemy 25% для остальных), connectedCount в MapGenerationSystem в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T012 Проверить: все 10 существующих consumer-систем работают без изменений (EnemyPathfindSystem, MovementResolveSystem, HazardSystem, SpawnPlayerSystem, SpawnInitialEnemiesSystem, EnemySpawnerSystem, PlayerAttackSystem, DeathSystem, TeleportSystem, TilemapRenderBridge)
- [X] T013 Проверить детерминизм: один seed → одинаковая карта дважды (BaseLayer, WallMaskLayer, FloorVariantLayer, Rooms идентичны)

**Checkpoint**: Новый алгоритм генерации работает. Карта содержит разнообразные комнаты и разветвлённые коридоры. Все существующие системы функционируют корректно.

---

## Phase 3: User Story 1 — Разнообразная структура подземелья (Priority: P1) 🎯 MVP

**Goal**: Генерация подземелий с комнатами разного размера/формы (включая Г/Т-образные), коридорами разной ширины с развилками, типизированными комнатами. Визуально проверяемо через Play Mode.

**Independent Test**: Запустить забег → карта содержит комнаты минимум 3 размерных категорий, есть составные формы, коридоры разной ширины с развилками. Один seed дважды → идентичная карта.

### Implementation for User Story 1

- [X] T014 [US1] Верифицировать генерацию комнат 3+ размерных категорий (малые 4×4–6×6, средние 7×7–10×10, крупные 11×11+) — Play Mode проверка, при необходимости скорректировать параметры генерации в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T015 [US1] Верифицировать генерацию составных комнат (Г/Т-образные) — Play Mode проверка, при необходимости скорректировать логику composite room в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T016 [US1] Верифицировать коридоры: вариативная ширина (1–3), наличие развилок (петель из extra edges) — Play Mode проверка в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T017 [US1] Верифицировать гарантированную связность: все комнаты доступны из стартовой (MST гарантирует) — Play Mode проверка
- [X] T018 [US1] Верифицировать типизацию комнат: первая = Start, остальные = Normal/Treasure/HardEnemy — отладочный лог RoomBlob данных
- [X] T019 [US1] Верифицировать auto-расчёт targetRooms при разных размерах карты: маленькая (20×20), default (50×70), большая (100×100) — убедиться что количество комнат адаптируется к размеру и карта плотно заполнена в `Assets/_Project/Dots/Runtime/Systems/Initialization/MapGenerationSystem.cs`
- [X] T020 [US1] Верифицировать edge cases: маленькая карта (10×10) — минимум 1 комната с корректной связностью; неудачное размещение — fallback на стартовую комнату
- [X] T021 [US1] Обновить документацию механики генерации карты в `Docs/Mechanics/MapGeneration.md`

**Checkpoint**: Генерация подземелий работает, визуально проверена. Все acceptance scenarios US1 пройдены.

---

## Phase 4: User Story 2 — Визуальное разнообразие тайлов (Priority: P2)

**Goal**: Замена одноцветных квадратов на pixel art тайлсет с auto-tiling стен (bitmask), вариациями пола и декоративными элементами.

**Independent Test**: Запустить забег → полы имеют 3+ визуальных варианта, стены рендерятся по bitmask (16 вариантов), видны декоративные элементы (факелы, арки).

### Implementation for User Story 2

- [X] T022 [US2] Подготовить/импортировать pixel art тайлсет: 16 стеновых тайлов (4-bit bitmask), 3+ тайла пола (вариации), 3+ декоративных тайла (факелы, знамёна, дверные проёмы) в `Assets/_Project/Art/Tiles/`
- [X] T023 [US2] Переработать TilemapRenderBridge: вместо одноцветных тайлов — индексация Tile[] wallTilesByMask[16] по WallMaskLayer bitmask, Tile[] floorVariantTiles[N] по FloorVariantLayer в `Assets/_Project/Dots/Hybrid/Rendering/TilemapRenderBridge.cs`
- [X] T024 [US2] Добавить декоративный Tilemap-слой в TilemapRenderBridge: отрисовка декора на каждом переходе комната↔коридор (дверной проём или арка), детерминировано по seed в `Assets/_Project/Dots/Hybrid/Rendering/TilemapRenderBridge.cs`
- [X] T025 [US2] Верифицировать детерминизм вариаций пола: один seed → одинаковое расположение вариаций — Play Mode проверка
- [X] T026 [US2] Визуальное сравнение с референсами (`Docs/References/MapGeneration/`): стены, полы, декор приближены к скриншотам

**Checkpoint**: Визуальный стиль карты приближен к референсу. Pixel art тайлы вместо одноцветных квадратов.

---

## Phase 5: User Story 3 — Туман войны и освещение (Priority: P3)

**Goal**: Система тумана войны — symmetric shadowcasting, три состояния видимости (Unexplored/Explored/Visible), стены блокируют обзор, враги видны только в FOV.

**Independent Test**: Запустить забег → при старте 80%+ карты скрыто, при движении новые области открываются, стены блокируют видимость, пройденные области остаются видимыми (затемнёнными).

### Implementation for User Story 3

- [X] T027 [P] [US3] Создать VisibilityConfigData (ViewRadius: int = 8, Enabled: bool = true) в `Assets/_Project/Dots/Runtime/Components/Config/VisibilityConfigData.cs`
- [X] T028 [P] [US3] Создать VisibilityConfigAuthoring + Baker в `Assets/_Project/Dots/Authoring/Components/VisibilityConfigAuthoring.cs`
- [X] T029 [P] [US3] Создать FogRenderRequest (IComponentData, IEnableableComponent, пустой — триггер обновления) в `Assets/_Project/Dots/Runtime/Components/FogRenderRequest.cs`
- [X] T030 [US3] Создать FogOfWarSystem: [BurstCompile], [UpdateInGroup(FixedStepSimulation)], [UpdateAfter(MovementResolveSystem)], RequireForUpdate<MapBlobReference>. Persistent NativeArray<FogState> поле. Каждый кадр: Visible→Explored, затем symmetric shadowcasting от позиции игрока в `Assets/_Project/Dots/Runtime/Systems/Fixed/FogOfWarSystem.cs`
- [X] T031 [US3] Реализовать алгоритм symmetric shadowcasting (октантный) в FogOfWarSystem: проверка 8 октантов, O(r²) обработка, стены блокируют видимость, запись в NativeArray<FogState> в `Assets/_Project/Dots/Runtime/Systems/Fixed/FogOfWarSystem.cs`
- [X] T032 [US3] Создать FogRenderBridge (MonoBehaviour, Hybrid): читает NativeArray<FogState> из FogOfWarSystem, управляет alpha/tint тайлов (Unexplored=чёрный, Explored=затемнённый, Visible=полная видимость) в `Assets/_Project/Dots/Hybrid/Rendering/FogRenderBridge.cs`
- [X] T033 [US3] Интегрировать FogRenderBridge с TilemapRenderBridge: скрытие/показ SpriteRenderer врагов по FogState (Visible=показать, иначе=скрыть) в `Assets/_Project/Dots/Hybrid/Rendering/FogRenderBridge.cs`
- [X] T034 [US3] Проверить Burst-компиляцию FogOfWarSystem: read_console на отсутствие ошибок
- [X] T035 [US3] Верифицировать: стены блокируют видимость (коридорный «туннельный» эффект), враги скрыты вне FOV, edge case — узкий коридор (1 клетка) создаёт «туннельное зрение» — Play Mode проверка
- [X] T036 [US3] Создать документацию механики тумана войны в `Docs/Mechanics/FogOfWar.md`

**Checkpoint**: Туман войны функционирует. Неисследованные области скрыты, исследованные затемнены, стены блокируют обзор.

---

## Phase 6: User Story 4 — Миникарта (Priority: P4)

**Goal**: UI-оверлей миникарты (Runtime Texture2D), показывающий исследованную часть подземелья с маркерами сущностей, toggle через InputBridge.

**Independent Test**: Запустить забег → открыть миникарту → видны исследованные комнаты/коридоры бежевым, игрок — зелёная точка, враги в FOV — красные точки, закрыть миникарту → оверлей исчезает.

### Implementation for User Story 4

- [X] T037 [P] [US4] Создать MinimapToggleTag (IComponentData, IEnableableComponent, пустой) в `Assets/_Project/Dots/Runtime/Components/MinimapToggleTag.cs`
- [X] T038 [US4] Создать MinimapBridge (MonoBehaviour, Hybrid/UI): RawImage с Runtime Texture2D (MapSize.x × MapSize.y, RGBA32), читает NativeArray<FogState> + MapBlob + GridPosition сущностей в `Assets/_Project/Dots/Hybrid/UI/MinimapBridge.cs`
- [X] T039 [US4] Реализовать отрисовку миникарты: Unexplored=прозрачный, Explored=тёмно-бежевый, Visible=бежевый, маркеры: игрок=зелёный, враги=красный (только Visible клетки) в `Assets/_Project/Dots/Hybrid/UI/MinimapBridge.cs`
- [X] T040 [US4] Интегрировать toggle миникарты с InputBridge: клавиша Tab → enable/disable MinimapToggleTag → показать/скрыть RawImage оверлей в `Assets/_Project/Dots/Hybrid/UI/MinimapBridge.cs` и `Assets/_Project/Dots/Hybrid/Input/InputBridge.cs`
- [X] T041 [US4] Реализовать обновление текстуры: пересчёт только при изменении позиции игрока (FogRenderRequest-подобный триггер) в `Assets/_Project/Dots/Hybrid/UI/MinimapBridge.cs`
- [X] T042 [US4] Верифицировать миникарту: маркеры обновляются, toggle работает, неисследованные области не отображаются, edge case — первое открытие показывает только стартовую комнату — Play Mode проверка
- [X] T043 [US4] Создать документацию механики миникарты в `Docs/Mechanics/Minimap.md`

**Checkpoint**: Миникарта работает как UI-оверлей с маркерами сущностей и toggle.

---

## Phase 7: Верификация и документация

**Purpose**: Финальная проверка и документация (NON-NEGOTIABLE)

- [X] T044 Burst-компиляция: проверить read_console на ошибки во всех системах
- [X] T045 Managed-типы: убедиться нет string/class в Runtime/ assembly
- [X] T046 ECB: структурные изменения только через EntityCommandBuffer
- [X] T047 [P] Обновить индекс механик — добавить строки FogOfWar.md, Minimap.md, обновить строку MapGeneration.md в `Docs/RuntimeRoguelike_CurrentMechanics.md`
- [X] T048 Проверить ordering: [UpdateInGroup], [UpdateAfter/Before] для FogOfWarSystem (после MovementResolveSystem)
- [X] T049 Замерить производительность MapGenerationSystem.OnUpdate(): генерация карты ДОЛЖНА завершаться менее чем за 1 секунду (SC-005) для всех поддерживаемых размеров карты
- [X] T050 Запустить Play Mode: полный цикл забега — генерация → перемещение → fog → minimap → combat → pathfinding
- [X] T051 Проверить детерминизм: seed X дважды → идентичная карта + fog + вариации пола

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup (T001–T006) completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational (T007–T013) — верификация алгоритма генерации
- **US2 (Phase 4)**: Depends on Foundational (T007–T013) — нужны WallMaskLayer и FloorVariantLayer
- **US3 (Phase 5)**: Depends on Foundational (T007–T013) — нужен MapBlob с BaseLayer
- **US4 (Phase 6)**: Depends on US3 (T030–T033) — нужен NativeArray<FogState> из FogOfWarSystem
- **Verification (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational (Phase 2) — No dependencies on other stories
- **US2 (P2)**: Can start after Foundational (Phase 2) — No dependencies on US1 (uses WallMaskLayer/FloorVariantLayer from Foundational)
- **US3 (P3)**: Can start after Foundational (Phase 2) — No dependencies on US1/US2
- **US4 (P4)**: Depends on US3 (requires FogOfWarSystem NativeArray<FogState>) — cannot start until US3 T030–T033 complete

### Within Each User Story

- Components before systems (данные → логика)
- Systems before bridges (логика → визуализация)
- Burst-проверка после каждой системы
- Документация механик — до завершения story

### Parallel Opportunities

**Phase 1 (Setup)**:
```
T001 (MapTypes) ║ T002 (MapBlob) ║ T003 (MapUtilities)
     ↓                ↓                    ↓
     T004 (Config) → T005 (Authoring) → T006 (Compile check)
```

**Phase 2 (Foundational)**: Sequential — все задачи в одном файле (MapGenerationSystem.cs)

**User Stories (после Phase 2)**:
```
US1 (Phase 3) ║ US2 (Phase 4) ║ US3 (Phase 5)
                                      ↓
                                US4 (Phase 6)
```

**Phase 5 (US3)**:
```
T027 (VisibilityConfig) ║ T028 (Authoring) ║ T029 (FogRenderRequest)
            ↓
       T030 → T031 (FogOfWarSystem)
            ↓
       T032 → T033 (FogRenderBridge)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T006)
2. Complete Phase 2: Foundational (T007–T013) — CRITICAL
3. Complete Phase 3: US1 (T014–T021)
4. **STOP and VALIDATE**: Генерация подземелий работает, визуально проверена
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 → Test independently → Demo (MVP!)
3. Add US2 → Pixel art визуал → Demo
4. Add US3 → Туман войны → Demo
5. Add US4 (depends on US3) → Миникарта → Demo
6. Verification → Final release

### Parallel Execution (if team allows)

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: US1 (верификация генерации)
   - Developer B: US2 (тайлсет + рендеринг)
   - Developer C: US3 (туман войны)
3. After US3 complete: US4 (миникарта)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US4 зависит от US3 (FogOfWarSystem provides NativeArray<FogState>)
- US1, US2, US3 могут выполняться параллельно после Phase 2
- MapGenerationSystem НЕ помечен [BurstCompile] — существующий паттерн (BlobBuilder + EntityManager)
- FogOfWarSystem ДОЛЖЕН быть [BurstCompile] — чистая арифметика, NativeArray
- NativeArray<FogState> — persistent поле в ISystem (паттерн из EnemyPathfindSystem)
- Тайлсет: минимум 22 спрайта (16 стен + 3 полов + 3 декор), путь: `Assets/_Project/Art/Tiles/`
