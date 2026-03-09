## Runtime Roguelike 2D — текущие механики

Документ описывает **геймплейные механики**, которые уже реализованы в проекте.
Детальное описание каждой механики — в отдельных файлах ниже.

**Важно**: при изменении любой механики ОБЯЗАТЕЛЬНО обновить
соответствующий файл документации в `Docs/Mechanics/`.

---

## Общая идея забега

- **Real-time roguelike**: игра идёт в реальном времени (без пошагового режима).
- **Runtime-only контент**: карта, визуал тайлов/сущностей и UI создаются во время запуска забега.
- **Простая процедурная графика**: пол, стены, игрок, враги, лут и ловушки визуально различимы.
- **Забег детерминируется seed'ом**: при одинаковом seed базовая раскладка карты воспроизводима.

---

## Механики

| Механика | Описание | Документация |
|----------|----------|--------------|
| Цикл забега | Старт, рестарт без перезагрузки сцены | [RunCycle.md](Mechanics/RunCycle.md) |
| Генерация карты | Процедурная, комнаты + MST-коридоры, bitmask auto-tiling, типы комнат | [MapGeneration.md](Mechanics/MapGeneration.md) |
| Передвижение | WASD, 4 направления, occupancy | [Movement.md](Mechanics/Movement.md) |
| Игрок | HP, Gold, XP/Level | [Player.md](Mechanics/Player.md) |
| Бой | Атака игрока/врагов, кулдауны, смерть | [Combat.md](Mechanics/Combat.md) |
| Враги | Спавн, AI (aggro, pathfinding, idle) | [Enemies.md](Mechanics/Enemies.md) |
| Ловушки | Spike (instant + periodic), Poison (DoT) | [Hazards.md](Mechanics/Hazards.md) |
| Лут | Gold, XP, Heal — drop и pickup | [Loot.md](Mechanics/Loot.md) |
| Перки | Level-up, выбор из 3 перков | [Perks.md](Mechanics/Perks.md) |
| Сложность | Рост со временем и уровнем | [Difficulty.md](Mechanics/Difficulty.md) |
| HUD | HP, XP, Level, Gold, Seed, Restart | [HUD.md](Mechanics/HUD.md) |
| DevTools | F5, G, T — отладка | [DevTools.md](Mechanics/DevTools.md) |
| Туман войны | Symmetric shadowcasting, 3 состояния видимости | [FogOfWar.md](Mechanics/FogOfWar.md) |
| Миникарта | UI-оверлей, Runtime Texture2D, маркеры сущностей | [Minimap.md](Mechanics/Minimap.md) |

---

## Добавление новой механики

1. Создать файл `Docs/Mechanics/<НазваниеМеханики>.md`
2. Добавить строку в таблицу выше
3. При изменении существующей механики — обновить соответствующий файл
