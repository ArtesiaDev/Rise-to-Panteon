# Rise to Panteon — Руководство для агента

## Проект

2D roguelike на Unity 2022.3 LTS с DOTS (Entities 1.4.x).
Гибридная архитектура: ECS-симуляция + GameObject-рендеринг.

## Правила работы с ECS

При работе с ECS используй Unity DOTS. Это обязательно.

### Документация проекта

Пути к документации по правильному использованию:
1. `/Docs/Unity Entities 101.md` — основы Unity Entities
2. `/Docs/UnityJobSystem.md` — Job System
3. `/Docs/UnityPhysics.md` — Unity Physics

Если в документах встречаются ссылки на другую документацию —
переходи по ним только если действительно нужна информация.

### Скилл unity-ecs-patterns

Используй скилл `unity-ecs-patterns` при работе с ECS.
Он содержит проверенные паттерны и best practices.
Расположен в `/.cursor/skills/unity-ecs-patterns/`.

### Официальная документация

При необходимости обращайся к официальной документации:
https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/index.html

## Конституция проекта

Принципы и ограничения описаны в `.specify/memory/constitution.md`.
Все архитектурные решения ДОЛЖНЫ соответствовать конституции.

## Структура проекта

```
Assets/_Project/Dots/
├── Runtime/        — IComponentData + ISystem (Burst-совместимые)
│   ├── Components/ — чистые struct-компоненты
│   ├── Systems/    — системы симуляции
│   ├── Map/        — BlobAsset карты
│   └── Navigation/ — A* pathfinding
├── Hybrid/         — MonoBehaviour-мосты (Input, Rendering, UI)
├── Authoring/      — MonoBehaviour + Baker
└── Baking/         — BakingSystem расширения
```

## Документация геймплейных механик

**ОБЯЗАТЕЛЬНОЕ ПРАВИЛО**: при любом изменении геймплейной механики
НЕОБХОДИМО обновить соответствующий файл документации.

- Индекс механик: `/Docs/RuntimeRoguelike_CurrentMechanics.md`
- Детали каждой механики: `/Docs/Mechanics/<Название>.md`

При изменении механики:
1. Обновить файл в `Docs/Mechanics/` для затронутой механики
2. Если добавлена новая механика — создать новый файл и добавить
   строку в таблицу индекса

При удалении механики:
1. Удалить файл из `Docs/Mechanics/`
2. Убрать строку из таблицы индекса

## Ключевые соглашения

- Компоненты: `struct IComponentData` без managed-полей
- Системы: `partial struct : ISystem` с `[BurstCompile]`
- Структурные изменения: только через `EntityCommandBuffer`
- Рандом: только `Unity.Mathematics.Random` (детерминизм по seed)
- Namespace: `RuntimeRoguelike.Dots.{Runtime|Hybrid|Authoring|Baking}`
- Именование: PascalCase, суффиксы `System`, `Config`, `State`, `Tag`
- Комментарии: на русском языке

## Active Technologies
- C# (.NET Standard 2.1) / Unity 2022.3 LTS + Unity.Entities 1.4.x, Unity.Burst, Unity.Collections, Unity.Mathematics (001-dots-audit-refactor)
- BlobAssetReference<MapBlob> для данных карты, IComponentData для состояния (001-dots-audit-refactor)
- BlobAssetReference<MapBlob> для карты, (002-map-generation-redesign)

## Recent Changes
- 001-dots-audit-refactor: Added C# (.NET Standard 2.1) / Unity 2022.3 LTS + Unity.Entities 1.4.x, Unity.Burst, Unity.Collections, Unity.Mathematics
