# Job System, Burst и Collections — справочник для агентов

> Версии: Unity 6000.6.3f1, Entities 6.6.0, Collections 6.6.0, Burst 2.0.0, Mathematics 1.4.0 (все — core-пакеты, поставляются с редактором).
> Сверено 27.09.2026 с локальной документацией редактора (`/Applications/Unity/Hub/Editor/6000.6.3f1/Documentation`),
> XML-документацией движковых сборок и исходниками в `Library/PackageCache/` (они — эталон).
> Заменяет `Docs/Tech/UnityJobSystem.md` (писался под 2022.3 / Entities 1.x).

Что важно знать про версии:

- `com.unity.burst@2.0.0` — пакет-прокладка (`"type": "shim"`): внутри только `TypeForwarders.cs`, который перенаправляет типы в `UnityEngine.BurstModule`. Компилятор встроен в редактор. Документация Burst переехала в Manual (`Manual/burst/...`); страниц `com.unity.burst@2.0` нет (404).
- `com.unity.mathematics@1.4.0` пуст: код перенесён в модуль `UnityEngine.MathematicsModule`.
- Collections и Entities 6.6 — core-пакеты с исходниками в `PackageCache`; их CHANGELOG после 6.4 не ведётся.

---

## 1. Назначение и правило проекта «по умолчанию — Burst-джоб»

Документ описывает, как писать симуляцию на C# Job System + Burst + Collections в ECS-контуре проекта.
Рендеринг — отдельный ООП-слой, здесь не рассматривается.

**Правило.** Любая логика симуляции — это `ISystem` с `[BurstCompile]`, которая планирует Burst-джобы
(`IJobEntity`/`IJobChunk`, при необходимости `IJobFor`/`IJobParallelFor*`). Отступление допускается
только с причиной, записанной комментарием у системы (`// NOT-BURST: ...` или `// MAIN-THREAD: ...`, ARCH-06).

Разумные причины для исключения (по документации Entities и `ArchitectureDecisions.md`, R-04):

| Ситуация | Что делать |
| --- | --- |
| Структурные изменения (создание/удаление сущностей, add/remove компонентов) | Записывать в `EntityCommandBuffer` в джобе, проигрывать в ECB-системе |
| Main-thread-only API движка, мосты к ООП-сервисам (VContainer внедряет только в `SystemBase`) | `SystemBase`-мост, без Burst, данные в ECS передаются компонентами |
| Крошечный объём данных, с которым не работают другие джобы | `foreach (… in SystemAPI.Query<…>())` прямо в `OnUpdate` — **он всё равно Burst-компилируется**, если `OnUpdate` помечен `[BurstCompile]` |

Burst и джоб — разные вещи: `ISystem.OnUpdate` с `[BurstCompile]` выполняется Burst-кодом на главном потоке.
Burst нужен всегда, а джоб — когда работы больше, чем накладные расходы на его планирование.
Документация прямо говорит: у каждого джоба есть небольшие накладные расходы, и если система только
планирует джоб, а её маркер в профайлере больше маркера самого джоба, мелкие джобы стоит объединить.

Чеклист новой системы:

1. `public partial struct XxxSystem : ISystem`, `[BurstCompile]` на struct и на `OnCreate`/`OnUpdate`/`OnDestroy` (так сделаны системы самого Entities, например `LocalToWorldSystem`).
2. Работа — в `[BurstCompile] partial struct : IJobEntity` → `ScheduleParallel()`.
3. Никаких `Complete()`, `Run()`, `EntityManager`-структурных изменений в `OnUpdate`.
4. Контейнеры на кадр — из `state.WorldUpdateAllocator`.
5. Авторитетные числа — `int`/`long`/fixed-point, порядок — по `StableId` (раздел 6).

Источники:
[ISystem vs SystemBase](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-comparison.html) · [Job scheduling overhead](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/job-overhead.html) · [Introduction to Burst](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/introduction-to-burst.html)

---

## 2. Типы джобов

| Тип | Где объявлен | Execute | Планирование | Когда брать |
| --- | --- | --- | --- | --- |
| `IJob` | движок, `Unity.Jobs` | `Execute()` | `Schedule(dep)`, `Run()` | Одна последовательная задача: свёртка, сортировка-слияние, применение отсортированных результатов |
| `IJobFor` | движок | `Execute(int i)` | `Schedule(n, dep)` — один поток; `ScheduleParallel(n, batch, dep)` | Цикл по массиву, который можно запустить и последовательно, и параллельно |
| `IJobParallelFor` | движок | `Execute(int i)` | `Schedule(n, batch, dep)` | Параллельный цикл по `NativeArray` |
| `IJobParallelForDefer` | Collections | `Execute(int i)` | `Schedule(NativeList<U> list, batch, dep)` | Длина известна только после предыдущего джоба (список заполняется в джобе) |
| `IJobParallelForBatch` | Collections | `Execute(int start, int count)` | `ScheduleParallel(n, indicesPerJob, dep)`, `ScheduleBatch(...)` | Нужен диапазон, а не индекс (векторизация по пачке) |
| `IJobFilter` | Collections | `bool Execute(int i)` | `ScheduleAppend(NativeList<int>, n, dep)`, `ScheduleFilter(NativeList<int>, dep)` (однопоточные) | Отбор индексов в список |
| `IJobEntity` | Entities | `Execute(<компоненты>)` | `Schedule`, `ScheduleParallel`, `Run` (+ `...ByRef`) | **По умолчанию** для перебора сущностей |
| `IJobChunk` | Entities | `Execute(in ArchetypeChunk, int, bool, in v128)` | `Schedule(query, dep)`, `ScheduleParallel(query, dep)`, `Run(query)` | Работа на уровне чанка, несколько проходов, нестандартный порядок |

У всех движковых типов есть варианты `ScheduleByRef`/`RunByRef` для больших структур.
Планировать и завершать джобы можно только с главного потока.

### IJobEntity

- Struct должен быть `partial`: source generator генерирует из него `IJobChunk`.
- Объявлять можно где угодно, но **планировать — только внутри системы** (`ISystem`/`SystemBase`), иначе исключение в рантайме.
- Запрос строится по параметрам `Execute` и атрибутам на struct: `[WithAll]`, `[WithAny]`, `[WithNone]`, `[WithDisabled]`, `[WithAbsent]`, `[WithPresent]`, `[WithChangeFilter]`, `[WithOptions]`. Можно передать и свой `EntityQuery`.
- Параметры `Execute`: `ref T` (чтение-запись), `in T` (чтение) для `IComponentData`; `Entity`; `DynamicBuffer<T>`; `RefRW<T>`/`RefRO<T>`; `EnabledRefRW<T>`/`EnabledRefRO<T>`; `int` с одним из атрибутов:

| Атрибут | Значение | Цена |
| --- | --- | --- |
| `[ChunkIndexInQuery]` | индекс чанка в запросе (сейчас — **нефильтрованный**, см. комментарий в генераторе) | бесплатно; стандартный sort key для ECB |
| `[EntityIndexInChunk]` | индекс сущности в чанке | бесплатно; пара с `ChunkIndexInQuery` уникальна |
| `[EntityIndexInQuery]` | плотный индекс сущности в запросе | дорого: планирует `CalculateBaseEntityIndexArrayAsync` |

- Enableable-компоненты: `IJobEntity` **сам пропускает** сущности, у которых выключен требуемый компонент.
  Параметр `EnabledRefRW<T>` добавляет `T` в `All`, поэтому джоб видит только сущности с включённым `T`:
  выключить можно, включить обратно — нет. Чтобы перебрать и выключенные, добавь `[WithPresent(typeof(T))]`.
- `IJobEntityChunkBeginEnd` добавляет `OnChunkBegin`/`OnChunkEnd` для работы на уровне чанка.
- Формы планирования: без аргумента `JobHandle` (`ScheduleParallel()`) джоб сам берёт и обновляет `state.Dependency`;
  с явным `JobHandle` возвращает handle и **не** объединяет его с `state.Dependency` — это делаешь сам.
- `IAspect` удалён (в исходниках 6.6 его нет), хотя таблица параметров в документации 6.6 его ещё упоминает. `Entities.ForEach`/`Job.WithCode` устарели — только `IJobEntity`/`IJobChunk`.

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    public struct Health : IComponentData { public int Value; public int Max; }
    public struct Regen  : IComponentData { public int PerTick; }

    [BurstCompile]
    public partial struct RegenSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<Regen>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Неявная форма: джоб сам читает и дополняет state.Dependency.
            new RegenJob().ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct RegenJob : IJobEntity
    {
        // Каждая сущность пишет только свои данные — параллельно и детерминированно.
        void Execute(ref Health health, in Regen regen)
        {
            health.Value = math.min(health.Value + regen.PerTick, health.Max);
        }
    }
}
```

### IJobChunk

- `Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`.
- `unfilteredChunkIndex` — индекс в списке всех чанков запроса без учёта фильтров; чанки не обязательно обрабатываются по порядку.
- **Enableable-компоненты `IJobChunk` не обрабатывает сам.** Перебирай через `ChunkEntityEnumerator`, либо, если уверен, что enableable нет, проверь `Assert.IsFalse(useEnabledMask)`.
- `ComponentTypeHandle<T>` создаётся один раз в `OnCreate` и обновляется `.Update(ref state)` в каждом `OnUpdate`. Необновлённый handle считается устаревшим и даёт ошибку.
- `isReadOnly` у handle должен совпадать с реальным доступом: запись увеличивает change version чанка, даже если значение не менялось.

```csharp
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

public struct Poison : IComponentData { public int DamagePerTick; }

[BurstCompile]
struct PoisonJob : IJobChunk
{
    public ComponentTypeHandle<Health> HealthHandle;
    [ReadOnly] public ComponentTypeHandle<Poison> PoisonHandle;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex,
                        bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var healths = chunk.GetNativeArray(ref HealthHandle);
        var poisons = chunk.GetNativeArray(ref PoisonHandle);
        var it = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
        while (it.NextEntityIndex(out int i))
        {
            var h = healths[i];          // копия — меняем и пишем обратно
            h.Value -= poisons[i].DamagePerTick;
            healths[i] = h;
        }
    }
}

[BurstCompile]
public partial struct PoisonSystem : ISystem
{
    ComponentTypeHandle<Health> _health;
    ComponentTypeHandle<Poison> _poison;
    EntityQuery _query;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _health = state.GetComponentTypeHandle<Health>(isReadOnly: false);
        _poison = state.GetComponentTypeHandle<Poison>(isReadOnly: true);
        _query  = SystemAPI.QueryBuilder().WithAllRW<Health>().WithAll<Poison>().Build();
        state.RequireForUpdate(_query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _health.Update(ref state);
        _poison.Update(ref state);
        state.Dependency = new PoisonJob { HealthHandle = _health, PoisonHandle = _poison }
            .ScheduleParallel(_query, state.Dependency);
    }
}
```

### Что выбрать

- Перебор сущностей, одна операция на сущность → `IJobEntity`.
- Статистика по чанкам, несколько проходов по чанку, свой порядок перебора → `IJobChunk`.
- Данные вне ECS (временные массивы, сетка, сортировка) → `IJobFor`/`IJobParallelFor`/`IJobParallelForDefer`.
- Последовательная свёртка или применение отсортированного списка → `IJob`.
- Случайный доступ к чужим сущностям — `ComponentLookup<T>`/`BufferLookup<T>`: это самый медленный вид доступа, используй только когда без него нельзя.

Источники:
[Jobs overview](https://docs.unity3d.com/6000.6/Documentation/Manual/job-system-jobs.html) · [IJobEntity](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/iterating-data-ijobentity.html) · [IJobChunk](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/iterating-data-ijobchunk.html) ·
[Implement IJobChunk](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/iterating-data-ijobchunk-implement.html) · [Enableable components](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-enableable-use.html)

---

## 3. Планирование и зависимости

### Schedule / ScheduleParallel / Run

- `Schedule` — один рабочий поток (для `IJobEntity`/`IJobChunk` чанки обрабатываются последовательно).
- `ScheduleParallel` — чанки (или батчи индексов) распределяются по потокам.
- `Run` — немедленно на главном потоке и **сначала завершает все зависимости**. Это sync point. Только для отладки.

### JobHandle

- `Schedule` возвращает `JobHandle`; передача его в следующий `Schedule` делает зависимость.
- `JobHandle.CombineDependencies(a, b)`, `(a, b, c)`, `(NativeArray<JobHandle>)` объединяют несколько зависимостей.
- `handle.Complete()` ждёт джоб и все его зависимости и освобождает главному потоку доступ к контейнерам. Главный поток может сам выполнить незавершённый джоб, пока ждёт.
- `handle.IsCompleted` — проверка без ожидания. `JobHandle.ScheduleBatchedJobs()` — отправить накопленные джобы рабочим потокам.

### state.Dependency в ISystem

- Перед `OnUpdate` ECS собирает `state.Dependency` из handle'ов систем, которые писали компоненты, нужные этой системе (или читали то, что она пишет).
- Отслеживаются **только компоненты**. Если джоб A пишет `NativeArray`, а джоб B её читает, зависимость передаёшь руками.
- Отслеживание идёт на уровне системы: джоб может ждать чужие джобы, которые трогают не нужные ему компоненты. Если это мешает, раздели системы.

```csharp
[BurstCompile]
public partial struct CountAliveJob : IJobEntity
{
    // В параллельном IJobEntity/IJobChunk запись в NativeArray-поле по умолчанию разрешена
    // только в элемент с индексом текущего (нефильтрованного) чанка — ровно этот случай.
    public NativeArray<int> PerChunk;

    void Execute([ChunkIndexInQuery] int chunkIndex, in Health health)
    {
        if (health.Value > 0) PerChunk[chunkIndex] += 1;
    }
}

[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    int chunkCount = _query.CalculateChunkCountWithoutFiltering();
    // Живёт два кадра, освобождается автоматически, можно передавать в джобы.
    var perChunk = CollectionHelper.CreateNativeArray<int>(chunkCount, state.WorldUpdateAllocator);

    // Явная форма IJobEntity: handle нужно вернуть в state.Dependency самому.
    JobHandle h = new CountAliveJob { PerChunk = perChunk }
        .ScheduleParallel(_query, state.Dependency);
    // perChunk не отслеживается state.Dependency — зависимость по h.
    h = new SumJob { PerChunk = perChunk, Result = _result }.Schedule(h);
    state.Dependency = h;
}
```

### Sync points и Complete()

Sync point — место, где главный поток ждёт завершения запланированных джобов. Их дают:
структурные изменения через `EntityManager`, `Run()`, `foreach` по `SystemAPI.Query` при наличии
незавершённых джобов на этих данных, явные `Complete()`/`state.CompleteDependency()`, чтение
синглтона, в который пишет незавершённый джоб. В профайлере ожидание видно как маркер
`WaitForJobGroupID` на главном потоке. Правила проекта:

- не вызывать `Complete()` в `OnUpdate` симуляции; результат нужен следующей системе — отдаём через компоненты и `state.Dependency`;
- структурные изменения — через ECB; в проекте для тика это `EndSimulationTickEcbSystem` (ARCH-07), в общем случае Unity — `EndFixedStepSimulationEntityCommandBufferSystem`;
- системы со структурными изменениями ставить подряд: две такие системы подряд дают один sync point.

### Размер батча и рабочие потоки

- `IJobEntity`/`IJobChunk`: единица параллелизма — чанк, параметра batch нет (сверено по сигнатурам в исходниках 6.6).
- `IJobParallelFor.Schedule(n, innerloopBatchCount, dep)`, `IJobFor.ScheduleParallel(n, innerloopBatchCount, dep)`: документация советует начинать с 1 и увеличивать, пока растёт выигрыш. Длинный параллельный джоб займёт все потоки. Увеличенный батч ограничивает число потоков, которые его возьмут.
- Практика проекта (эмпирика, не из документации): для дешёвой операции на элемент начинать с 32–64, мерить на целевом устройстве.
- Джобы не прерываются. Длинную работу режь на цепочку коротких джобов, иначе они блокируют независимые цепочки.
- `JobsUtility.JobWorkerCount` (get/set) — текущее число рабочих потоков, `JobWorkerMaximumCount` — максимум, `ResetJobWorkerCount()` — вернуть авто-режим.
  **Android:** Unity сама меняет `JobWorkerCount`, когда ОС сообщает об изменении числа доступных ядер (энергосбережение). Ручная установка отключает эту подстройку до вызова `ResetJobWorkerCount()`. Без замеров не трогать.
- Редактор: Preferences › Jobs › Use Job Threads (выключение = все джобы на главном потоке, для отладки), Enable Jobs Debugger (safety checks).

Источники:
[Create and run a job](https://docs.unity3d.com/6000.6/Documentation/Manual/job-system-creating-jobs.html) · [Job dependencies](https://docs.unity3d.com/6000.6/Documentation/Manual/job-system-job-dependencies.html) · [Parallel jobs](https://docs.unity3d.com/6000.6/Documentation/Manual/job-system-parallel-for-jobs.html) ·
[Entities: job dependencies](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/scheduling-jobs-dependencies.html) · [Sync points](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/performance-sync-points.html) · [JobsUtility.JobWorkerCount](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount.html) ·
[Jobs preferences](https://docs.unity3d.com/6000.6/Documentation/Manual/preferences-jobs.html)

---

## 4. Нативные контейнеры и аллокаторы

### Контейнеры

`Native*` — с проверками безопасности потоков и освобождения. `Unsafe*` (namespace `Unity.Collections.LowLevel.Unsafe`) — без проверок.
Без safety checks разницы в скорости почти нет: большинство `Native*` — обёртки над `Unsafe*`.
`Native`-контейнер не может содержать `Native`-контейнер: `NativeList<UnsafeList<T>>` можно, `NativeList<NativeList<T>>` нельзя.

| Контейнер | Параллельная запись | Заметки |
| --- | --- | --- |
| `NativeArray<T>`, `NativeSlice<T>` (движок) | по своему индексу в `IJobParallelFor` | Фиксированная длина |
| `NativeList<T>` | `AsParallelWriter().AddNoResize` | Ёмкость в параллельном джобе не растёт — исключение при переполнении. `AsDeferredJobArray()` + `IJobParallelForDefer` |
| `NativeHashMap<K,V>`, `NativeHashSet<T>` | **нет** | Однопоточные, экономные по памяти |
| `NativeParallelHashMap<K,V>`, `NativeParallelHashSet<T>` | `AsParallelWriter().TryAdd` | Ёмкость задаётся заранее; если полна — `InvalidOperationException` |
| `NativeParallelMultiHashMap<K,V>` | `AsParallelWriter().Add` | Несколько значений на ключ: `TryGetFirstValue`/`TryGetNextValue`, `GetValuesForKey` |
| `NativeQueue<T>` | `AsParallelWriter().Enqueue` | Растёт блоками |
| `NativeStream` | `AsWriter()`: `BeginForEachIndex(i)` / `Write<T>` / `EndForEachIndex()` | Отдельный буфер на индекс — **детерминированный** порядок чтения |
| `NativeReference<T>` | — | Одно значение, аналог массива длины 1; результат джоба |
| `NativeRingQueue<T>`, `NativeBitArray`, `NativeText` | — | Кольцевой буфер фиксированного размера, биты, UTF-8 строка |
| `FixedList32/64/128/512/4096Bytes<T>` | — (значение) | Без аллокации; 2 байта служебных |
| `FixedString32/64/128/512/4096Bytes` | — (значение) | UTF-8; полезно 29/61/125/509/4093 байт |

Ключи хеш-контейнеров: `TKey : unmanaged, IEquatable<TKey>`, бакет считается по `key.GetHashCode()`.
Для своих struct-ключей реализуй `GetHashCode` явно, целочисленной арифметикой.

Сортировка и поиск: `NativeSortExtension.Sort`, `SortJob(...).Schedule(dep)`, `BinarySearch`.
Алгоритм — **IntroSort, нестабильный**: при равных ключах порядок не гарантирован. Компаратор должен задавать полный порядок (добавляй `StableId` вторым ключом).

### Аллокаторы

| Аллокатор | Время жизни | В джоб? | Освобождение |
| --- | --- | --- | --- |
| `Allocator.Temp` | кадр (главный поток) / время джоба (внутри джоба) | нельзя передать в поле джоба | автоматически; `Dispose` ничего не делает |
| `Allocator.TempJob` | ≤ 4 кадров | да | вручную |
| `Allocator.Persistent` | сколько нужно | да | вручную; самый медленный (`malloc`) |
| `state.WorldUpdateAllocator` / `World.UpdateAllocator` | 2 обновления мира (двойной rewindable) | да | автоматически |
| Аллокатор ECB-системы | как у ECB | да | автоматически |
| Аллокатор группы систем (`SetRateManagerCreateAllocator`) | 2 обновления группы | да | автоматически |
| `RewindableAllocator` через `AllocatorHelper<T>` | до `Rewind()` | да | `Rewind()` / `Dispose()` хелпера |

- **TempJob > 4 кадров:** предупреждение в консоли (не исключение), аллокация остаётся валидной. Если пул TempJob исчерпан долгоживущими аллокациями, следующие идут через медленный Persistent.
- Выравнивание: Temp и rewindable — 64 байта, TempJob и Persistent — 16.
- `NativeArray` из пользовательского аллокатора (в том числе `WorldUpdateAllocator`) создаётся через `CollectionHelper.CreateNativeArray<T>(n, allocator)`. Контейнеры Collections принимают `AllocatorManager.AllocatorHandle` в конструкторе, `Allocator` приводится к нему неявно.
- В ядре 6.6 есть `Allocator.Domain` («на время жизни домена»). Семантика подробнее не документирована — не использовать (не проверено).
- Освобождение после джоба: `container.Dispose(jobHandle)` планирует dispose-джоб. Альтернатива — атрибут `[DeallocateOnJobCompletion]`.
- `Dispose` на одной копии struct не сбрасывает `IsCreated` у других копий.
- Утечки: Preferences › Jobs › Leak Detection Level (`Disabled`/`Enabled`/`Enabled With Stack Trace`), из кода — `NativeLeakDetection.Mode`. В редакторе и dev-билдах по умолчанию `Enabled`. Отчёт выводится при выгрузке домена.
- Известная проблема: все `Temp`-контейнеры одного потока делят один `AtomicSafetyHandle`. Операция над одним Temp-хешсетом может инвалидировать `NativeList.AsArray()` другого Temp-списка.

### Система безопасности

Каждый `Native`-контейнер несёт `AtomicSafetyHandle`. В редакторе и dev-билдах (`ENABLE_UNITY_COLLECTIONS_CHECKS`)
при `Schedule` проверяется, что два джоба без зависимости не пишут в одни данные и что главный поток
не трогает контейнер, занятый джобом. Параллельно читать можно, если все участники помечены `[ReadOnly]`.
В релизных билдах проверок нет: гонка, пойманная в редакторе, в релизе просто портит данные.

| Атрибут | Смысл |
| --- | --- |
| `[ReadOnly]` | Только чтение: джобы с одним контейнером могут идти параллельно, главный поток может читать |
| `[WriteOnly]` | Только запись |
| `[NativeDisableParallelForRestriction]` | Параллельный джоб пишет вне своего диапазона индексов. Диапазон по умолчанию: `IJobParallelFor` — индекс итерации; параллельный `IJobChunk`/`IJobEntity` — элемент `[unfilteredChunkIndex]` (с `[EntityIndexInQuery]` — диапазон сущностей чанка). Нужен и для записи через `ComponentLookup` в `ScheduleParallel`. Отсутствие гонок гарантируешь сам |
| `[NativeDisableContainerSafetyRestriction]` | Полностью отключает проверки для поля. Burst считает, что поле может алиаситься с другими контейнерами (хуже оптимизация) |
| `[NativeSetThreadIndex]` | Внедряет индекс рабочего потока в `int`-поле |
| `[DeallocateOnJobCompletion]` | Освободить контейнер по завершении джоба |

Элементы — копии: `arr[i].Value = x` не компилируется для struct (CS1612). Читай в локальную переменную, меняй, пиши `arr[i] = tmp`.

Пример: пространственная сетка для 2D-карты с неотрицательными целыми координатами.

```csharp
[BurstCompile]
public partial struct FillGridJob : IJobEntity
{
    public NativeParallelMultiHashMap<int, Entity>.ParallelWriter Grid;
    public int CellSize;   // в тех же целых единицах, что и GridPos
    public int GridWidth;  // ширина сетки в ячейках

    void Execute(Entity e, in GridPos pos)
    {
        int key = (pos.Y / CellSize) * GridWidth + (pos.X / CellSize);
        Grid.Add(key, e);  // порядок значений внутри ключа недетерминирован — см. раздел 6
    }
}

// OnUpdate: ёмкость = число сущностей, память живёт два кадра.
var grid = new NativeParallelMultiHashMap<int, Entity>(_query.CalculateEntityCount(),
                                                       state.WorldUpdateAllocator);
state.Dependency = new FillGridJob { Grid = grid.AsParallelWriter(), CellSize = 4, GridWidth = w }
    .ScheduleParallel(_query, state.Dependency);
```

Источники:
[Collections overview](https://docs.unity3d.com/Packages/com.unity.collections@6.6/manual/collections-overview.html) · [Collection types](https://docs.unity3d.com/Packages/com.unity.collections@6.6/manual/collection-types.html) · [Allocator overview](https://docs.unity3d.com/Packages/com.unity.collections@6.6/manual/allocator-overview.html) ·
[Rewindable allocator](https://docs.unity3d.com/Packages/com.unity.collections@6.6/manual/allocator-rewindable.html) · [Known issues](https://docs.unity3d.com/Packages/com.unity.collections@6.6/manual/issues.html) · [Entities allocators](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/allocators-overview.html) ·
[World update allocator](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/allocators-world-update.html) · [NativeContainer](https://docs.unity3d.com/6000.6/Documentation/Manual/job-system-native-container.html)

---

## 5. Burst

Burst переводит IL в нативный код через LLVM. В редакторе компиляция JIT и по умолчанию **асинхронная**:
пока Burst-версия не готова, код выполняется как обычный managed. Первые кадры Play Mode — не Burst,
и профилировать их нельзя. В Player-билдах — AOT. Меню: **Jobs › Burst** (Enable Compilation,
Enable Safety Checks: Off/On/Force On, Synchronous Compilation, Native Debug Mode Compilation, Show Timings, Open Inspector).
Настройки билда: Project Settings › Burst AOT Settings (отдельно на каждую платформу).
`ENABLE_BURST_AOT` — define, если в AOT Settings включён Burst.

### Куда ставить `[BurstCompile]`

- **Джоб** — на struct джоба (включая `partial struct : IJobEntity`). Всё, что вызывается из `Execute`, компилируется тоже.
- **ISystem** — на struct системы и на `OnCreate`/`OnUpdate`/`OnDestroy`.
- **Статический метод** — на метод и на содержащий тип (class/struct). Такой метод можно вызывать из managed-кода напрямую: IL post-processing превращает вызов в function pointer. Отключается `DisableDirectCall = true`. Generic-методы и методы generic-типов так вызывать нельзя.
- **Сборка** — `[assembly: BurstCompile(OptimizeFor = ...)]` задаёт умолчания. Приоритет: меню Burst → атрибут на джобе → атрибут сборки.

Параметры атрибута: `FloatMode`, `FloatPrecision`, `CompileSynchronously`, `DisableSafetyChecks`,
`OptimizeFor` (`Performance`/`Size`/`FastCompilation`/`Balanced` — по умолчанию), `Debug`, `DisableDirectCall`.

### Что поддерживается (HPC#)

- Типы: `bool`, `byte/sbyte`, `short/ushort`, `int/uint`, `long/ulong`, `float`, `double`, enum'ы (без методов `Enum`, например `HasFlag`), struct'ы (Sequential/Explicit), указатели, `IntPtr`. `Span<T>`/`ReadOnlySpan<T>` и `ValueTuple` — только внутри Burst-кода, не через границу вызова.
- **Не поддерживаются:** `char`, `decimal`, `string` (managed), классы и любые managed-объекты, делегаты (вместо них `FunctionPointer<T>`), `catch`, запись в статические поля (кроме `SharedStatic<T>`), методы `string`, `foreach` по generic-параметру `S : IEnumerable<T>`.
- Статика: только `static readonly`, вычисляется при компиляции. `static readonly` managed-массивы можно читать напрямую, но нельзя передавать дальше. Многомерные массивы не поддерживаются.
- `System.Math`, `Interlocked` (адрес должен быть естественно выровнен), `Volatile.Read/Write`, `Thread.MemoryBarrier`.
- `System.HashCode` поддержан, но **результат отличается от .NET** (у Burst нет seed managed-реализации). Для чего-то авторитетного не использовать.
- Исключения: только простые `throw new X("literal")`. В редакторе исключение ловится managed-кодом, **в Player-билде приложение аварийно завершается**. `finally` при исключении не выполняется. Throw вне метода с `[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]` даёт предупреждение BC1370.
- Строки: `Debug.Log/LogWarning/LogError` с литералом или интерполяцией (только встроенные типы и векторы Mathematics; `ToString()` struct'ов не вызывается). Хранить и передавать строки — через `FixedStringNBytes`.
- Векторы Mathematics (`float4`, `int4`, `bool4`, `uint4` и 2/3-компонентные) переводятся в SIMD; 4-компонентные предпочтительнее.

### Float: FloatMode, FloatPrecision и детерминизм

| `FloatMode` | Поведение |
| --- | --- |
| `Default` | = `Strict` |
| `Strict` | Без перестановок, NaN и денормалы соблюдаются (по умолчанию) |
| `Fast` | Алгебраические перестановки, FMA, обратные величины; допускает отсутствие NaN/Inf. Результат может отличаться |
| `Deterministic` | Одинаковый float-результат на всех поддерживаемых платформах. **Только 64-бит** |

`FloatPrecision`: `Standard` (= `Medium`, 3.5 ulp), `High` (1 ulp), `Medium` (3.5 ulp), `Low` (для `sin/cos/exp/log/pow/fmod…` 350 ulp, есть ограничения диапазона).
Умолчание для билда задаёт поле Floating Point Mode в Burst AOT Settings, атрибут его переопределяет.

**Статус `FloatMode.Deterministic` в установленной версии — реализован.**
XML движковой сборки `UnityEngine.BurstModule` (6000.6.3f1): *«Ensure that floating point calculations are deterministic (64-bit only).»*
Manual 6.6: *«Ensure that floating point calculation in Burst are deterministic, i.e., consistent across all supported platforms. Only supported on 64-bit architectures.»*

Источники расходятся из-за истории версий: до Burst 1.8.24 режим был помечен «Reserved for future»;
в CHANGELOG 1.8.25 (2025-09-16) написано *«FloatMode.Deterministic is now supported by Burst»*.
Встроенный Burst в 6.6 — уже с реализацией. Ограничения (из Manual 6.6):

- действует только на Burst-код; managed-код (Mono/IL2CPP, .NET-сервер) этой гарантии не получает;
- денормалы сбрасываются в ноль на всех платформах; битовое представление NaN может различаться;
- процессорные интринсики и платформенные инструкции могут сломать детерминизм (Burst выдаёт предупреждение);
- float-входы, посчитанные вне Burst, и function pointer из кода с другим `FloatMode` ломают детерминизм;
- часть оптимизаций отключена, float-код медленнее — насколько, нужно мерить.

Для проекта: 32-битные ARMv7-сборки Android под этот режим не подходят, собирать только ARM64.
Совпадение между устройствами в самом проекте **не проверено** — до golden-теста iOS ↔ Android ↔ десктоп не опираться.
Авторитетная логика всё равно целочисленная (раздел 6).

### SharedStatic, function pointers, интринсики, BurstDiscard

```csharp
// Изменяемая статика, общая для C# и Burst. Инициализировать из C# до первого чтения в Burst.
public abstract class SimCounters
{
    public static readonly SharedStatic<int> Ticks =
        SharedStatic<int>.GetOrCreate<SimCounters, TicksKey>();
    private class TicksKey {}
}
```

- `FunctionPointer<T>`: `BurstCompiler.CompileFunctionPointer<TDelegate>(StaticMethod)`. Метод и тип — `[BurstCompile]`; для IL2CPP на методе нужен `[AOT.MonoPInvokeCallback(typeof(TDelegate))]`. Generic-делегаты не поддерживаются, большинство `NativeContainer` передать нельзя. Документация: джоб быстрее function pointer; если всё же нужен указатель — обрабатывай пачку, а не элемент.
- Интринсики: `Unity.Burst.Intrinsics.Arm.Neon` (перед использованием проверять `IsNeonSupported`), `X86.*`, `Common` (`Pause`, `Prefetch`, `umul128`, `InterlockedAnd/Or`). Подсказки компилятору: `Unity.Burst.CompilerServices` — `Hint.Likely/Unlikely`, `Constant.IsConstantExpression`, `[AssumeRange]`, `[SkipLocalsInit]`, `Loop.ExpectVectorized()`. Алиасинг: `[NoAlias]`; поля-контейнеры джоба и так не алиасятся. Нужны редко, мешают `FloatMode.Deterministic`.
- `[BurstDiscard]` — метод выбрасывается из Burst-компиляции (например, managed-логирование). Не может возвращать значение. Узнать, выполняется ли код под Burst, можно через `ref`-параметр.
- Generic-джобы: в билде компилируются только конкретные инстанциации (`MyJob<int>`). Планирование generic-джоба через generic-метод (`Schedule<TData>()`) **не поддерживается**: в редакторе он работает на Burst, а в Player — без Burst.

### Burst Inspector

Jobs › Burst › Open Inspector. Слева список целей компиляции (выключенные — без `[BurstCompile]`),
справа Assembly / .NET IL / LLVM IR (до и после оптимизаций) / LLVM Optimization Diagnostics.
Полезно: выпадающий список архитектуры (выбирать ARMV8A), Safety Checks (выключить для реальной картины),
«Highlight SIMD Scalar vs Packed» — быстро видно, векторизовался ли цикл.

### iOS / Android

- AOT при сборке Player: Burst собирает одну динамическую библиотеку `lib_burst_generated` в папке `Plugins`. Для iOS вместо неё — **статическая библиотека** (требование Apple для TestFlight); для сборки Xcode-проекта нужен macOS + Xcode.
- iOS: только ARMV8 AARCH64. Android: x86 SSE2, ARMV7 (Thumb2, Neon32), ARMV8 AARCH64 с целями `ARMV8A` (по умолчанию), `ARMV8A_HALFFP` (fullfp16, dotprod, crypto, crc, rdm, lse; Cortex A75/A55 и новее), `ARMV9A` (+SVE2, **экспериментально**). На Android arm64 работает динамический выбор версии по CPU.
- Android NDK — ставить через Unity Hub. Если внешние инструменты не настроены, Burst берёт `ANDROID_NDK_ROOT`. Android-сборка из Linux-редактора Burst не поддерживает.
- Если тулчейн цели неисправен, Unity соберёт Player **без Burst**, без ошибки сборки. Проверяй, что Burst-библиотека попала в билд.
- Отладка в Player: Development Build или Force Debug Information, плюс `[BurstCompile(Debug = true)]` на конкретном джобе.

Источники:
[Marking code for Burst](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/compilation-burstcompile.html) · [HPC# overview](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/csharp-hpc-overview.html) · [Type support](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/csharp-type-support.html) ·
[String support](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/csharp-string-support.html) · [System support](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/csharp-system-support.html) · [Float precision and determinism](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/float-precision-determinism.html) ·
[SharedStatic](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/csharp-shared-static.html) · [Function pointers](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/csharp-function-pointers.html) · [Generic jobs](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/compilation-generic-jobs.html) ·
[BurstDiscard](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/compilation-burstdiscard.html) · [Burst Inspector](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/editor-burst-inspector.html) · [Burst menu](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/editor-burst-menu.html) ·
[Platform build support](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/building-projects.html) · [AOT settings](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/building-aot-settings.html) · [Burst 1.8 CHANGELOG](https://docs.unity3d.com/Packages/com.unity.burst@1.8/changelog/CHANGELOG.html)

---

## 6. Детерминизм и порядок

Цель: одинаковые входы (сид, ввод за тик, операции) дают побитово одинаковое авторитетное состояние
на любом устройстве и в будущем на сервере. Архитектура: `Docs/Tech/ArchitectureDecisions.md`, R-05.

### Источники недетерминизма

| Источник | Почему | Что делать |
| --- | --- | --- |
| Обход `Native*HashMap/Set`, `GetKeyArray`, `GetKeyValueArrays` | Порядок по контракту не определён («in no particular order»); зависит от ёмкости, истории вставок и реализации, которая может смениться с версией Unity. На .NET-сервере реализация другая | Собрать ключи → отсортировать → обходить |
| `ParallelWriter` любого контейнера | Порядок записей зависит от планирования потоков | `NativeStream` (буфер на индекс) или сортировка после записи |
| Значения одного ключа в `NativeParallelMultiHashMap` | Порядок вставки из разных потоков случаен | Выбор по полному ключу `(метрика, StableId)` |
| ECB `ParallelWriter` с одинаковым/случайным sort key | Команды одного ключа из разных потоков перемешаны | Sort key = `ChunkIndexInQuery` / `unfilteredChunkIndex` |
| Порядок чанков и `Entity.Index` | Зависит от истории структурных изменений, у сервера и клиента разный | Логика и сохранения — по `StableId`, не по `Entity` |
| Float между платформами и компиляторами | Mono/IL2CPP/.NET/Burst, FMA в `FloatMode.Fast`, денормалы | Авторитетные числа — `int`/`long`/fixed-point. В Unity.Mathematics fixed-point типа нет, писать свой |
| Асинхронная компиляция Burst в редакторе | Первые кадры идут managed-кодом, float может отличаться | Детерминизм-тесты — с Synchronous Compilation или в Player |
| Параллельная свёртка float / `Interlocked` | Сложение float не ассоциативно, порядок случаен | Частичные суммы по чанку → последовательная свёртка целых в фиксированном порядке |
| `System.HashCode`, `GetHashCode` по умолчанию | Burst ≠ .NET | Свой хеш (например, `xxHash3` из Collections в клиенте; в ядре правил — свой) |
| Нестабильная сортировка (IntroSort) | Равные ключи в любом порядке | Компаратор с `StableId` на последнем месте |
| `JobsUtility.ThreadIndex`, число потоков, `Time` | Зависят от устройства | Не использовать в логике; время — только номер тика |

### Unity.Mathematics.Random

- Xorshift, состояние 32 бита, value type. `new Random(seed)`: **seed ≠ 0**. `Random.CreateFromIndex(uint index)` хеширует индекс, `index ≠ uint.MaxValue`.
- `NextInt(min, max)` — полуинтервал `[min, max)`, `NextUInt(max)` — `[0, max)`. Для авторитетной логики — только целочисленные методы, не `NextFloat*`.
- Ловушка value type: копия `Random` в поле джоба продвигает свою копию. Исходное состояние не меняется, и следующий кадр повторит ту же последовательность. Сохраняй состояние обратно (компонент на сущности) или создавай генератор заново из `(worldSeed, stableId, tick)`.
- Один `Random` на весь параллельный джоб — гонка и зависимость от порядка. Нужен свой генератор на сущность или на операцию.
- Решение проекта (журнал, A-16): `Unity.Mathematics.Random`, состояние в компонентах, сиды по правилам GDD. Собственный ГСЧ не используется.

### Рекомендуемые паттерны

1. **Каждая сущность пишет только свои компоненты** — параллельно и детерминированно без дополнительных усилий.
2. **Собрать → отсортировать → применить.** Параллельный джоб пишет кандидатов (`NativeStream` или `NativeList.ParallelWriter`), затем `SortJob` с компаратором `(ключ, StableId)`, затем последовательный `IJob` применяет результат.
3. **ECB:** sort key — `[ChunkIndexInQuery]` в `IJobEntity` или `unfilteredChunkIndex` в `IJobChunk`. Команды с большим ключом проигрываются после команд с меньшим. Это даёт детерминизм при одинаковой раскладке чанков, т. е. для одного прогона и реплея на том же клиенте. Между разными машинами опирайся на `StableId`.
4. **Свёртки:** массив частичных результатов длиной `CalculateChunkCountWithoutFiltering()`, индекс — `ChunkIndexInQuery`; затем `IJob` суммирует по порядку индексов.
5. **Float только там, где расхождение не влияет на авторитетное состояние** (движение, визуал). Если float-реплей между устройствами всё же нужен — `[BurstCompile(FloatMode = FloatMode.Deterministic)]` на соответствующих джобах, 64-бит, и golden-тест.

```csharp
// Детерминированное удаление погибших: порядок проигрывания не зависит от потоков.
[BurstCompile]
public partial struct DeathJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;

    void Execute(Entity e, [ChunkIndexInQuery] int sortKey, in Health health)
    {
        if (health.Value <= 0)
            Ecb.DestroyEntity(sortKey, e);
    }
}

// В OnUpdate системы фиксированного тика:
var ecb = SystemAPI.GetSingleton<EndSimulationTickEcbSystem.Singleton>() // в проекте (ARCH-07)
    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
new DeathJob { Ecb = ecb }.ScheduleParallel();
```

```csharp
// Полный порядок для сортировки: равенство только у одной и той же сущности.
public struct ByDistanceThenId : IComparer<Candidate>
{
    public int Compare(Candidate a, Candidate b)
    {
        int c = a.DistSq.CompareTo(b.DistSq);   // int/long, не float
        return c != 0 ? c : a.StableId.CompareTo(b.StableId);
    }
}
// candidates.SortJob(new ByDistanceThenId()).Schedule(dep);
```

Источники:
[Float precision and determinism](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/float-precision-determinism.html) · [Parallel readers and writers](https://docs.unity3d.com/Packages/com.unity.collections@6.6/manual/parallel-readers.html) · [ECB playback](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-entity-command-buffer-playback.html) ·
[Unity.Mathematics.Random](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Mathematics.Random.html) · [System support (HashCode)](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/csharp-system-support.html)

---

## 7. Производительность на мобильных

### Раскладка данных

- Чанк — 16 KiB, максимум 128 сущностей (поэтому маска включённости — `v128`). Компоненты в чанке уже лежат SoA; задача — чтобы в чанк помещалось много сущностей и джоб читал только нужное.
- Мелкие компоненты под конкретную систему. Горячие данные отдельно от холодных. Большую «сущность-персонажа» можно разбить на несколько сущностей по группам систем.
- Общие неизменяемые данные (статы врагов, конфиги) — в BlobAsset, не копией в каждой сущности.
- Фрагментация: много архетипов с малым числом чанков (часто из-за add/remove тегов) и неправильные shared-компоненты. Проверять в Entities Memory Profiler module.
- `float4`/`int4` векторизуются лучше, чем 2/3-компонентные. Ветвления в горячем цикле мешают векторизации.

### Структурные изменения

- Вместо add/remove тегов-состояний — enableable-компоненты: без смены архетипа, без sync point, можно переключать в джобе (`EnabledRefRW<T>`, `ComponentLookup<T>.SetComponentEnabled`). Джоб с правом записи в enableable-компонент может блокировать операции главного потока до своего завершения.
- ECB: для множества одинаковых команд — batched-перегрузки (`Instantiate(Entity, NativeArray<Entity>)` и т. п.). ECB сам одинаковые команды не объединяет.
- Структурное изменение инвалидирует `DynamicBuffer` и прямые ссылки на данные компонентов.

### Пространственное разбиение

- Сетка карты целочисленная — индекс ячейки считается из целых координат, без float.
- Два варианта: (1) `NativeParallelMultiHashMap<int cell, Entity>` через `ParallelWriter` — просто, но порядок внутри ячейки случаен; (2) массив `(cell, StableId, index)` → `SortJob` → диапазоны ячеек. Второй вариант детерминирован и лучше для кэша, для авторитетной логики предпочтителен.
- Ёмкость хеш-контейнеров задавай заранее (`CalculateEntityCount()`): `ParallelWriter` не умеет расти.
- `ComponentLookup` в горячем цикле — случайный доступ. Если можно, копируй нужные поля соседей в плотный массив до поиска.

### Профилирование

- Profiler › CPU Usage › **Timeline**: видно рабочие потоки, простои, `WaitForJobGroupID` (главный поток ждёт джобы).
- Entities Memory Profiler module (архетипы, заполненность чанков), модуль Structural Changes.
- Burst Inspector — проверить векторизацию и что код вообще Burst-компилирован.
- Мерить на устройстве (Development Build с подключённым Profiler), не в редакторе. Детали планирования — нативные профайлеры платформы (документация упоминает Instruments, Superluminal и др.).
- В редакторе safety checks и Jobs Debugger искажают картину: для замеров Burst › Safety Checks = Off (сбрасывается в On при перезапуске редактора) и прогрев перед замером.

### Частые ошибки

- `Complete()`/`Run()` в `OnUpdate` «чтобы проще» — sync point каждый кадр.
- Нет `[BurstCompile]` на джобе или системе — код молча идёт через Mono/IL2CPP.
- `[EntityIndexInQuery]` там, где хватает `[ChunkIndexInQuery]` — лишний проход по чанкам.
- Handle не обновлён (`.Update(ref state)`) — ошибка устаревшей версии.
- Компонент объявлен `ref`, а только читается — лишние зависимости и ложные срабатывания change filter.
- `ParallelWriter` без запаса ёмкости — исключение в редакторе, порча памяти в релизе.
- `TempJob` держится дольше 4 кадров — предупреждение и деградация до Persistent.
- Generic-джоб планируется через generic-метод — в Player без Burst.
- Тяжёлый джоб на весь кадр забивает все потоки — режь на части.
- Тулчейн Android/iOS не настроен — билд без Burst без явной ошибки.

Источники:
[Chunk allocations](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/performance-chunk-allocations.html) · [Enableable components](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/components-enableable-use.html) · [Use an ECB](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/systems-entity-command-buffer-use.html) ·
[Job overhead](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/job-overhead.html) · [CPU Usage Profiler](https://docs.unity3d.com/6000.6/Documentation/Manual/profiler-cpu.html) · [Type support (vectors)](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/csharp-type-support.html)

---

## 8. Шпаргалка изменений относительно старого `UnityJobSystem.md`

| Было в старом документе / в 2022.3 | Сейчас (6000.6, пакеты 6.6) |
| --- | --- |
| Unity 2022.3, Entities 1.x, отдельные пакеты Burst/Collections/Mathematics | Core-пакеты с редактором. Burst 2.0.0 и Mathematics 1.4.0 — пустые прокладки, код в движковых модулях |
| Документация Burst — `com.unity.burst@1.x` | Manual 6.6, раздел `Manual/burst/`. Страниц `com.unity.burst@2.0` нет |
| TempJob: «исключение, если не освобождён за 4 кадра» | Предупреждение, аллокация валидна. При исчерпании пула — откат на медленный Persistent |
| Три аллокатора: Temp / TempJob / Persistent | Плюс `WorldUpdateAllocator` (рекомендуемый для кадровых данных ECS), ECB- и group-аллокаторы, `RewindableAllocator`, `Allocator.Domain` в ядре |
| Примеры на MonoBehaviour: `Schedule()` → сразу `Complete()` | `ISystem` + `state.Dependency`, без `Complete()` |
| Пример «Targets and Seekers» (GameObject, брутфорс, сортировка по X) | Убран. Вместо него — сетка на `NativeParallelMultiHashMap` или сортировка по ячейке (разделы 4, 7) |
| `IJobEntity`/`IJobChunk` только упомянуты | Разделы про codegen, индексы, enableable, `ChunkEntityEnumerator`, handles |
| `Entities.ForEach`, `Job.WithCode`, `IAspect` | Устарели / удалены. `IAspect` в 6.6 нет, хотя таблица документации его ещё показывает |
| `FloatMode.Deterministic` — «reserved for future» (Burst ≤ 1.8.24) | Реализован (Burst 1.8.25+, встроенный Burst 6.6), только 64-бит, с оговорками (раздел 5). В AOT Settings появилось поле Floating Point Mode |
| Запись в `NativeArray` из параллельного джоба — «только по index» | Для параллельного `IJobChunk`/`IJobEntity` разрешённый элемент — индекс чанка; остальное — через `[NativeDisableParallelForRestriction]` |
| Порядок параллельной записи не обсуждался | Раздел 6: `ParallelWriter`, хеш-карты, ECB sort keys, нестабильная сортировка |
| Batch size «подбирать на глаз» | Документация: начинать с 1 и увеличивать; у `IJobEntity`/`IJobChunk` батча нет — единица — чанк |
| Число потоков не обсуждалось | `JobWorkerCount` на Android подстраивается ОС; ручная установка отключает подстройку |
| `NativeDisableContainerSafetyRestriction` — «просто выключить проверки» | Ещё и снимает гарантию неалиасинга для Burst |
| SortJob: SegmentSort + SegmentSortMerge | Так и есть (сегменты по 1024), но сортировка нестабильна — нужен полный порядок |

Источники: см. разделы 1–7. Базовый индекс:
[Job system (Manual 6.6)](https://docs.unity3d.com/6000.6/Documentation/Manual/job-system.html) · [Burst compilation (Manual 6.6)](https://docs.unity3d.com/6000.6/Documentation/Manual/burst/script-compilation-burst.html) · [Collections 6.6](https://docs.unity3d.com/Packages/com.unity.collections@6.6/manual/index.html) ·
[Entities 6.6](https://docs.unity3d.com/Packages/com.unity.entities@6.6/manual/index.html)
