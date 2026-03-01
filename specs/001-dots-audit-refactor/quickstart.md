# Quickstart: DOTS Audit & Refactor

## Что делаем

Рефакторинг 22 DOTS-систем для полного соответствия Unity Entities 1.4 best practices:
1. Замена EntityManager вызовов на SystemAPI в 9 системах
2. Добавление [BurstCompile] на методы OnUpdate/OnCreate/OnDestroy
3. Обновление документации миграционного плана

## Ключевые паттерны замены

### Паттерн A: GetBuffer → GetSingletonBuffer

```csharp
// БЫЛО:
var occupancy = state.EntityManager.GetBuffer<CellOccupant>(
    SystemAPI.GetSingletonEntity<RunState>());

// СТАЛО:
var occupancy = SystemAPI.GetSingletonBuffer<CellOccupant>();
```

### Паттерн B: EntityManager.Exists → SystemAPI.HasComponent

```csharp
// БЫЛО:
if (!state.EntityManager.Exists(target.ValueRO.Value))

// СТАЛО:
if (!SystemAPI.HasComponent<Health>(target.ValueRO.Value))
```

### Паттерн C: Добавление [BurstCompile] на методы

```csharp
// БЫЛО:
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state) { ... }
}

// СТАЛО:
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state) { ... }
}
```

### Паттерн D: RestartSystem — замены

```csharp
// HasBuffer
state.EntityManager.HasBuffer<PerkOption>(entity)
→ SystemAPI.HasBuffer<PerkOption>(entity)

// GetBuffer.Clear
state.EntityManager.GetBuffer<PerkOption>(entity).Clear()
→ SystemAPI.GetBuffer<PerkOption>(entity).Clear()

// SetComponentEnabled
state.EntityManager.SetComponentEnabled<MapRenderRequest>(entity, true)
→ SystemAPI.SetComponentEnabled<MapRenderRequest>(entity, true)
```

## Порядок работы

1. **Сначала** замени EntityManager → SystemAPI (иначе BurstCompile на OnUpdate не скомпилируется)
2. **Потом** добавь [BurstCompile] на методы
3. **В конце** обнови документацию

## Верификация

После каждого изменения:
- Проект компилируется без ошибок
- Burst Inspector показывает компиляцию системы
- Запуск с seed=42 — поведение не изменилось
