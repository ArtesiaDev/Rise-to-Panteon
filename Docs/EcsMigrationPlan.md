# План миграции на ECS

## Цель
Перенести весь геймплей в кастомный ECS, оставить UI на MVP, а визуал — в тонком presentation‑контуре.

## Пошаговая миграция
1. Включить новый рантайм ECS (уже сделано в `MainInstaller` через `EcsBootstrapper`) и проверить запуск сцены.
2. Проверить базовый цикл забега:
   - генерация карты и тайлмапы
   - спавн игрока/врагов
   - движение по сетке и occupancy
3. Проверить бой:
   - атака игрока, урон врагам
   - атака врагов по кулдауну
   - смерть и удаление сущностей
4. Проверить hazards:
   - spike урон при входе и по тикам
   - poison DoT
5. Проверить лут:
   - шанс дропа
   - подбор и эффекты (gold/xp/heal)
6. Проверить прокачку и перки:
   - XP → level‑up → оффер перков
   - применение перков
7. Проверить рост сложности и спавн:
   - множители сложности
   - периодический доспавн
8. Проверить UI:
   - HUD (HP/XP/Level/Gold/Seed)
   - окно выбора перка
9. Проверить DevTools:
   - restart
   - gizmos
   - teleport
10. После стабилизации — удалить/отключить старые MonoBehaviour‑скрипты (см. список ниже).

## Классы на удаление/замену

### Полностью заменены ECS‑слоем
- `Assets/_Project/Scripts/Core/RunController.cs` → `EcsBootstrapper`, `EcsWorldBehaviour`
- `Assets/_Project/Scripts/Entities/Player/PlayerController.cs`
- `Assets/_Project/Scripts/Entities/Player/PlayerAttack.cs`
- `Assets/_Project/Scripts/Entities/Player/PlayerStats.cs`
- `Assets/_Project/Scripts/Entities/Enemy/EnemyAI.cs`
- `Assets/_Project/Scripts/Entities/Enemy/EnemyFactory.cs`
- `Assets/_Project/Scripts/Systems/EnemySpawner.cs`
- `Assets/_Project/Scripts/Systems/HazardSystem.cs`
- `Assets/_Project/Scripts/Systems/LevelSystem.cs`
- `Assets/_Project/Scripts/Systems/DifficultyService.cs`
- `Assets/_Project/Scripts/Loot/LootDropper.cs`
- `Assets/_Project/Scripts/Loot/Pickup.cs`
- `Assets/_Project/Scripts/Combat/Health.cs`
- `Assets/_Project/Scripts/Combat/StatusEffects.cs`
- `Assets/_Project/Scripts/Dev/DevTools.cs`
- `Assets/_Project/Scripts/UI/HudController.cs`
- `Assets/_Project/Scripts/UI/PerkSelectionUI.cs`

### Можно оставить (используются ECS‑слоем)
- `Assets/_Project/Scripts/Map/MapGenerator.cs`
- `Assets/_Project/Scripts/Map/HazardGenerator.cs`
- `Assets/_Project/Scripts/Map/MapGrid.cs`
- `Assets/_Project/Scripts/Navigation/AStarPathfinder.cs`
- `Assets/_Project/Scripts/Navigation/GridOccupancy.cs`
- `Assets/_Project/Scripts/Navigation/GridPositionConverter.cs`
- `Assets/_Project/Scripts/Rendering/SpriteFactory.cs`
- `Assets/_Project/Scripts/Rendering/TilemapWorldRenderer.cs`
- `Assets/_Project/Scripts/UI/UiFactory.cs`
- все `Configs/*` ScriptableObject

### Потенциальные кандидаты на удаление (если не используются)
- `Assets/_Project/Scripts/Rendering/CameraFollow.cs` (заменён `CameraFollowSystem`)

