> **Это НЕ архитектура Rise to Panteon.** Краткая карта продакшн-проекта WO (мобильная 4X, клиент Unity + бэкенд .NET/Orleans, репозиторий `~/ProjectsUnity/vibecode`). Используется только как референс в спорных решениях. Архитектура этого проекта — `Docs/Tech/ArchitectureDecisions.md`.

# Архитектура WO — краткая карта

Мобильная 4X-стратегия. Два режима игры:

- **Город** — одиночная часть. Всё, что игрок делает у себя, превращается в операции, и сервер повторяет их пачками.
- **Карта** — общий мир королевства. Им управляет живая симуляция на сервере, и все игроки видят его одинаково.

Проект разбит на королевства (realm). У каждого свои базы данных, свой NATS и свой процесс карты.

## Репозитории

| Каталог | Что это | Язык |
|---|---|---|
| `backend-services/` | Игровой бэкенд: все сервисы и общие библиотеки, решение `GR.WO.Services.sln` | C# .NET, Orleans |
| `client-unity-N/` | Клиент на Unity, 5 рабочих копий одного репозитория. Общие git-объекты лежат в `client-unity-shared/` | C# Unity |
| `chat/` | Чат: 3 сервиса | Python 3.12 |
| `static-mapping/` | StaticUtils: переносит таблицы геймдизайна из Google Sheets в PostgreSQL | C# .NET 8 |
| `backend-migrations` | Миграции всех баз (golang-migrate). Локальной копии в vibecode нет | SQL |
| `docs/`, `.claude/`, `e2e/`, `tools/` | Спеки, конституция, база знаний для RAG, агенты и скиллы, e2e-сценарии | — |

---

## 1. Базовая модель (конституция, `docs/specs/constitution/`)

1. **Сервер авторитарен, клиент предсказывает.** Клиент применяет действие к своим моделям сразу, без ожидания сервера. Затем он кладёт операцию в пачку и позже отправляет. Сервер повторяет операцию на своих данных. От клиента он принимает только намерение, результат расчёта клиента не принимает.
2. **Операция самодостаточна и детерминирована.** В операции есть тип, параметры, время и seed случайности. По ним сервер получает тот же результат, что предсказал клиент.
3. **Операции одного игрока идут строго по очереди.** Пачка обрабатывается идемпотентно и сохраняется одной транзакцией.
4. **Ревизию двигает только клиент.** Это счётчик применённых операций, по нему сервер отбрасывает дубли. Сервер повышает ревизию, только когда применяет клиентскую операцию, и ещё при авто-отыгровке.
5. **В одном стеке вызовов допустима одна операция.** Операция не порождает другую операцию.
6. **Валидация одинакова на обеих сторонах.** Если стороны разошлись, прав сервер: он отвечает 409, и клиент перезагружает стейт.
7. **Id сущностей поделены по диапазонам.** Id до 1 500 000 выдаёт клиент, от 1 500 000 — сервер, и только для сущностей, созданных картой. В операции, пришедшей от клиента, сервер новых id не выдаёт.
8. **Загрузка данных:**
   - `getCurrent` отдаёт полный стейт, и только на старте сессии;
   - `getPartial` догружает только то, что создал сервер.
9. **Статика одна для клиента и сервера.** Эффекты — это формулы над значениями статики. Сервер компилирует их через Roslyn, клиент интерпретирует. Код у сторон разный, контракт один.
10. **Город и Карта взаимоисключающие.** Активна ровно одна сцена. Скрытая сцена не должна ничего делать: ни рисовать, ни обрабатывать ввод, ни менять стейт.
11. **Состояние карты клиент читает только по WebSocket** через `Map.ProxyService`. Состояние альянса, от которого зависят решения клиента, приходит в стейт игрока дельтой. Живым запросом к карте клиент его не читает.

---

## 2. Общая схема

```
                    Клиент (Unity)
      HTTP JSON-RPC │        │ WebSocket (бинарные дельты по чанкам)
                    ▼        ▼
          Town.Service     Map.ProxyService ── 1 соединение на realm ──► Map.Service (ECS в памяти)
                    │ Orleans                                              ▲   │ Map DB
                    ▼                                                      │   │
          Town.GrainHost (DynUserGrain = единственный writer игрока) ──gRPC─┘   │ таблицы-outbox
                    │ Town DB (MemoryPack-блоб + outbox)                        ▼
                    │                                         Map.Workers ─gRPC─► Town.Service.Internal ─► грейн
                    └─► outbox → NATS JetStream (realm) → Town.Workers / прочие консьюмеры
       NATS для клиента: сигналы «перезапроси» (user-partial, profile-mv-update, alliance-mv-update)
```

---

## 3. Бэкенд (`backend-services/`)

### Сервисы

Каждому сервису соответствует свой Dockerfile в корне репозитория, цели сборки описаны в `docker-bake.hcl`.

| Сервис | Что делает | Вход | База |
|---|---|---|---|
| **Town.Service** | Клиентские запросы города: `ops`, `getCurrent`, `getPartialState`, магазин, почта. Действия на карте тоже идут через него (`MapProxyController`) | HTTP JSON-RPC | — (проксирует в грейн) |
| **Town.GrainHost** | Orleans-кластер. Единственный, кто пишет стейт игрока | Orleans | **Town DB** (своя на realm) |
| **Town.Service.Internal** | Вход в стейт игрока для других сервисов: завершение марша, лут, дифф альянса и т.п. | gRPC + NATS-консьюмеры | — |
| **Town.Workers** | Отправка outbox города, cron-задачи: сбор ресурсов, санация, архивация | Quartz / фон | Town DB (чтение) |
| **Map.Service** | Карта мира на ECS: движение, бои, ивенты. Один процесс на королевство, весь снимок в памяти | gRPC + WS | **Map DB** (своя на realm) |
| **Map.ProxyService** | Публичный WS-вход клиента на карту. Склеивает одинаковые подписки клиентов в одну | WebSocket | — |
| **Map.Workers** | Доставка результатов карты в город: завершённые марши, лут, слияния | фон + gRPC | Map DB (чтение) |
| **MapCoordinator.Service** | Координатор над всеми королевствами: общие лидерборды, расписание глобальных ивентов | HTTP + gRPC + cron | MapCoordinator DB |
| **Mv.Service / Mv.Workers** | Материализованные представления (MV): профиль, альянс, лидерборды. Service только читает, Workers пересобирают | HTTP / cron | MV DB |
| **Auth.Service (+ .Internal)** | Логин, JWT, привязка аккаунтов, выдача NATS-токена | HTTP / gRPC | Security DB |
| **Update.Service** | Версии клиента и статики, техработы, выбор кластера для клиента | HTTP | Config DB |
| **Config.Workers** | Реестры королевств | фон | Config DB |
| **Bot** | AI-боты, которые играют как люди. Плагины подключаются через keyed DI, тексты генерирует LLM | HTTP к Town, NATS | — |
| **Notificator / Push** | Письма игрокам; пуши через Firebase | NATS | своя |
| **Analytics.Service (+ .Store)** | Приём аналитических фактов и запись в ClickHouse | NATS | ClickHouse |
| **Finance.Internal / CurrencyRate** | Проверка покупок; курсы валют | gRPC / HTTP | Finance DB |
| **ChatIntegration.Internal** | gRPC-мост к Python-чату | gRPC | — |
| **Admin / GlobalAdmin / Moderation / Dev** | Админка поддержки, админка по всем окружениям, модерация, читы для QA | HTTP | Security DB / своя |

### Общие библиотеки

- **GR.Platform** — фундамент:
  - NATS;
  - базовый ECS (`Ecs/EntitiesCacheLoop.cs`);
  - инфраструктура операций (`IOpHandler`, реестр типов);
  - WS-прокси `RealmProxy`;
  - основа для воркеров.
- **GR.Platform.Features** — шаблон обработчика операции `StandardOpHandler`, эндпоинты авторизации, middleware.
- **GR.WO.Components** — ядро домена:
  - все операции `Ops/*` (≈240 клиентских);
  - статика и условия;
  - сборщики MV;
  - сообщения клиенту;
  - сервисы королевств;
  - `SupportedOpType`.
- **GR.WO.Map.Components** — ECS-компоненты карты, бинарные пакеты для клиента, outbox из карты в город (CitySync).
- **`*.Entities` / `*.Contracts`** — EF-модели баз и gRPC/proto-контракты.

### Ключевые механизмы

**Как исполняется клиентская операция.**

1. Клиент отправляет пачку `{ops[], revision}` на `town/ops` (`GR.WO.Town.Service/Controllers/TownController.cs`).
2. Town.Service передаёт её в грейн игрока: `IDynUserClientGrain.ExecuteOpsAsync`.
3. Грейн прогоняет каждую операцию через `StandardOpHandler`: проверка, исполнение, цены и награды.
4. После каждой операции ревизия растёт на 1 (`OpsContext.UpdateRevision`).
5. Всё пишется одной транзакцией: блоб стейта в MemoryPack плюс строки outbox.
6. Клиент получает новую ревизию и список изменений `changes[]`. Пачка применяется целиком или никак.

**Грейн игрока — единственный writer.** На каждый `DynUserId` в кластере королевства живёт одна активация `DynUserGrain` (`GR.WO.Town.GrainHost/Grains/DynUserGrain.cs`). Через неё проходят все изменения игрока: HTTP-операции, gRPC-импорт, cron, покупки, админка. Писать в `DynUserStates` напрямую запрещено. Рядом живут вспомогательные грейны:

- `DynUserExportGrain` — экспорт на карту;
- `DynUserMvGrain` — сборка MV профиля;
- `DynUserAllianceDiffGrain` — раздача диффа альянса участникам.

**Карта.** Map.Service держит всё королевство в памяти и крутит два цикла:

- расчётный — десятки симуляций: движение, взаимодействия, бой и т.д.;
- цикл записи — пачками в Map DB через Npgsql, без EF.

Команды от других сервисов приходят по gRPC в `InternalCommandHandlers`. Клиенты подключаются через Map.ProxyService. Прокси держит одно соединение на королевство и раздаёт дельты по спискам подписчиков. Токен клиента прокси проверяет сам.

**Синхронизация карты и города.** Направлений три, и механизмы у них разные:

- **Карта → город, результаты сценариев** (марш завершён, лут, слияние). Строка-outbox пишется в Map DB той же транзакцией, что и стейт карты. Затем `*ExportWorker` в Map.Workers зовёт `Town.Service.Internal` по gRPC и после успеха удаляет строку. Доставка — как минимум один раз.
- **Глобальные события** (раннее завершение ивента, синхронизация альянса). Работают через транзакционный outbox `DatabusDbMessages`. Дальше `DatabusProduceWorker` отправляет их в NATS JetStream королевства, откуда их читают консьюмеры.
- **Город → карта.** Работает прямым gRPC из `DynUserExportGrain` по отложенным таймерам (pending timers). Результат экспорта применяется к стейту игрока. Прежняя схема через NATS выпилена.

**Сигналы клиенту («перезапроси»).** Используются каналы NATS `user-partial.{id}`, `profile-mv-update.{id}` и `alliance-mv-update.{id}`. Данных в сигнале нет, клиент по нему сам делает `getPartial`. Сигнал отправляется только после коммита и без гарантии доставки: потерянный сигнал подберёт следующий обычный запрос.

**Статика на сервере.**

- На старте сервис блокирующе получает версию из Update.Service и скачивает JSON с CDN.
- `DetectChangedStaticsWorker` (из `GR.WO.Components`) подключён почти в каждом сервисе. Он следит за сменой версии и перезагружает статику на лету или перезапускает сервис.
- A/B-группа игрока выбирается по `DynUser.AbGroup`.

---

## 4. Клиент (`client-unity-N/Assets/Sources`)

### Сборки и зависимости

```
Framework (GG.*)  ←  Models  ←  Main / City / Map / MapService / MiniGames
                                  ↑ туда же вливаются слои каждой фичи из Features/
```

| Модуль | Что внутри |
|---|---|
| **Entry** (без asmdef) | `EntrySceneHandler`: поднять Addressables и перейти в `MainScene`. Больше ничего: прогрев и сервисы — узлами графа загрузки |
| **Framework / GG.\*** | Ядро без игровой логики. Сборки: `Commands` (команды + `Performer`), `Operations` (пачки, ревизия, мапперы, бинарный контракт профиля), `Networking` (HTTP, WS, NATS), `Windows`, `Views`, `Assets` (обёртка Addressables), `Conditions` (интерпретатор условий из статики), `Ecs` (свой лёгкий ECS, не DOTS), `Statics`, `Scenes`, `Analytics`, `Localization`, `Audio`, `Bootstrap` |
| **Models** | ~50 `Dyn*`-сущностей игрока, `UserEntitiesModel`, мапперы серверных данных |
| **Main** | Запуск (`LaunchCommand` + граф загрузки), глобальные сервисы и окна, туториал |
| **City** | Сцена города: слоты зданий, `BuildingViewController`, радиальное меню, клик, камера |
| **Map** (+ `Map.Ecs.Burst`, `Map.Marches.Brg`, `Map.Sdf.Burst`) | Сцена карты: клиентский ECS-мир, марши, королевства, рельеф. Тяжёлые расчёты вынесены в Burst-сборки, марши рисуются через BatchRendererGroup |
| **MapService** (+ `.Ecs.Burst`) | Транспорт карты: WebSocket, чанки, бинарный протокол пакетов, кеш |
| **Features/** (128 фич) | Каждая фича разложена по слоям: `Models/`, `Main/`, `City/`, `Map/`, `MapService/`, `Statics/`, `Analytics/`. Своих сборок у фич нет: каждая подпапка через `.asmref` вливается в сборку своего слоя. Шаблон — `Features/_Template` |
| **Statics** | ~270 типизированных обёрток таблиц (`StaticXxx`, `ConfigXxx`) и `ParseStatic*Command` |
| **MiniGames** | Арена, Battler, FruitNinja, ZombieCrusher, Wall и др., у каждой своё ядро |
| **Cutscene, Audio, FX, Spine, Video** | Катсцены и медиа |
| **Dev, MapEditor, Editor** | Читы, редактор карты, инструменты редактора |
| **TestBridge (+ GameState)** | WS-канал к game-mcp для e2e и зонды состояния игры (только чтение). Изолированы от игровых сборок |

### DI (VContainer)

```
ProjectRootScope (Framework + Main + Models)
  └─ MainScene (живёт всю сессию)
       ├─ City scope      ┐
       ├─ Map scope       ├─ активна ровно одна
       └─ MiniGame scope  ┘
```

Каждая сцена находит свой `SceneScope`, и он становится родителем следующей сцены (`SceneLoader`). Фичи регистрируются через инсталлеры и partial-части общих фабрик `MainSingleFactory` / `CitySingleFactory` / `ScopedSingleFactory`. Так фичу можно зарегистрировать, не правя общий файл.

### Ключевые механизмы

**Запуск — граф, а не цепочка.** Граф собирается в `Main/Loading/Graph/LoadGraphFactory.cs`: ~22 узла, зависимости между ними заданы в одном месте. Две ветки идут параллельно:

- статика: `UpdateStatics → ParseConfigs → Localization → ParseAllStatics → StaticsReady`;
- авторизация: `Auth → CleanUserState → ThirdParties → OutboxInit`.

Ветки сходятся на `MapUserState`: стейт игрока раскладывается по моделям, только когда статика готова. `MapSessionClaim` обязан пройти раньше `OutboxInit` и `FetchUserState`, иначе ревизии разойдутся и сервер ответит 409.

**Три вида команд, все выполняются через `IPerformer`:**

| Вид | База | Что делает | Вложенность |
|---|---|---|---|
| Игровая | `AbstractSyncCommand` / `AbstractAsyncCommand` | Меняет клиентские модели | Можно вкладывать игровые команды |
| Операция | `OperationCommand<TOpParams>` | Предсказание + операция в пачку | Вызов из игровой команды; не больше одной операции на стек вызовов |
| HTTP | `RequestCommand` | Запрос JSON-RPC | Только прямой вызов |

**Жизнь операции на клиенте** (`Framework/Operations/Handlers/OperationCommandExecutor.cs`):

1. `HandleValidate` проверяет, можно ли выполнить действие.
2. `HandleExecute` сразу меняет модели, и UI обновляется до ответа сервера.
3. Ревизия растёт на 1, операция уходит в `OperationsPackController`.
4. `HandleApply`.

В `Execute` нельзя бросать исключения и синхронно рассылать события моделей. События откладываются через `InvokeYield`. Пачки уходят на `town/ops` и повторяются, пока сервер не подтвердит. На 409 ставится флаг конфликта, и дальше отправка заблокирована до перезапуска с `getCurrent`.

**Данные игрока.**

- `getCurrent` → `MapUserStateCommand`: прогоняет ~50 реализаций `IUserDataMapper.Setup`, затем вызывает `IUserDataInitializer`.
- `getPartialState` → `SetupPartial`: в запросе передаются id отслеживаемых сущностей, инициализаторы не зовутся.
- Мапперы только переносят данные в модели. Отсутствие данных в partial-ответе не значит «удалить».

**Карта на клиенте.** Цепочка обработки данных:

1. `MapWebSocket` получает бинарные фреймы.
2. `MapEntitiesUpdater` разбирает их через `IComponentPacketHandler` каждого компонента.
3. Результат попадает в очередь синхронизации.
4. `EcsMapEntitySyncController` пишет в клиентский `EcsWorld`.
5. Системы и вьюхи рисуют мир.

**Город.** Статика слотов города определяет, что где стоит. По ней `BuildingViewControllerFactory` выбирает один из 21 типа `BuildingControllerName` и создаёт `BuildingViewController<TView>`. Клик обрабатывают `CityClickController` и `CityClickCommand`.

**Окна.** Окно — пара `WindowController<TWindow, TView>` и View. View грузится из Addressables и освобождается вместе со скоупом. Показ идёт через очередь с приоритетами.

**Условия и триггеры.**

- `condition` из статики — это гейт видимости и активности, и он источник истины.
- Триггер (`IClientTriggers`, чисто клиентская шина событий) — только сигнал «переоцени условие».
- Без условия сущность не покажется игроку, зашедшему после события.

**Статика на клиенте.**

1. `update.getVersions` отдаёт хеш статики.
2. Если хеш совпал с локальным, клиент берёт снимок `statics_binary/statics.bin`, иначе качает `statics.zip` с CDN.
3. `ParseAllStatics` параллельно разбирает таблицы в `StaticsContainer`.

---

## 5. Статика: от таблицы до игры

```
Google Sheets (Манифест + таблицы фич)
   → StaticUtils (маппер на каждую фичу → SQL) → Postgres статики: БД gd (рабочая)
   → TeamCity «BuildAndDeployStatics»: копия gd → v<N>-<env> → statics.json/zip на CDN
   → Update.Service: версия + хеш → клиент и сервер берут одну и ту же версию
preupdate: фиксация v<N>-preupdate (отдельная папка таблиц, ветка мапперов) → из неё v<N>-prod
```

- SQL-патчи к статике делаются только скиллами `sql-patch` и `apply-sql-patch`, после патча статику передеплоить.
- MCP-инструменты `statics_*` видят только `gd` и центральные версии. Снимок окружения (`v<N>-<env>`) читается с CDN через `statics-explorer`.
- Проверить клиентское условие быстро можно подменой локального архива статики (`.claude/rules/local-statics-override.md`).

---

## 6. Чат (`chat/`)

Три сервиса на Python: `structure_service`, `message_service`, `tasks_service`. Развёрнуты отдельно на каждое королевство.

- Клиент ходит в `structure_service` по HTTP с JWT.
- Между собой сервисы общаются по gRPC, контракты — в `chat/protobuf/protos`.
- Хранилище — Postgres.
- Автоперевод и лайки идут через NATS.

С игрой чат связывают два встречных gRPC-контракта:

- **игра → чат**: `ChatBackendIntegrationService`, реализован в `tasks_service`;
- **чат → игра**: `GameBackendIntegrationService`, его обслуживает `GR.WO.ChatIntegration.Service.Internal`.

---

## 7. Хранилища

| База | Где | Кто пишет | Что лежит |
|---|---|---|---|
| Town DB | своя на realm | только Town.GrainHost | `DynUserStates` (MemoryPack-блоб), индексы, outbox, служебные таблицы Orleans |
| Map DB | своя на realm | Map.Service | Снимок ECS, outbox-таблицы сценариев |
| Config DB | общая | Update.Service, Config.Workers | Королевства, версии клиента и статики |
| Security DB | общая | Auth, Admin | Пользователи, сессии, права |
| MV DB | общая | Mv.Workers, DynUserMvGrain | `ProfileMv`, `AllianceMv`, лидерборды |
| MapCoordinator DB | общая | MapCoordinator | Глобальные рейтинги, группы королевств |
| Finance / Moderation / Push / чат | общие | свои сервисы | — |
| ClickHouse | общая | Analytics.Store | Аналитические факты |
| Postgres статики | отдельный хост | StaticUtils | `gd`, `v<N>`, `v<N>-<env>` |

Redis в проекте не используется.

---

## 8. Окружения, ветки, миграции

- **Окружения:**
  - `squad-1..15` и `squad-gd` — дев-стенды, по одной ноде на сквад;
  - `preprod` и `preupdate` — на выделенных нодах;
  - `prod` — отдельный контур.
- **Деплой** — только через TeamCity (`BuildAndUpdate`, `OneServiceBuildAndUpdate`, `InstallSquadEnvParallel`, `DeleteNamespace`). Helm-чарты лежат в `wo-k8s`.
- **Ветки:**
  - задача работает в ветке `feature/WO-XXXX-<slug>`, целевая ветка — `preprod` или `preupdate`;
  - багфикс вливает AI-ревьюер, фичу — QA после апрува продюсера (`.claude/rules/jira-delivery-flow.md`).
- **Миграции** — golang-migrate, только вперёд, у каждой базы своя папка. Главное правило: timestamp новой миграции должен быть строго больше максимума в транке. Миграция с меньшим timestamp молча пропускается. Проверка — `check_migration_order`, подробности — `SPEC_24_migration_order_guard.md`.

---

## 9. Инструменты вокруг

- **game-mcp + TestBridge.** Агент играет в живой клиент через WS-мост в самом клиенте. Сценарии лежат в `e2e/scenarios`, раннер без LLM выдаёт отчёт в JUnit.
- **RAG knowledge.** Документы `docs/knowledge/*.md` векторизованы в Qdrant, поиск — `search_context`. После merge в `preprod` документы перегенерируются автоматически.
- **`.claude/`.** ~18 агентов (backend-dev, client-dev, statics-dev, расследователи, советники консилиума, qa-bot) и ~100 скиллов. Работа над задачей начинается только через `/work`.

---

## 10. Куда смотреть дальше

- Бэкенд подробно — `docs/specs/architecture/backend/SPEC_01..24`. Главные: 02 (сервисы), 06 (операции), 10 (синхронизация карты и города), 19 (Orleans), 23 (MemoryPack).
- Клиент — `docs/knowledge/architecture/client/overview.md`, `docs/specs/architecture/client/`.
- Отдельные сервисы и механики — `docs/knowledge/services/*.md`, `docs/knowledge/mechanics/*.md`.
- Правила, которые нельзя нарушать, — `docs/specs/constitution/`.

**Где документация расходится с кодом (верить коду):**

- **Город → карта.** `SPEC_22` описывает экспорт через NATS, в коде это прямой gRPC из `DynUserExportGrain`. `SPEC_02` описывает верно.
- **Прокси карты.** Принцип `map-reads-client-websocket` называет соединение прокси с Map.Service «upstream-gRPC». В коде `RealmProxy` держит WebSocket (`WebSocketConnectionUri`).
- **Сервисы в `SPEC_03`.** Там указаны «Message.Service» и «Chat.Service», в решении их нет. Почта — это Notificator, чат — отдельный Python-стек.
- **Статика в `statics-versioning.md`.** Документ говорит, что Town и Map читают статику из Config DB. На деле её берут с CDN по версии из Update.Service.
- **Имя образа `city-workers`** — наследие: внутри запускается `GR.WO.Town.Workers`.
