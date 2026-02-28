# Debug / DevTools (для тестирования)

DevTools можно включать/выключать настройкой.

## Реализация (DOTS)

- **InputBridge** (MonoBehaviour) — читает ввод с клавиатуры и записывает в `InputState` singleton
- **InputReadSystem** (SimulationSystemGroup) — обрабатывает движение и атаку
- **TeleportSystem** (SimulationSystemGroup, после InputReadSystem) — обрабатывает телепорт
- **RestartRequestSystem** (SimulationSystemGroup) — обрабатывает рестарт

## Доступные действия (дефолтные хоткеи):

- **R** — restart забега (очистка всех run-entities, перегенерация карты)
- **G** — переключить отладочные gizmos
- **T** — телепорт игрока на случайную свободную клетку (floor + нет occupancy).
  InputBridge ищет свободную клетку (до 100 попыток), записывает `TeleportRequested = true`
  и `TeleportTarget` в `InputState`. `TeleportSystem` обновляет occupancy,
  `GridPosition`, `PreviousGridPosition` и `RenderPosition` игрока.

## Отладочный вывод на экране:
- seed
- координаты клетки игрока
- текущее количество врагов
- текущее количество предметов лута на карте.
