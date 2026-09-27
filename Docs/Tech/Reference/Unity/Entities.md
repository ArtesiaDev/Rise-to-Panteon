# Unity Entities 6.6 — справочник для агентов

> Unity 6000.6.3f1, `com.unity.entities` 6.6.0 (core package), Collections 6.6.0, Burst 2.0.0; iOS/Android. Проверено 2026-09-27.
> Burst 2.0.0 — пакет-**shim**: только `TypeForwardedTo` на встроенный в движок Burst. API (`[BurstCompile]`, `SharedStatic`, `FunctionPointer`) прежнее, исходников и документации Burst в `PackageCache` нет.
>
> **Источник истины** — исходники `Library/PackageCache/com.unity.entities@de96d69a35c5/` (дальше: `PackageCache`). Мануал 6.6 местами устарел: страница SystemBase упоминает `Job.WithCode`, страница `SystemAPI.Query` перечисляет `IAspect`, `whats-new` описывает 1.4. При расхождении верь исходникам. Непроверенное помечено **«не проверено»**.

---

## 1. Назначение и что в проекте НЕ используется

ECS в проекте — **только симуляция игровой логики**. Рендер — отдельный OOP-слой (URP 2D Renderer + GameObjects через Addressables). Все сущности создаются в рантайме: из JSON-конфигов, генерации мира и сейвов. Burst и jobs используются с первого дня.

| Не используем | Почему |
| --- | --- |
| SubScenes, Bakers, baking systems, `TransformUsageFlags` | Сущности создаются в рантайме из данных, а не из сцен |
| Entities content management (`WeakObjectReference`, `RuntimeContentManager`, content archives) | Ассеты грузит OOP-слой через Addressables |
| Entities Graphics, companion components | Рендер живёт в OOP-слое, симуляция о нём не знает |
| Managed components: `class : IComponentData`, struct с managed-полями, managed `ISharedComponentData`, `AddComponentObject`, `SystemAPI.ManagedAPI` | Deprecated в 6.6, не работают в Burst/jobs, создают GC-нагрузку |
| `UnityObjectRef<T>` в компонентах симуляции | Ссылки на ассеты нужны только presentation; в компонентах храним стабильные ID |
| Aspects (`IAspect`), `Entities.ForEach`, `Job.WithCode` | Удалены в 6.5 |
| Unity Physics | Не в стеке проекта |
| `World.DefaultGameObjectInjectionWorld` в игровом коде, хелперы VContainer `*DefaultWorld*` | Статический доступ ломает мульти-мир и серверный сценарий (см. §2.6) |
| Journaling (`EntitiesJournaling`, окно Journaling) | Deprecated в 6.5; вместо него `IDebugOnAdded/IDebugOnRemoved` (§8) |
| `PlaybackPolicy`, `EntityQueryCaptureMode.AtRecord` | Obsolete (§7) |

Необязательная страховка: scripting define `UNITY_DISABLE_MANAGED_COMPONENTS` заставляет `TypeManager` бросать исключение на `class : IComponentData` (проверено в `Types/TypeManager.cs`). Перед включением убедиться, что сторонние пакеты не объявляют managed-компоненты (не проверено).

Источники: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/upgrade-guide.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-managed.html>

---

## 2. Мир и бутстрап

### 2.1 World

`World` — контейнер сущностей и систем. Основное API: `new World(name, WorldFlags)`, `EntityManager`, `Unmanaged` (`WorldUnmanaged`, доступен из Burst), `Time`, `Update()`, `Dispose()`, `GetOrCreateSystem<T>()`, `GetExistingSystem<T>()`, `GetExistingSystemManaged<T>()`.

- Миры изолированы: `EntityQuery` и `EntityManager` работают только с сущностями своего мира.
- `World.Dispose()` завершает все jobs мира, уничтожает системы в порядке, обратном созданию (вызывая `OnDestroy`), но **не убирает мир из PlayerLoop**. Уберите его сами до `Dispose`.
- `World.Update()` вручную обновляет `InitializationSystemGroup` → `SimulationSystemGroup` → `PresentationSystemGroup` (только существующие). Это нужно для тестов, сервера и тик-драйва.
- `WorldFlags` описывают роль мира: `Game`, `Editor`, `Simulation` (значение по умолчанию в конструкторе), `GameServer`, `GameClient` и т.д.

### 2.2 Автоматический бутстрап: что он делает

Без defines `AutomaticWorldBootstrap` (сборка `Unity.Entities.Hybrid`) на `RuntimeInitializeLoadType.BeforeSceneLoad` вызывает `DefaultWorldInitialization.Initialize("Default World")`. Он делает следующее:

1. Регистрирует скрытый proxy-GameObject. При выходе из Play Mode и выгрузке домена он убирает **все** миры из PlayerLoop и вызывает `World.DisposeAllWorlds()`.
2. Инициализирует Entities Profiler (модули Structural Changes / Memory).
3. Ищет `ICustomBootstrap`: берётся самый производный тип, а типы или сборки с `[DisableBootstrapOverrides]` пропускаются. Если `Initialize(name)` вернул `true`, дефолтный мир не создаётся. При этом бутстрап **обязан** присвоить `World.DefaultGameObjectInjectionWorld`, иначе сработает assert. Экземпляр `ICustomBootstrap` создаётся через `Activator` до загрузки сцены, поэтому DI туда не дотягивается.
4. Иначе создаёт `World(name, WorldFlags.Game)`, записывает его в `DefaultGameObjectInjectionWorld`, добавляет все системы из `GetAllSystemTypeIndices(WorldSystemFilterFlags.Default)` и встраивает мир в PlayerLoop.

### 2.3 Отключение автосоздания

| Механизм | Эффект |
| --- | --- |
| `UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_RUNTIME_WORLD` | Нет рантайм-мира. Editor World в Edit Mode остаётся, но в нём живут только системы с `[WorldSystemFilter(WorldSystemFilterFlags.Editor)]` |
| `UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_EDITOR_WORLD` | Нет Editor World |
| `UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP` | Отключает оба |
| `[DisableAutoCreation]` на системе | Система не попадает в `GetAllSystems*`. На `CreateSystem` и `AddSystemsToRootLevelSystemGroups` с явным списком атрибут **не влияет** |
| `[assembly: DisableAutoCreation]` | То же для **всех** систем сборки (проверено в `Types/TypeManagerSystems.cs`) |
| `[DisableBootstrapOverrides]` (тип или сборка) | `ICustomBootstrap` не выбирается автоматически |

Как это применяется в проекте, определяет `Docs/Tech/Architecture/README.md` §4.4 (он приоритетнее справочника): define `..._RUNTIME_WORLD` отключает только мир по умолчанию; системы симуляции **не** помечаются `[DisableAutoCreation]` и находятся автоматически через `GetAllSystems` + `AddSystemsToRootLevelSystemGroups` по `[UpdateInGroup]` (без центрального списка); `[DisableAutoCreation]` ставится только на мостовые `SystemBase`, которые создаёт VContainer и хост добавляет через `World.AddSystemManaged`.

### 2.4 Создание мира вручную

```csharp
using System;
using System.Collections.Generic;
using Unity.Entities;

// Владелец мира. Создаётся и уничтожается композиционным корнем (VContainer), не статикой.
public sealed class SimulationWorldHost : IDisposable
{
    public World World { get; }

    public SimulationWorldHost(string name, IReadOnlyList<Type> systemTypes)
    {
        World = new World(name, WorldFlags.Game);
        // Создаёт Initialization/Simulation/PresentationSystemGroup и системы из списка
        // (с учётом [CreateAfter]/[CreateBefore]), раскладывает по [UpdateInGroup], сортирует группы.
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(World, systemTypes);
        // Не вызывайте, если мир тикается вручную через World.Update() (сервер, тесты).
        ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(World);
    }

    public void Dispose()
    {
        if (!World.IsCreated) return;
        ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(World); // Dispose этого не делает
        World.Dispose();
    }
}
```

Что обязательно положить в список: в нём оказывается **только** то, что перечислено.

- `typeof(UpdateWorldTimeSystem)`. Без неё `SystemAPI.Time` не двигается, и `FixedStepSimulationSystemGroup` не тикает. Альтернатива — `World.SetTime(...)` вручную (сервер, тесты).
- Нужные ECB-системы: `BeginSimulationEntityCommandBufferSystem`, `EndSimulationEntityCommandBufferSystem` и т.д. Без них их `Singleton` не существует.
- `FixedStepSimulationSystemGroup` и `Begin/EndFixedStepSimulationEntityCommandBufferSystem`, если они используются.
- **Свои группы.** Если группа из `[UpdateInGroup]` не создана, система не попадёт ни в одну группу и не будет обновляться (в лог пишется warning).
- Transform-системы пакета (`TransformSystemGroup`, `ParentSystem`, `LocalToWorldSystem`) добавляйте, только если симуляция использует `LocalTransform`/`Parent`.

Чего нет при ручном бутстрапе (проверено по исходникам `DefaultWorldInitialization.cs`):

- Нет автоматического `Dispose` миров при выходе из Play Mode, освобождайте сами.
- `EntitiesProfiler.Initialize()` вызывается только внутри `DefaultWorldInitialization.Initialize`. Поэтому модули профайлера Entities, скорее всего, будут пустыми (в редакторе не проверено).
- Исключения из `OnCreate` при создании списком логируются (`Debug.LogException`), а не пробрасываются. После бутстрапа проверяйте консоль или тестами.

### 2.5 WorldSystemFilter

`[WorldSystemFilter(flags)]` и `DefaultWorldInitialization.GetAllSystemTypeIndices(flags)` фильтруют системы по фиксированному enum `WorldSystemFilterFlags` (`Default`, `Editor`, `LocalSimulation`, `ServerSimulation`, `ClientSimulation`, `Presentation`, …). Своих флагов добавить нельзя. Для наших миров надёжнее **явные списки типов**. Фильтры пригодятся, только если понадобится разделение client/server.

### 2.6 Почему нельзя `DefaultGameObjectInjectionWorld` в игровом коде

- Это статическое свойство с одним миром. Оно ломает сценарии «несколько миров», «серверная симуляция» и изолированные тесты.
- Мануал прямо говорит, что его читают редакторные инструменты (Entity Inspector, SubScene Inspector). Поэтому **присвоить** его в dev-коде бутстрапа допустимо, **читать** из игрового кода — нет. Нужен ли он для отображения наших миров в Hierarchy — не проверено.
- Мир передаётся явно: через DI в bridge-системы и сервисы, через `ref SystemState` в системы.
- В VContainer 1.19 (`PackageCache/jp.hadashikick.vcontainer@…`) ECS-интеграция включается автоматически (`VCONTAINER_ECS_INTEGRATION`). `UseDefaultWorld`, `RegisterSystemFromDefaultWorld` и `Register*IntoDefaultWorld` читают `DefaultGameObjectInjectionWorld`, их **не используем**. `RegisterNewWorld` создаёт `new World(name)` без удаления из PlayerLoop при `Dispose` (владение миром при уничтожении scope не проверено). Предпочтителен собственный host (§2.4).

Источники: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/concepts-worlds.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-icustombootstrap.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-update-order.html>

---

## 3. Сущности и архетипы

### 3.1 Entity

`Entity` — это `struct` из `int Index` и `int Version`. Также есть `Entity.Null` и Burst-совместимый `ToFixedString()`.

- С Entities 1.2 индексы выделяются из **общего на процесс хранилища** (`EntityComponentStore.s_entityStore`). Значения `Entity` из разных миров не пересекаются и не соответствуют друг другу. `EntityManager.Exists` проверяет принадлежность миру.
- Значения `Entity` **не детерминированы**: зависят от других миров и (для parallel ECB) от потока записи. Не используйте `Entity.Index` как стабильный ID в сейвах, сети, сортировке для детерминизма. Нужен свой ID-компонент.
- `Version` может переполниться, сравнивать версии на «новее/старше» нельзя.

### 3.2 Создание в рантайме

```csharp
// Архетип создаём один раз (например, в OnCreate или в фабрике), дальше — батчами.
EntityArchetype unitArchetype = em.CreateArchetype(
    typeof(UnitId), typeof(Health), typeof(Position2D), typeof(Velocity2D));

// Батч-создание дешевле поштучного: одна структурная операция.
using NativeArray<Entity> units = em.CreateEntity(unitArchetype, count, Allocator.Temp);
```

- Chunk = 16 KiB, максимум 128 сущностей в чанке (`TypeManager.MaximumChunkCapacity`). Большие компоненты уменьшают ёмкость.
- Создание, уничтожение, add/remove компонента и смена shared-значения — структурные изменения (§7).

### 3.3 Сущности-префабы (без baking)

```csharp
// Префаб — обычная сущность с тегом Prefab: запросы её не видят, системы не обрабатывают.
var prefab = em.CreateEntity(em.CreateArchetype(
    typeof(Prefab), typeof(UnitId), typeof(Health), typeof(Velocity2D)));
em.SetComponentData(prefab, new Health { Value = cfg.MaxHp }); // значения из JSON-конфига

// Инстансы получают все компоненты префаба, кроме Prefab.
using var spawned = em.Instantiate(prefab, spawnCount, Allocator.Temp);
```

- Запросы по умолчанию исключают сущности с `Prefab` и `Disabled`. Чтобы видеть их, нужны `EntityQueryOptions.IncludePrefab` / `IncludeDisabledEntities`.
- `EntityCommandBuffer.CreateEntity(archetype)` с `Prefab` в архетипе **бросает исключение при playback** (из документации метода в `EntityCommandBuffer.cs`). Префабы создавайте через `EntityManager`.
- Enableable-компоненты инстанса копируют enabled-состояние префаба. Cleanup-компоненты не копируются.

### 3.4 LinkedEntityGroup

`LinkedEntityGroup` — буфер с особой семантикой:

- `EntityManager.Instantiate` клонирует всю группу (с ремапом ссылок внутри неё), `DestroyEntity` уничтожает всю группу, `SetEnabled` ставит/снимает `Disabled` на всей группе.
- Первый элемент **всегда** сама корневая сущность.
- Группы не рекурсивны, вложенных групп избегать. Уничтоженных участников нужно удалять из буфера вручную.
- `DestroyEntity(EntityQuery)`: либо вся группа матчит запрос, либо никто.
- С 1.4: `Instantiate` сущности **без** LEG ведёт себя как с LEG из неё самой. Ссылки на себя в компонентах инстанса указывают на инстанс.
- Ёмкость: `[InternalBufferCapacity(0)]`. Уменьшена с 1 до 0 в **Entities 1.3** (не в 6.x), так что буфер всегда лежит вне чанка.
- Иерархия трансформов (`Parent`/`Child`) — отдельное понятие и в LEG не попадает автоматически.

### 3.5 Имена сущностей

`EntityManager.SetName(e, FixedString64Bytes)` и `GetName` работают только в редакторе (`DOTS_DISABLE_DEBUG_NAMES` отключает их). Строка длиннее 61 **байта UTF-8** (кириллица — 2 байта на символ, т.е. ~30 русских букв) **бросает исключение** при неявном приведении `string` → `FixedString64Bytes`; обрезайте через `CopyFromTruncated`.

### 3.6 Entity ≠ EntityId

`UnityEngine.EntityId` — 64-битный идентификатор `UnityEngine.Object`, заменивший `InstanceID` (в 6.5 устаревшие `InstanceID`-API дают ошибку компиляции). Unity называет его идентификатором, общим для GameObject и сущностей. В `Entity.cs` есть неявное преобразование `Entity → EntityId` (побитовое), обратного нет. ECS-API (`EntityManager`, ECB, lookups) по-прежнему принимают `Entity`. Для нас это важно в OOP-слое: не храните `EntityId` в `int`, иначе значение обрежется.

Источники: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/concepts-entities.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/linked-entity-group.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/upgrade-guide.html#migrate-instanceid-code-to-entityid>

---

## 4. Компоненты

### 4.1 Unmanaged IComponentData и теги

Допустимые поля: blittable-типы, `bool`, `char`, `BlobAssetReference<T>`, `FixedString*Bytes`, `FixedList*Bytes<T>`, fixed-массивы (unsafe), вложенные struct с теми же ограничениями. Пустая struct — **тег** (0 байт в чанке). Методы в компонентах не приветствуются: логика живёт в системах. Строки: `FixedString32Bytes`/`64`/`128`/`512`/`4096`; короткие списки: `FixedList32Bytes<T>` … `FixedList4096Bytes<T>`.

### 4.2 Динамические буферы

```csharp
[InternalBufferCapacity(8)] // до 8 элементов лежат прямо в чанке
public struct PathNode : IBufferElementData { public int2 Cell; }
```

- Ёмкость по умолчанию — сколько элементов влезает в 128 байт (`TypeManager.DefaultBufferCapacityNumerator`).
- Если длина превысила ёмкость, данные переезжают в кучу **навсегда**: внутреннее место в чанке теряется, каждое чтение даёт промах кэша.
- Для буферов сильно переменной длины ставьте `InternalBufferCapacity(0)`.
- `EnsureCapacity(n)` перед серией `Add`, `TrimExcess()` чтобы сжать.
- API: `em.AddBuffer<T>(e)`, `em.GetBuffer<T>(e)`, `SystemAPI.GetBuffer<T>(e)`, `BufferLookup<T>`, `DynamicBuffer<T>` в `SystemAPI.Query`/`IJobEntity`.
- Любое структурное изменение **инвалидирует** полученный `DynamicBuffer<T>`. После него буфер нужно получить заново.

### 4.3 Enableable-компоненты

`struct X : IComponentData, IEnableableComponent` (или `IBufferElementData`) можно включать и выключать на сущности **без структурного изменения**, в том числе из jobs.

- Выключенный компонент для запросов равен отсутствующему (`WithAll` не матчит, `WithNone` матчит). Но `HasComponent` = true, значение читается и пишется.
- Новый компонент изначально включён.
- API: `EnabledRefRW<T>`/`EnabledRefRO<T>` в итерации (самый быстрый путь), `ComponentLookup<T>.SetComponentEnabled`, `EnabledMask` в `IJobChunk`, `em.IsComponentEnabled<T>` / `SetComponentEnabled<T>`, `ecb.SetComponentEnabled<T>`.
- Подходят для частых смен состояния и вместо россыпи тегов (меньше архетипов). Цена — место в чанке и маски при итерации.

### 4.4 Shared components (только unmanaged)

`struct X : ISharedComponentData` без managed-полей. Для своей семантики равенства реализуйте `IEquatable<X>` + `GetHashCode()` (можно `[BurstCompile]`). Managed shared components deprecated: struct с managed-полями даёт warning `EA0017`.

- Смена значения — структурное изменение: сущность переезжает в чанк с новым значением.
- Все сущности чанка делят одно значение. Много уникальных значений означает фрагментацию (500 уникальных значений = 500 почти пустых чанков).
- Используйте как ключ разбиения, например «этаж/уровень», а не как часто меняющиеся данные.
- API: `em.AddSharedComponent`, `em.SetSharedComponent`, `em.GetSharedComponent`, фильтр `WithSharedComponentFilter<T>(value)` / `EntityQuery.SetSharedComponentFilter` (не больше двух shared-типов в фильтре). У `SystemAPI` нет per-entity get/set для shared.

### 4.5 Cleanup-компоненты

`ICleanupComponentData`, `ICleanupBufferElementData`, `ICleanupSharedComponentData`.

- `DestroyEntity` на сущности с cleanup-компонентом удаляет только обычные компоненты. Сущность живёт, пока не сняты все cleanup-компоненты. Снятие последнего уничтожает её.
- Не копируются при `Instantiate` и при копировании между мирами.
- Это основной паттерн моста к OOP-слою:
  - «новые» = `WithAll<ViewId>().WithNone<ViewCleanup>()` → создать view, добавить `ViewCleanup`;
  - «умершие» = `WithAll<ViewCleanup>().WithNone<ViewId>()` → убрать view, снять `ViewCleanup`.
  - В `OnDestroy` bridge-системы подчистите все оставшиеся.

### 4.6 Singletons

Singleton — компонент, который в мире есть ровно у одной сущности. Создание: `em.CreateSingleton<T>(value, name)`.

- Чтение: `SystemAPI.GetSingleton<T>()` возвращает копию, регистрирует систему как читателя и не меняет версию.
- Запись: `SystemAPI.GetSingletonRW<T>()` возвращает `RefRW<T>`, регистрирует систему как писателя и **всегда** поднимает change version. Также есть `SetSingleton`, `TryGetSingleton*`, `HasSingleton`, `GetSingletonBuffer`.
- Singleton-API **не завершают** jobs. При конфликте с работающим job — ошибка safety. Лечится порядком систем или `em.CompleteDependencyBeforeRO/RW<T>()`.
- Singleton-API исключают `Prefab`/`Disabled` сущности и бросают исключение для enableable-типов и при >1 совпадении.
- Типичное применение: конфиг симуляции (`SimulationConfig` с `BlobAssetReference`), RNG-состояние, счётчик тиков.

### 4.7 Chunk components

Одно значение на чанк (`em.AddChunkComponentData<T>`, `GetChunkComponentData`, `SetChunkComponentData`). Запись не является структурным изменением. Всегда unmanaged. Применение — кэш агрегатов по чанку (bounds) для раннего отсечения. В проекте пока не нужны.

### 4.8 Blob assets (рантайм)

Blob — иммутабельные unmanaged данные одним блоком с относительными смещениями. Безопасны для параллельного чтения. Подходят для статических конфигов (таблицы урона, карта, навигация).

```csharp
public struct EnemyTable { public BlobArray<EnemyDef> Defs; }
public struct EnemyDef { public int Id; public float Hp; public BlobString Name; }

public static BlobAssetReference<EnemyTable> Build(IReadOnlyList<EnemyJson> src)
{
    using var builder = new BlobBuilder(Allocator.Temp);
    ref EnemyTable root = ref builder.ConstructRoot<EnemyTable>();
    BlobBuilderArray<EnemyDef> arr = builder.Allocate(ref root.Defs, src.Count);
    for (int i = 0; i < src.Count; i++)
    {
        arr[i].Id = src[i].Id;
        arr[i].Hp = src[i].Hp;
        builder.AllocateString(ref arr[i].Name, src[i].Name);
    }
    // Копирует данные в итоговый блок; builder после этого не нужен.
    return builder.CreateBlobAssetReference<EnemyTable>(Allocator.Persistent);
}
```

- Доступ **только по ref**: `ref EnemyTable t = ref blobRef.Value;`. Копия `BlobArray`/`BlobString`/`BlobPtr` по значению ломает смещения. Не передавайте `BlobAssetReference<T>` параметром с `in`, если нужен `.Value`.
- До `CreateBlobAssetReference` у `BlobArray` `Length == 0`: читайте длину из `BlobBuilderArray`.
- Есть `BlobArray<T>.AsSpan()` и `BlobString.AsSpan()` (`ReadOnlySpan`); в 1.4.4 их не было.
- `UnityObjectRef<T>` внутри blob не поддерживается.

**Владение и Dispose.** У рантайм-блобов нет refcount'а: кто вызвал `CreateBlobAssetReference`, тот и вызывает `Dispose()`. `Dispose` обнуляет указатель только в той копии, на которой вызван. Копии в компонентах об этом не узнают (`IsCreated` у них остаётся `true`), и обращение к ним — use-after-free. Правило проекта:

- Владелец — система или сервис, построившие блоб.
- Освобождать после того, как ни одна сущность и ни один job его не используют. Надёжнее всего — в `OnDestroy` владеющей системы: `World.Dispose()` сначала завершает все jobs, затем вызывает `OnDestroy` в обратном порядке создания.
- При горячей замене конфига: сначала переписать компоненты на новый блоб, потом освободить старый.

Источники: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-type.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-buffer-set-capacity.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-enableable-use.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-shared-optimize.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-cleanup-create.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-singleton.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/blob-assets-create.html>

---

## 5. Системы

### 5.1 ISystem — вся симуляция

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MoveSystem : ISystem // partial обязателен: SystemAPI — source generation
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationConfig>(); // не обновляться, пока нет конфига
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Schedule без аргументов сам берёт и записывает state.Dependency.
        new MoveJob { Dt = SystemAPI.Time.DeltaTime }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct MoveJob : IJobEntity
{
    public float Dt;
    void Execute(ref Position2D pos, in Velocity2D vel) => pos.Value += vel.Value * Dt;
}
```

- `OnCreate`/`OnUpdate`/`OnDestroy` имеют пустые default-реализации в интерфейсе. `[BurstCompile]` ставится на методы. `IJobEntity` по умолчанию **не** Burst-компилируется, нужен `[BurstCompile]` на struct.
- `ISystemStartStop` добавляет `OnStartRunning`/`OnStopRunning`: вызываются при смене «работает/не работает» (Enabled, RequireForUpdate).
- В ISystem нельзя хранить managed-поля. Состояние хранится в полях struct (unmanaged, включая NativeContainer'ы, которые освобождаются в `OnDestroy`) или в компонентах/синглтонах.

### 5.2 SystemBase — только мосты к OOP

`SystemBase` (managed class, без Burst в `OnUpdate`) допускается **только** для небольшого числа bridge-систем, которые говорят с OOP-сервисами:

- Можно: читать ECS-данные (`SystemAPI.Query`, lookups, синглтоны) и передавать их в сервисы (presentation, audio, UI). Можно также переносить внешние события (ввод, команды UI, результат загрузки) в ECS: создать сущности-команды через ECB или записать синглтон.
- Нельзя: игровые правила, расчёты симуляции, тяжёлые циклы. Всё это делают ISystem + Burst jobs.
- Сервисы передаются через метод после создания мира, а не через статику:

```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class ViewSyncBridgeSystem : SystemBase
{
    IViewService _views; // OOP-сервис
    public void Construct(IViewService views) => _views = views;

    protected override void OnUpdate()
    {
        foreach (var (pos, id) in SystemAPI.Query<RefRO<Position2D>, RefRO<ViewId>>())
            _views.SetPosition(id.ValueRO.Value, pos.ValueRO.Value);
    }
}
// В host'е после создания мира:
// world.GetExistingSystemManaged<ViewSyncBridgeSystem>().Construct(views);
```

`SystemAPI.Query` в главном потоке **завершает** зависимые jobs (sync). Размещайте мосты там, где это дёшево, обычно в `PresentationSystemGroup`.

### 5.3 SystemState

`ref SystemState` даёт: `EntityManager`, `World`, `WorldUnmanaged`, `WorldUpdateAllocator` (память на кадр), `Dependency` + `CompleteDependency()`, `Enabled`, `LastSystemVersion`, `GlobalSystemVersion`, `GetEntityQuery(...)`, `GetComponentLookup<T>(ro)`, `GetBufferLookup<T>(ro)`, `GetComponentTypeHandle<T>(ro)`, `GetEntityTypeHandle()`, `RequireForUpdate<T>()`, `RequireForUpdate(query)`, `RequireAnyForUpdate(...)`.

Запросы и handles берите через `SystemState`/`SystemAPI`, а не напрямую из `EntityManager`. Только так система регистрирует свои типы, и авто-зависимости между системами работают.

### 5.4 SystemAPI

Работает только в нестатических методах `ISystem` с `ref SystemState` и в `SystemBase`. В статических хелперах не работает: это source-gen заглушки.

| Задача | API |
| --- | --- |
| Итерация | `Query<...>()`, `QueryBuilder()` |
| Компоненты | `GetComponent`, `SetComponent`, `GetComponentRO/RW`, `HasComponent`, `TryGetComponent`, `IsComponentEnabled`, `SetComponentEnabled`, `GetComponentLookup` |
| Буферы | `GetBuffer`, `HasBuffer`, `IsBufferEnabled`, `SetBufferEnabled`, `GetBufferLookup` |
| Синглтоны | `GetSingleton`, `GetSingletonRW`, `SetSingleton`, `TryGetSingleton`, `TryGetSingletonRW`, `GetSingletonEntity`, `TryGetSingletonEntity`, `GetSingletonBuffer`, `TryGetSingletonBuffer`, `HasSingleton` |
| Сущности/handles | `Exists`, `GetEntityStorageInfoLookup`, `GetEntityTypeHandle`, `GetComponentTypeHandle`, `GetBufferTypeHandle`, `GetSharedComponentTypeHandle` |
| Время | `SystemAPI.Time` (`ref readonly TimeData`) |

`GetComponentLookup`/`GetBufferLookup`/handles кэшируются в `OnCreate` и обновляются перед использованием без sync. `GetComponent`/`SetComponent`/`GetBuffer` завершают jobs-писателей (sync).

### 5.5 Группы и порядок

Дефолтная иерархия. Каждая группа существует, только если её создали.

```text
InitializationSystemGroup      (конец фазы Initialization PlayerLoop)
  BeginInitializationEntityCommandBufferSystem   [OrderFirst]
  UpdateWorldTimeSystem
  EndInitializationEntityCommandBufferSystem     [OrderLast]
SimulationSystemGroup          (конец фазы Update)
  BeginSimulationEntityCommandBufferSystem       [OrderFirst]
  FixedStepSimulationSystemGroup                 [OrderFirst, после BeginSimulation ECB]
    Begin/EndFixedStepSimulationEntityCommandBufferSystem
  VariableRateSimulationSystemGroup              [OrderFirst, после BeginSimulation ECB]
  ... системы без UpdateInGroup ...
  LateSimulationSystemGroup                      [OrderLast, до EndSimulation ECB]
  EndSimulationEntityCommandBufferSystem         [OrderLast]
PresentationSystemGroup        (конец фазы PreLateUpdate)
  BeginPresentationEntityCommandBufferSystem     [OrderFirst]
```

- Своя группа — это `public partial class GameplaySystemGroup : ComponentSystemGroup {}` плюс `[UpdateInGroup]`.
- `[UpdateInGroup(typeof(G), OrderFirst = true / OrderLast = true)]`: OrderFirst/OrderLast приоритетнее, чем `[UpdateBefore]`/`[UpdateAfter]`.
- `UpdateBefore`/`UpdateAfter` действуют только между **детьми одной группы**. На группе они ограничивают всех её членов.
- Цикл ограничений бросает исключение при сортировке.
- `[CreateAfter(typeof(X))]`/`[CreateBefore]` задают порядок **создания** (и `OnCreate`). Уничтожение идёт в обратном порядке. Нужны, если `OnCreate` читает синглтон, созданный другой системой.
- Без `[UpdateInGroup]` система попадает в `SimulationSystemGroup`.
- `[RequireMatchingQueriesForUpdate]` пропускает `OnUpdate`, если все запросы системы пусты.

### 5.6 Время и fixed step

- `UpdateWorldTimeSystem` (SystemBase в `Unity.Entities.Hybrid`) каждый кадр делает `World.SetTime(elapsed + dt, dt)`, где `dt = min(UnityEngine.Time.deltaTime, World.MaximumDeltaTime)`. По умолчанию `MaximumDeltaTime = 1/3` с.
- `World.SetTime(new TimeData(elapsed, dt))`, `PushTime`/`PopTime` — ручное управление (сервер, тесты, реплей). Push без Pop ловится assert'ом в `World.Update()`.
- `FixedStepSimulationSystemGroup` по умолчанию: шаг 1/60, `RateUtils.FixedRateCatchUpManager`. Группа выполняется 0..N раз за кадр, «догоняя» время. Первый апдейт — при t=0. На время апдейта она подставляет `ElapsedTime`/`DeltaTime` = фиксированный шаг (PushTime). Системы внутри должны быть дешёвыми, иначе начинается спираль отставания.

```csharp
var fixedGroup = world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
fixedGroup.Timestep = 1f / 30f;   // clamp в [0.0001, 10]
// Альтернатива без «догоняния»: ровно один апдейт на кадр с фиксированным dt
// fixedGroup.RateManager = new RateUtils.FixedRateSimpleManager(1f / 30f);
```

- `VariableRateSimulationSystemGroup`: по умолчанию ~15 Гц (`VariableRateManager`, 66 мс).
- Детерминизм симуляции: фиксированный шаг, `Unity.Mathematics.Random` из состояния в компоненте и детерминированный порядок ECB (§7.4). Время кадра (`UnityEngine.Time`) в правила не пускаем.

Источники: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-isystem.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-comparison.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-systemapi.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-update-order.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-time.html>

---

## 6. Запросы и доступ к данным

### 6.1 SystemAPI.Query

```csharp
foreach (var (hp, regen, e) in SystemAPI.Query<RefRW<Health>, RefRO<Regen>>()
             .WithAll<Alive>()
             .WithNone<Stunned>()
             .WithEntityAccess())                 // Entity — последний элемент кортежа
{
    hp.ValueRW.Value = math.min(hp.ValueRO.Value + regen.ValueRO.PerTick, hp.ValueRO.Max);
}
```

- Типы-параметры (до 7): `RefRO<T>`, `RefRW<T>`, `T` (копия, read-only), `DynamicBuffer<T>`, `EnabledRefRO<T>`, `EnabledRefRW<T>`, unmanaged shared-компонент.
- Цепочки (до 3 типов в вызове): `WithAll`, `WithAny`, `WithNone`, `WithDisabled`, `WithPresent`, `WithAbsent`, `WithChangeFilter`, `WithSharedComponentFilter`, `WithOptions`, `WithEntityAccess`.
- `foreach` выполняется в главном потоке и завершает зависимости. Тяжёлую работу выносите в `IJobEntity`.

### 6.2 EntityQuery и builder

```csharp
// В OnCreate: кэшируем запрос. SystemAPI.QueryBuilder() делает то же и кэширует сам.
_query = new EntityQueryBuilder(Allocator.Temp)
    .WithAllRW<Health>()
    .WithAll<Alive>()
    .WithNone<Dead>()
    .Build(ref state);
```

| Метод | Условие матча |
| --- | --- |
| `WithAll<T>` / `WithAllRW<T>` | компонент есть и включён |
| `WithAny<T>` | хотя бы один есть и включён |
| `WithNone<T>` | нет, **или есть, но выключен** |
| `WithDisabled<T>` / `WithDisabledRW<T>` | есть и выключен |
| `WithPresent<T>` / `WithPresentRW<T>` | есть, состояние неважно |
| `WithAbsent<T>` | отсутствует вовсе (выключенный не считается) |
| `WithOptions(EntityQueryOptions)` | `IncludePrefab`, `IncludeDisabledEntities`, `IgnoreComponentEnabledState`, `FilterWriteGroup`, … |
| `AddAdditionalQuery()` | объединение (OR) нескольких описаний |

- `Build` есть в перегрузках `(ref SystemState)`, `(SystemBase)` и `(EntityManager)`. Последняя для тестов и хостов, в системах используйте первые две.
- Методы запроса: `CalculateEntityCount`, `IsEmpty`, `ToEntityArray`, `ToComponentDataArray<T>`, `CopyFromComponentDataArray<T>`, `ToArchetypeChunkArray`, `ToEntityListAsync`, `GetSingleton*`. Варианты `…IgnoreFilter` / `…WithoutFiltering` не учитывают фильтры и enabled-состояние и не синхронизируются.
- Синхронные методы с учётом enableable ждут jobs-писателей этих компонентов.

### 6.3 IJobEntity и IJobChunk

- `IJobEntity`: `Execute(...)` принимает `ref`/`in` компоненты, `RefRW/RO`, `EnabledRefRW/RO`, `DynamicBuffer<T>`, `Entity`, а также индексы `[ChunkIndexInQuery] int`, `[EntityIndexInQuery] int`, `[EntityIndexInChunk] int`. Фильтры задаются атрибутами на struct: `[WithAll]`, `[WithNone]`, `[WithAny]`, `[WithDisabled]`, `[WithPresent]`, `[WithAbsent]`, `[WithChangeFilter]`, `[WithOptions]`. Запуск: `Schedule()`, `ScheduleParallel()`, `Run()` (+ перегрузки с `EntityQuery` и `JobHandle`).
- `IJobChunk` нужен для полного контроля: `void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`. По сущностям ходите через `new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count)`, данные берите через `chunk.GetNativeArray(ref handle)`, `chunk.GetEnabledMask(ref handle)`, `chunk.Has<T>()`.

### 6.4 Change filters и версии

- Фильтр изменений работает **на уровне чанка** и срабатывает при любом доступе на запись (RW handle, `RefRW`, `GetSingletonRW`), даже если значение не менялось. Поэтому всё, что только читается, объявляйте read-only.
- Как задать: `.WithChangeFilter<T>()`, `[WithChangeFilter(typeof(T))]`, `query.SetChangedVersionFilter(typeof(T))`. В `IJobChunk` — `chunk.DidChange(ref handle, LastSystemVersion)`.
- `query.AddOrderVersionFilter()` — «в чанке были структурные изменения».
- Версии — `uint` и могут переполняться. Сравнивать через `ChangeVersionUtility.DidChange(a, b)`, не `>`.
- Фильтры остаются до `ResetFilter()`.

### 6.5 ComponentLookup / BufferLookup

```csharp
[BurstCompile]
public partial struct AttackJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Position2D> PosLookup;
    void Execute(ref AttackState atk, in Target target)
    {
        if (PosLookup.TryGetComponent(target.Value, out var targetPos)) { /* ... */ }
    }
}
// В OnUpdate:
// new AttackJob { PosLookup = SystemAPI.GetComponentLookup<Position2D>(true) }.ScheduleParallel();
```

- `ComponentLookup<T>`: индексатор, `HasComponent`, `TryGetComponent`, `EntityExists`, `GetRefRO`/`GetRefRW`, `TryGetRefRO`/`TryGetRefRW` (1.4+; `TryGetRefRW` корректно поднимает change version), `IsComponentEnabled`/`SetComponentEnabled`, `GetEnabledRefRW<T>`, `DidChange`, `Update(ref state)`. `GetRefRWOptional`/`GetRefROOptional` устарели, используйте `TryGetRef*`.
- `BufferLookup<T>`: индексатор, `HasBuffer`, `TryGetBuffer`, `EntityExists`, `IsBufferEnabled`/`SetBufferEnabled`, `Update`.
- Если lookup хранится в поле системы, получите его в `OnCreate` (`state.GetComponentLookup<T>(true)`) и вызывайте `.Update(ref state)` в каждом `OnUpdate`. `SystemAPI.GetComponentLookup` делает это сам.
- В parallel job запись через lookup запрещена safety-проверками. `[NativeDisableParallelForRestriction]` допустим только при гарантированном отсутствии гонок.
- Random access — это промахи кэша. Используйте его только там, где без него нельзя.
- `EntityStorageInfoLookup.Exists(e)` проверяет существование сущности в job.

### 6.6 Зависимости jobs

- Перед `OnUpdate` в `state.Dependency` лежат хэндлы jobs других систем, которые трогают те же типы компонентов.
- Все jobs системы должны зависеть от входного `Dependency`, а итоговый хэндл записывается обратно. `Schedule()`/`ScheduleParallel()` без аргументов делают это автоматически.
- NativeContainer'ы **не отслеживаются**. Контейнер, общий для нескольких систем, кладите в компонент или синглтон, тогда зависимость пойдёт через тип компонента.

Источники: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-systemapi-query.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-entityquery-create.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-entityquery-filters.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/iterating-data-ijobentity.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-looking-up-data.html>

---

## 7. Структурные изменения

### 7.1 Что это и чем грозит

Структурные изменения — создание/уничтожение сущности, add/remove компонента, смена значения shared-компонента. Они возможны **только в главном потоке** и создают **sync point**: ожидание завершения всех запланированных jobs, которые их касаются. Они также инвалидируют ранее полученные `DynamicBuffer`, `RefRW/RefRO` и lookups. Запись значения обычного компонента и enable/disable структурными изменениями не являются.

Стратегия проекта: копить структурные изменения в ECB и проигрывать в нескольких точках кадра; системы с прямыми структурными изменениями ставить подряд; частые переключения состояния делать enableable-компонентами.

### 7.2 EntityCommandBuffer (семантика 6.6)

```csharp
var ecb = new EntityCommandBuffer(Allocator.TempJob);
Entity e = ecb.CreateEntity(archetype);          // РЕАЛЬНАЯ сущность уже сейчас (6.6)
ecb.SetComponent(e, new Health { Value = 10 });
ecb.AddComponent(other, new Target { Value = e }); // ссылку можно класть в данные
// ... job.Schedule(); state.Dependency.Complete();
ecb.Playback(state.EntityManager);                // один раз; повтор → исключение
ecb.Dispose();
```

- **6.6: плейсхолдеров больше нет.** `CreateEntity`/`Instantiate` (и в `ParallelWriter`) возвращают валидный `Entity` во время записи. Его можно хранить, передавать в другие буферы и класть в поля компонентов: ремаппинг не нужен. До `Playback` у сущности нет чанка, и `EntityManager`/запросы её не видят. Проверки «`Index < 0` = временная сущность» больше не работают.
- При `ecb.Instantiate(prefab)` с `LinkedEntityGroup` заранее выделяется **только корень**. Дети появляются при playback, их читают из `LinkedEntityGroup` корня после playback.
- **`PlaybackPolicy` obsolete целиком.** Любой ECB проигрывается ровно один раз. Чтобы повторить команды, запишите их в новый ECB.
- Команды по `EntityQuery` (`AddComponent`, `RemoveComponent`, `DestroyEntity`, …) передавайте с `EntityQueryCaptureMode.AtPlayback`: перегрузки без него и режим `AtRecord` obsolete. Если нужна семантика «на момент записи», передайте `NativeArray<Entity>`.
- ECB не объединяет одинаковые команды. Для массовых операций используйте батч-перегрузки: `CreateEntity(archetype, NativeArray<Entity>)`, `Instantiate(e, NativeArray<Entity>)`.
- Методы ECB: `CreateEntity`, `Instantiate`, `DestroyEntity`, `AddComponent`, `SetComponent`, `RemoveComponent`, `AddBuffer`/`SetBuffer`/`AppendToBuffer`, `SetComponentEnabled`, `AddSharedComponent`/`SetSharedComponent`, `SetName`. Чтения нет.
- Safety-проверки ECB работают только в редакторе.

### 7.3 ECB-системы

```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                       .CreateCommandBuffer(state.WorldUnmanaged);
    new DeathJob { Ecb = ecb.AsParallelWriter() }.ScheduleParallel();
    // Playback и Dispose сделает EndSimulationEntityCommandBufferSystem. Сами НЕ вызываем.
}
```

- `GetSingleton<…Singleton>()` регистрирует зависимость. ECB-система при своём апдейте завершит jobs, проиграет буферы в порядке создания и освободит их.
- Дефолтные ECB-системы: `Begin/EndInitialization…`, `Begin/EndFixedStepSimulation…`, `Begin/EndVariableRateSimulation…`, `Begin/EndSimulation…`, `BeginPresentation…`. `EndPresentation` нет: вместо него `BeginInitialization` следующего кадра.
- Каждая ECB-система — отдельная sync point. Используйте минимальный набор, обычно `EndSimulation` + `EndFixedStepSimulation`.

### 7.4 ParallelWriter и детерминизм

```csharp
[BurstCompile]
public partial struct DeathJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    void Execute(Entity e, [ChunkIndexInQuery] int sortKey, in Health hp)
    {
        if (hp.Value <= 0) Ecb.DestroyEntity(sortKey, e); // sortKey — первый аргумент
    }
}
```

- Порядок **записи** из потоков недетерминирован. При playback команды сортируются по sort key, а большие ключи идут позже. Ключ должен не зависеть от планирования: `[ChunkIndexInQuery]` в `IJobEntity`, `unfilteredChunkIndex` в `IJobChunk`.
- **Один ECB на один job.** Два job'а с одинаковыми `ChunkIndexInQuery` в одном буфере перемешают команды.
- Значения `Entity`, выданные `ParallelWriter.CreateEntity/Instantiate`, берутся из per-thread пулов и зависят от потока. Порядок команд детерминирован, **номера сущностей — нет** (§3.1).

Источники: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/concepts-structural-changes.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/performance-sync-points.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-entity-command-buffers.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-entity-command-buffer-playback.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-entity-command-buffer-automatic-playback.html>

---

## 8. Отладка и тесты

### 8.1 Окна редактора (6.6)

- **Стандартное окно Hierarchy** показывает миры ECS как узлы под сценами. Внутри — сущности, вложенные по `Parent`. Все ECS-узлы только для чтения. Выбор сущности показывает её в Inspector.
- **Window › Entities › Hierarchy (Deprecated)** — старое Entities Hierarchy. В окне висит баннер: *«deprecated and will be removed… Entities have now been integrated into the standard Hierarchy window»*. Мануал 6.6 об этом пока молчит, это видно только по исходникам.
- **Window › Entities › Systems** — дерево систем по мирам, время в мс, число сущностей в запросах, связи `UpdateBefore/After`, временное отключение системы. Поиск: `c=`, `sd=`, `ns=`. В меню ⋮ есть «Show All Worlds».
- Окон **Components** и **Archetypes** в `Window › Entities` больше нет: они стали провайдерами поиска `Window › Search › Components` / `Archetypes` / `Node Hierarchy`.
- **Journaling (Deprecated)** — не использовать.
- **Profiler**: модули «Entities Structural Changes» и «Entities Memory». Оговорка про ручной бутстрап — §2.4.

### 8.2 Отладочные колбэки компонентов (новое в 6.6)

```csharp
public struct Health : IComponentData, IDebugOnAdded, IDebugOnRemoved
{
    public float Value;
    public static void OnAdded(Entity entity, in Health c)   { /* breakpoint → call stack */ }
    public static void OnRemoved(Entity entity, in Health c) { }
}
```

- Колбэки есть только в редакторе и development-сборках. В release они исключены, если не задан `UNITY_DOTS_DEBUG`.
- Вызываются при реальном добавлении или удалении компонента, включая `DestroyEntity`, по разу на сущность. Не вызываются при create-with-archetype, enable/disable, записи значения и для `IBufferElementData`.
- Для изменений из ECB call stack показывает playback, а не систему-автора.
- Не использовать для игровой логики.

### 8.3 EditMode-тесты со своим миром

`ECSTestsFixture` лежит во внутренней тестовой сборке пакета `Unity.Entities.Tests` (`autoReferenced: false`, `UNITY_INCLUDE_TESTS`). Зависеть от неё не стоит, но её паттерн стоит повторить:

```csharp
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

public abstract class EcsTestFixture
{
    protected World World;
    protected EntityManager Em;

    [SetUp]
    public virtual void SetUp()
    {
        World = new World("Test World");          // не трогаем DefaultGameObjectInjectionWorld
        Em = World.EntityManager;
        World.SetTime(new TimeData(elapsedTime: 0, deltaTime: 1f / 30f)); // детерминированное время
    }

    [TearDown]
    public virtual void TearDown()
    {
        if (World is { IsCreated: true })
        {
            Em.CompleteAllTrackedJobs();
            World.Dispose();                        // вызовет OnDestroy систем (освобождение блобов)
        }
    }
}

public class MoveSystemTests : EcsTestFixture
{
    [Test]
    public void Moves_By_Velocity()
    {
        var sys = World.CreateSystem<MoveSystem>();
        Em.CreateSingleton(new SimulationConfig());          // для RequireForUpdate
        var e = Em.CreateEntity(typeof(Position2D), typeof(Velocity2D));
        Em.SetComponentData(e, new Velocity2D { Value = new float2(3, 0) });

        sys.Update(World.Unmanaged);                          // один апдейт одной системы
        Em.CompleteAllTrackedJobs();                          // дождаться ScheduleParallel

        Assert.AreEqual(0.1f, Em.GetComponentData<Position2D>(e).Value.x, 1e-5f);
    }
}
```

- `World.CreateSystem<T>()` создаёт систему **вне групп**. Её обновляют через `SystemHandle.Update(world.Unmanaged)`, либо собирают мир как в проде (`AddSystemsToRootLevelSystemGroups`) и вызывают `World.Update()`. Во втором случае не добавляйте `UpdateWorldTimeSystem`: время задаётся `SetTime`.
- Документация `SystemHandle.Update` предупреждает: не обновляйте одну обрабатывающую данные систему из другой, это ломает версии и change filters. В тестах вызывайте из теста.
- Перед assert'ами вызывайте `CompleteAllTrackedJobs()`.
- В редакторе Burst по умолчанию компилирует асинхронно, поэтому первые прогоны могут идти на Mono. Float-результаты Mono и Burst могут расходиться в последних битах: сравнивайте с допуском. Если нужен именно Burst-код, ставьте `[BurstCompile(CompileSynchronously = true)]` (так делает `ECSTestsFixture` 6.6).
- Тестовая asmdef ссылается на `Unity.Entities`, `Unity.Collections`, `Unity.Mathematics`, `Unity.Burst` и на сборку симуляции.

Источники: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/editor-hierarchy-world-node.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/editor-systems-window.html>, <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/entities-component-lifecycle-callbacks.html>, исходники: `PackageCache/Unity.Entities.Editor/Constants.cs`, `Hierarchy/HierarchyWindow.cs`, `Unity.Entities.Tests/ECSTestsFixture.cs`

---

## 9. Шпаргалка: что изменилось с 1.x (только релевантное нам)

| Изменение | Версия | Что делать |
| --- | --- | --- |
| Пакет стал core, версия = версия Unity (6.4.0 ≈ 1.4) | 6.4 | Версию не пинить отдельно, она идёт с редактором |
| `Entities.ForEach`, `Job.WithCode` удалены | 6.5 | `IJobEntity`, `SystemAPI.Query`, `IJob` |
| Aspects (`IAspect`) удалены | 6.5 | Явные запросы + статические хелперы |
| `InstanceID` → `UnityEngine.EntityId` (64 бит) | 6.5 | Не хранить в `int`; `Entity` → `EntityId` конвертируется неявно (§3.6) |
| Journaling deprecated | 6.5 | `IDebugOnAdded/IDebugOnRemoved` (6.6) |
| Managed components и managed shared deprecated | 6.6 | Только unmanaged struct; `FixedString`, буферы, блобы |
| `PlaybackPolicy` obsolete, MultiPlayback уходит | 6.6 | Один ECB на одно проигрывание |
| ECB возвращает реальные `Entity` при записи, плейсхолдеров нет | 6.6 | Не проверять `Index < 0`; ссылки можно хранить сразу |
| Entities Hierarchy → стандартный Hierarchy; окна Components/Archetypes → Window › Search | 6.x | §8.1 |
| `ComponentLookup.TryGetRefRO/TryGetRefRW`, `SystemAPI.TryGetComponent` | 1.4 | Вместо `GetRef*Optional` |
| `[DisableBootstrapOverrides]` | 1.4 | Отключать лишние `ICustomBootstrap` (тесты) |
| `Instantiate` без LEG ремапит самоссылки | 1.4 | Ссылка на себя в инстансе указывает на инстанс |
| Ёмкость `LinkedEntityGroup` 1 → 0 | 1.3 | Буфер всегда вне чанка |
| `EntityQueryCaptureMode.AtRecord` obsolete | 1.3 | `AtPlayback` или массив сущностей |
| Значения `Entity` не совпадают между мирами | 1.2 | Свои стабильные ID; ремап при копировании миров |
| `BlobArray.AsSpan()`, `BlobString.AsSpan()` | 6.x (нет в 1.4.4) | Удобное чтение без копий |

Источник: <https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/upgrade-guide.html> и `CHANGELOG.md` пакета (записи до 6.4; после 6.4 changelog ведётся в «What's new in Unity»).
