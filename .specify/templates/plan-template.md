# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

**Language/Version**: C# (.NET Standard 2.1) / Unity 2022.3 LTS
**Primary Dependencies**: Unity.Entities 1.4.x, Unity.Burst,
Unity.Collections, Unity.Mathematics, URP 2D
**Storage**: BlobAssetReference<MapBlob> для карты,
IComponentData для состояния, singleton-компоненты для глобалов
**Testing**: Unity Test Runner (EditMode + PlayMode)
**Target Platform**: Unity 2D (PC/Mac)
**Project Type**: 2D roguelike game (ECS + Hybrid rendering)
**Performance Goals**: 60 FPS, детерминированная симуляция по seed
**Constraints**: Burst-совместимый код, без managed-типов в
компонентах, ECB для структурных изменений
**Scale/Scope**: 200x200 процедурная карта, 4 assembly (Runtime,
Hybrid, Authoring, Baking)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Принцип | Проверка | Статус |
|---|---------|----------|--------|
| I | DOTS-First | Вся логика через ECS (IComponentData + ISystem), MonoBehaviour только в Hybrid | ☐ |
| II | Simulation/Presentation | Данные текут Runtime → Hybrid, обратных зависимостей нет | ☐ |
| III | Детерминизм | Только Unity.Mathematics.Random, фиксированный порядок систем | ☐ |
| IV | Burst | [BurstCompile] на всех системах (или комментарий с обоснованием) | ☐ |
| V | Порядок систем | [UpdateInGroup], [UpdateAfter/Before], RequireForUpdate | ☐ |
| VI | YAGNI | Нет абстракций «на будущее», минимальная сложность | ☐ |
| — | Документация механик | Обновлены Docs/Mechanics/ и индекс (если затронута механика) | ☐ |

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Assets/_Project/Dots/
├── Runtime/
│   ├── Components/        — struct IComponentData
│   ├── Components/Config/ — singleton-конфигурации
│   ├── Systems/Fixed/     — FixedStepSimulationSystemGroup
│   ├── Systems/Initialization/ — InitializationSystemGroup
│   ├── Systems/Presentation/   — PresentationSystemGroup
│   ├── Map/               — BlobAsset карты
│   └── Navigation/        — A* pathfinding
├── Hybrid/
│   ├── Input/             — InputBridge
│   ├── Rendering/         — Sprite/Tilemap мосты
│   ├── UI/                — HUD, PerkUI мосты
│   ├── Presentation/      — камера, cleanup
│   └── Debug/             — Gizmos
├── Authoring/
│   └── Components/        — MonoBehaviour + Baker
└── Baking/                — BakingSystem расширения

Docs/Mechanics/            — документация механик (NON-NEGOTIABLE)
```

**Structure Decision**: 4-assembly DOTS-архитектура. Новые
компоненты — в `Runtime/Components/`, системы — в соответствующую
группу `Systems/`, мосты — в `Hybrid/`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., managed-тип в компоненте] | [необходимость строковых данных] | [FixedString недостаточен из-за...] |
| [e.g., EntityManager вместо ECB] | [BlobBuilder требует прямого доступа] | [ECB не поддерживает BlobBuilder] |
