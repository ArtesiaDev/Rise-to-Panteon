# Rise to Panteon — Руководство для агента

## Проект

2D RPG-песочница на Unity 6.6 (6000.6.3f1) для iOS и Android, одиночная игра.
Вся игровая логика — Unity ECS (Entities 6.6); отрисовка (URP 2D), UI (UI Toolkit) и сервисы (VContainer) —
отдельные ООП-контуры, связанные только данными.

## Архитектура — читать первой

`/Docs/Tech/Architecture/README.md` — контуры, каркас, канонические имена, правила ARCH-xx; оттуда ссылки на
документы контуров (`Simulation.md`, `Presentation.md`, `UI.md`, `Content.md`, `Services.md`,
`CodeStructure.md`). Любой новый код пишется только по ним. Почему принято то или иное решение —
`/Docs/Tech/ArchitectureDecisions.md`.

Код в `Assets/_Project/{Dots,Framework,Main,Dev,Editor}` — прототип старой концепции (legacy): новый код на него
не ссылается и не берёт из него образцы. Конституция `.specify/memory/constitution.md` пока описывает этот
прототип и будет переписана вместе с харнессом агентов; при расхождении приоритет у `/Docs/Tech/Architecture/`.

## Справочники по ECS

Справочники по API (что и как работает в 6.6):
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

## Структура проекта

Папки, сборки, неймспейсы, именование и пошаговое «как добавить фичу» — `/Docs/Tech/Architecture/CodeStructure.md`.
Инфраструктура — `Assets/_Project/Code/<Контур>/`, фичи — `Assets/_Project/Features/<Фича>/<Контур>/`,
конфиги — git submodule `Configs/` в корне репозитория.

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

Полные правила — ARCH-xx в `/Docs/Tech/Architecture/README.md` и правила документов контуров. Кратко:
- Контуры общаются только типами `Contracts`: из симуляции — снимок, события, read-модели; в симуляцию —
  `PlayerInputFrame` и `Operation`. Кроме моста, никто не обращается к ECS.
- Системы симуляции — `ISystem` + `[BurstCompile]`, работа в джобах; структурные изменения — через ECB тика
- Рандом: только `Unity.Mathematics.Random` с состоянием в компонентах (детерминизм по seed)
- Ввод: только Input System (`IInputService`, project-wide actions
  `Assets/Settings/InputSystem_Actions.inputactions`); `UnityEngine.Input` и `StandaloneInputModule` не использовать
- Enter Play Mode без перезагрузки домена: static-состояние сбрасывать
  в `OnDestroy` или через `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`
- Namespace: `RiseToPanteon.<Контур>` и `RiseToPanteon.<Фича>.<Контур>`; именование — `CodeStructure.md` §5
- Числа баланса — только в `Configs/`; текст для игрока — только ключи локализации
- Комментарии: на русском языке

## Active Technologies
- C# (.NET Standard 2.1) / Unity 6.6 (6000.6.3f1) + Unity.Entities 6.6 (core), Unity.Burst 2.0, Unity.Collections 6.6, Unity.Mathematics, URP 17.6 (unity6-upgrade)

## Recent Changes
- unity6-upgrade: Unity 2022.3.62f2 → 6000.6.3f1, Entities 1.4.2 → 6.6.0 (core), URP 14 → 17.6, Addressables 1.22 → 2.11, TextMeshPro → uGUI 2.6
