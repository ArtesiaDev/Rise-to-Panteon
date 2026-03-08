# Туман войны (Fog of War)

## Обзор

Система тумана войны скрывает неисследованные области карты и ограничивает видимость игрока.
Используется алгоритм **symmetric shadowcasting** (октантный) — O(r²) на кадр, Burst-совместим.

## Состояния видимости

| Состояние | Описание | Визуал |
|-----------|----------|--------|
| Unexplored | Ещё не видел | Чёрный (непрозрачный) |
| Explored | Видел ранее | Затемнённый (полупрозрачный) |
| Visible | Видит сейчас | Полностью видимый |

## Алгоритм

1. Каждый кадр все клетки со состоянием **Visible** переводятся в **Explored**.
2. Symmetric shadowcasting от позиции игрока в радиусе `ViewRadius` помечает видимые клетки как **Visible**.
3. Стены блокируют обзор — создаётся эффект «туннельного зрения» в коридорах.
4. Пересчёт выполняется только при перемещении игрока.

## Влияние на геймплей

- Враги скрыты вне поля зрения (SpriteRenderer отключается через FogRenderBridge).
- Исследованные области остаются видимыми (затемнёнными) — игрок помнит layout.
- При старте забега ~80%+ карты скрыто.

## Конфигурация

Параметры в `VisibilityConfigAuthoring` (Inspector):
- `ViewRadius` (int, default 8) — радиус видимости в клетках.
- `Enabled` (bool, default true) — включить/выключить туман войны.

## Реализация

- **ECS система**: `FogOfWarSystem` (ISystem, [BurstCompile], FixedStepSimulation)
- **Хранение**: `NativeArray<FogState>` — persistent поле в ISystem (паттерн из EnemyPathfindSystem)
- **Рендеринг**: `FogRenderBridge` (MonoBehaviour, Hybrid) — overlay Tilemap + скрытие врагов
- **Ordering**: `[UpdateAfter(MovementResolveSystem)]`
