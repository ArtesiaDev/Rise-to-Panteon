# Rise to Panteon — Руководство для агента

## Проект

2D roguelike на Unity 6.6 (6000.6.3f1) с DOTS (Entities 6.6 — core-пакет движка).
Гибридная архитектура: ECS-симуляция + GameObject-рендеринг.

## Правила работы с ECS

При работе с ECS используй Unity DOTS. Это обязательно.

### Документация проекта

Пути к документации по правильному использованию:
1. `/Docs/Tech/Reference/Unity/Entities.md` — Unity Entities 6.6: что используем и как
2. `/Docs/Tech/Reference/Unity/JobsAndBurst.md` — Job System, Burst, Collections

Архитектурные решения проекта — `/Docs/Tech/ArchitectureDecisions.md`.
`/Docs/Tech/Reference/WO_ProductionArchitecture.md` — чужой проект, только референс.

Если в документах встречаются ссылки на другую документацию —
переходи по ним только если действительно нужна информация.

### Официальная документация

При необходимости обращайся к официальной документации:
https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/index.html

С Unity 6.4 Entities встроен в движок: `Entities.ForEach`, `Job.WithCode`
и Aspects удалены, managed-компоненты устарели. Гайд по миграции:
https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/upgrade-guide.html

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

Концепт игры переработан (сентябрь 2026): RPG-песочница с готовыми ассетами,
без генерации визуала в рантайме. Текущий код в `Assets/_Project/` — прототип
старой концепции, техническая база будет пересмотрена.

Источник истины по геймдизайну — `/Docs/GDD/`:
- Видение, столпы, индекс механик: `/Docs/GDD/README.md`
- Детали каждой механики: `/Docs/GDD/<Название>.md`
- Нерешённое: `/Docs/GDD/OpenQuestions.md`
- `/Docs/Parked/` — отложенные F2P/мультиплеер-механики, НЕ источник истины

**ОБЯЗАТЕЛЬНОЕ ПРАВИЛО**: при любом изменении геймплейной механики
НЕОБХОДИМО обновить соответствующий файл документации.

При изменении механики:
1. Обновить файл в `Docs/GDD/` для затронутой механики
2. Если добавлена новая механика — создать новый файл и добавить
   строку в таблицу документов в `Docs/GDD/README.md`

При удалении механики:
1. Удалить файл из `Docs/GDD/`
2. Убрать строку из таблицы документов

## Ключевые соглашения

- Компоненты: `struct IComponentData` без managed-полей
- Системы: `partial struct : ISystem` с `[BurstCompile]`
- Структурные изменения: только через `EntityCommandBuffer`
- Рандом: только `Unity.Mathematics.Random` (детерминизм по seed)
- Ввод: только Input System — project-wide actions в
  `Assets/Settings/InputSystem_Actions.inputactions` (`InputSystem.actions`);
  `UnityEngine.Input` и `StandaloneInputModule` не использовать
- Enter Play Mode без перезагрузки домена: static-состояние сбрасывать
  в `OnDestroy` или через `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`
- Namespace: `RuntimeRoguelike.Dots.{Runtime|Hybrid|Authoring|Baking}`
- Именование: PascalCase, суффиксы `System`, `Config`, `State`, `Tag`
- Комментарии: на русском языке

## Active Technologies
- C# (.NET Standard 2.1) / Unity 6.6 (6000.6.3f1) + Unity.Entities 6.6 (core), Unity.Burst 2.0, Unity.Collections 6.6, Unity.Mathematics, URP 17.6 (unity6-upgrade)

## Recent Changes
- unity6-upgrade: Unity 2022.3.62f2 → 6000.6.3f1, Entities 1.4.2 → 6.6.0 (core), URP 14 → 17.6, Addressables 1.22 → 2.11, TextMeshPro → uGUI 2.6
