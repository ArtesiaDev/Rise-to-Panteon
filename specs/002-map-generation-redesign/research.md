# Research: Редизайн генерации карты

**Date**: 2026-03-01 | **Branch**: `002-map-generation-redesign`

## R1: Алгоритм Fog of War (grid-based LOS)

### Decision: Symmetric Shadowcasting (октантный алгоритм)

### Rationale
- O(r^2) — при радиусе 8 обрабатывает ~200 клеток, O(1) на каждую
- Полностью Burst-совместим — чистая целочисленная арифметика, NativeArray
- Симметричная видимость — клетка A видна из B тогда и только тогда, когда B видна из A
- Соответствует паттерну EnemyPathfindSystem — persistent NativeArray поля в ISystem

### Alternatives Considered
| Алгоритм | Burst | Производительность | Качество | Отклонён потому что |
|-----------|-------|-------|----------|---------------------|
| Bresenham LOS per cell | Да | O(r^3) ~4600 cell reads | Асимметричный | Визуальные артефакты, в 20x медленнее |
| BFS Flood-fill | Частично | O(r^2) | Некорректный | Моделирует связность, а не видимость |

### Архитектурное решение
- `FogState` enum (byte): Unexplored=0, Explored=1, Visible=2
- Хранение: `NativeArray<FogState>` как persistent-поле в `FogOfWarSystem` (аналогично A* массивам в EnemyPathfindSystem)
- НЕ в MapBlob (BlobArray — read-only, fog изменяется каждый ход)
- Каждый кадр: Visible → Explored, затем пересчёт shadowcasting от позиции игрока
- Hybrid-мост читает массив для управления видимостью тайлов

---

## R2: Auto-tiling стен и вариации полов

### Decision: Bitmask подход — расчёт маски в генераторе, индексация в мосту

### Rationale
- Расчёт маски — чистая арифметика, помещается в BlobBuilder рядом с BaseLayer
- Прямая индексация Tile[] по bitmask — O(1) при рендеринге
- Полный контроль над логикой (в отличие от RuleTile, который «чёрный ящик»)
- Детерминированные вариации пола через hash(seed, cell) — без Random state

### Alternatives Considered
| Подход | Burst | Контроль | Floor варианты | Отклонён потому что |
|--------|-------|----------|----------------|---------------------|
| Unity RuleTile | Нет (managed SO) | Опосредованный | Неудобно с seed | Недетерминированные вариации, непрозрачная логика |

### Архитектурное решение

**Новые поля в MapBlob:**
- `BlobArray<byte> WallMaskLayer` — 4-bit маска (N=1, E=2, S=4, W=8) для каждой стены
- `BlobArray<byte> FloorVariantLayer` — индекс варианта пола (0..N-1), рассчитан по hash(seed, cell)

**Расчёт масок:**
- `ComputeWallMasks()` — static метод в MapGenerationSystem, проверяет 4 кардинальных соседа
- `ComputeFloorVariants()` — xxHash от seed+cell, Burst-совместим
- Результаты запекаются в BlobBuilder вместе с BaseLayer

**Тайлсет (минимум для 4-bit):**
- 16 стеновых тайлов (по одному на каждую bitmask-комбинацию)
- 3 тайла пола (обычный, трещины, узор)
- 3+ декоративных тайлов (факелы, знамёна, дверные проёмы)
- Итого минимум: ~22 уникальных спрайта

---

## R3: Алгоритм генерации комнат (расширенный)

### Decision: BSP + составные комнаты + MST коридоры

### Rationale
Текущий алгоритм (random placement + L-shaped corridors) слишком прост для референса.
Нужны: разнообразие форм, плотное заполнение, разветвлённые коридоры.

### Архитектурное решение

**Генерация комнат:**
1. Рассчитать целевое количество комнат: `targetRooms = (mapArea / avgRoomArea) * densityFactor`
2. Размещать прямоугольные комнаты random placement (текущий подход, расширить)
3. Для Г/Т-образных: с вероятностью `compositeChance` объединить два перекрывающихся прямоугольника
4. Хранить комнаты как `NativeList<RoomData>` при генерации

**Коридоры:**
1. Построить граф комнат (центры)
2. Minimum Spanning Tree (Prim/Kruskal) — гарантирует связность
3. Добавить ~20% дополнительных рёбер (random) — создаёт петли и развилки
4. Рисовать коридоры шириной 1–3 клетки (random per edge)

**Типизация комнат:**
- Первая комната = стартовая
- Вероятностный выбор: обычная 60%, сокровищница 15%, усиленные враги 25%
- Тип хранится в `BlobArray<RoomBlob>` внутри MapBlob

**Данные комнаты (BlobAsset):**
```
struct RoomBlob { int4 Bounds; byte RoomType; byte ConnectedRoomCount; }
```

---

## R4: Миникарта

### Decision: UI-оверлей через RawImage + Runtime Texture2D

### Rationale
- Hybrid-слой (MonoBehaviour) — конституция разрешает UI в Hybrid
- Runtime Texture2D — пиксели соответствуют клеткам карты 1:1
- Обновление: записать в текстуру только Explored/Visible клетки + маркеры
- Минимальная сложность, не требует дополнительной камеры

### Архитектурное решение
- `MinimapBridge` (MonoBehaviour, Hybrid/UI/) читает FogState NativeArray + MapBlob
- `Texture2D` размером MapSize.x × MapSize.y, формат RGBA32
- Цвета: Unexplored = прозрачный, Explored = тёмно-бежевый, Visible = бежевый
- Маркеры: игрок = зелёный пиксель, враги = красный (только Visible клетки)
- Toggle через InputBridge → `MinimapToggleTag` IComponentData
- Обновление текстуры: только при изменении позиции игрока (MapRenderRequest-подобный триггер)

---

## R5: Совместимость с существующими системами

### Decision: Обратная совместимость MapBlob — добавление полей, без удаления

### Rationale
10 систем читают MapBlob. Базовые поля (Size, StartCell, BaseLayer, ObstacleLayer, HazardLayer) остаются без изменений. Новые поля добавляются к BlobBuilder.

### Затронутые системы
| Система | Изменения |
|---------|-----------|
| MapGenerationSystem | Полная переработка алгоритма генерации + расчёт новых слоёв |
| TilemapRenderBridge | Замена одноцветных тайлов на спрайты по bitmask/variant |
| EnemyPathfindSystem | Без изменений (читает BaseLayer, ObstacleLayer, Size) |
| MovementResolveSystem | Без изменений |
| HazardSystem | Без изменений |
| SpawnPlayerSystem | Без изменений |
| SpawnInitialEnemiesSystem | Адаптация: спаун с учётом типа комнаты |
| EnemySpawnerSystem | Адаптация: спаун с учётом типа комнаты |
| PlayerAttackSystem | Без изменений |
| DeathSystem | Без изменений |
| TeleportSystem | Без изменений |

### Новые системы
| Система | Группа | Описание |
|---------|--------|----------|
| FogOfWarSystem | FixedStepSimulation | Расчёт видимости shadowcasting |
| FogRenderBridge | Hybrid/Rendering | Обновление видимости тайлов |
| MinimapBridge | Hybrid/UI | Рендеринг миникарты |

### Новые компоненты
| Компонент | Тип | Описание |
|-----------|-----|----------|
| VisibilityConfigData | IComponentData (singleton) | Радиус видимости, параметры FoW |
| MinimapToggleTag | IComponentData, IEnableableComponent | Состояние миникарты (вкл/выкл) |
| FogRenderRequest | IComponentData, IEnableableComponent | Триггер обновления fog-рендера |
