# Харнес вокруг AI-агентов — как устроено

Документ описывает всё, что окружает агентов на проекте. Это что они читают, какие правила соблюдают, какими обёртками пользуются, откуда берут знания, кто и как проверяет их работу.

Харнес состоит из трёх слоёв:

1. **Локальный харнес в репозитории.** Инструкции, правила, конституция, агенты, скиллы, хуки и state-файлы управляют Claude Code (и Codex) на машине разработчика.
2. **RAG (база знаний).** Код описан markdown-документами, документы векторизованы, агенты ищут по ним через MCP. После merge кода документы перегенерирует фабрика.
3. **Серверный конвейер и проверки.** Робот автоспецификации задач, Task Forge, AI-ревьюер MR, шлюз `mcp-tools` с правами, Discord-бот.

> Код серверных сервисов (`mcp/*`, `ai-reviewer`) лежит в отдельных репозиториях, локально его нет. Всё про их внутренности описано **по документации** (`docs/knowledge/services/*`, `docs/specs/architecture/*`, `docs/specs/features/WO-12070-ai-mr-reviewer/`). Локальные файлы, git-история и живые вызовы MCP сверены напрямую.

---

## 0. Общая картина

```
 Разработчик ──► Claude Code / Codex
                   │  всегда в контексте: CLAUDE.md + индекс конституции + .claude/rules/* + CLAUDE.local.md + MEMORY.md
                   │  по требованию:     SKILL.md, references/, тела агентов, файлы принципов, .specify/
                   │  хуки:              .claude/settings.json → .claude/hooks/*
                   ▼
            /work (единственная точка входа в задачу)
                   │ сабагенты: backend-dev, client-dev, statics-dev, advisor-*, qa-bot…
                   │ скиллы-инструменты: branch-repos, gitlab-mr, deploy, jira-report, consilium…
                   ▼
   MCP-серверы ─── mcp-tools (Jira, GitLab, TeamCity, k8s, БД, статика, RAG) · game-mcp · UnityMCP · discord
                   │
   RAG ◄────────── search_context / get_feature_flow / get_related_docs ── Qdrant ◄── фабрика knowledge
                   │
   Сервер ──────── робот автоспецификации → Jira;   MR → AI-ревьюер → автомерж / QA;   Discord-бот
```

---

## 1. Слой инструкций

### Что грузится в контекст всегда

| Что | Где | Роль |
|---|---|---|
| `CLAUDE.md` | корень | Глоссарий, обязательный RAG, стиль ответов, ~20 проектных правил, маршрутизация по агентам |
| Индекс конституции | `@docs/specs/constitution/_constitution.md` (импорт из `CLAUDE.md`) | Только хуки-указатели на 28 принципов, самих норм нет |
| Правила | `.claude/rules/*.md` (13 файлов) | Грузятся безусловно: frontmatter `paths:` нет ни у одного |
| Личные инструкции | `CLAUDE.local.md` (в `.gitignore`) | Карта рабочих копий клиента, где секреты (`.env`), python для Sheets |
| Память агента | `~/.claude/projects/<проект>/memory/MEMORY.md` | Индекс записей: по одной строке на запись |
| Описания скиллов и агентов | frontmatter `description` | По ним модель решает, что вызвать |

### Что грузится по требованию

- тело `SKILL.md` — при вызове скилла;
- его `references/` — по фазам («читать под конкретную работу»);
- тело агента — при запуске сабагента;
- файлы принципов конституции — только через `Read`;
- шаблоны spec-kit из `.specify/`.

### Зеркала для других агентов

- **Codex:**
  - `AGENTS.md` состоит из одной строки и отсылает к `.codex/CODEX.md`;
  - источник истины — `.claude/`;
  - `.codex/{agents,commands,skills}` — прокси на 3–5 строк («Proxy for `.claude/...`, прочитай оригинал»).
- **Codex-плагин:** `.agents/plugins/marketplace.json` → `plugins/vibecode-workflows/` (одна команда `/bugfix`), плюс `.agents/skills/bugfix`.
- **Copilot:** `.github/agents`, `.github/prompts` — копии spec-kit.

---

## 2. Правила

### Проектные правила из `CLAUDE.md` (главные)

| Правило | Суть |
|---|---|
| Работа над задачей | Только через `/work`. 10 прежних точек входа превращены в указатели |
| Фикс первопричины | Нельзя глушить ошибку, ослаблять валидацию, расширять допустимые состояния, ретраить детерминированную ошибку. Если костыль дешевле — показать оба варианта, решает человек |
| SQL-патчи к статике | Только скиллы `sql-patch` и `apply-sql-patch` (идемпотентность, GRANT, автокоммит) |
| Номер операции (opType) | Только через Confluence-реестр: номер бронируется строкой сразу при выборе |
| Статика: condition и trigger | `condition` — гейт и источник истины; `trigger` — только сигнал переоценить условие |
| Статика окружения | Упор MCP ≠ «недоступно»: есть CDN через `statics-explorer` и `db_helper` |
| Проверка клиентского условия | Подменой локального архива статики, а не полным циклом деплоя |
| Jira | Заголовок из 2–3 слов с префиксом области; суть сверху, детали под `<details>`; личный OAuth через `/jira-link` |
| Discord | Ссылки на задачу и MR — только ссылкой с заголовком внутри |
| Миграции | Timestamp строго выше максимума в транке, иначе rebump |
| Бинарный контракт профиля | Только дописывание, в обоих репозиториях |
| Компиляция клиента | Проверяет только Unity; `dotnet build` — не гейт |
| Кроссплатформенность | Windows и macOS, никаких путей с юзернеймом |
| Google Sheets | Только ADC под корпоративным OAuth-клиентом; service account не использовать |
| Публикация страниц | По умолчанию `/publish-page` (GitLab Pages), а не артефакты |
| Внешний ревьюер | Работу Claude проверяет Codex |

### Конституция (`docs/specs/constitution/`)

- **Устройство.** Индекс `_constitution.md` плюс 28 файлов-принципов в стиле obsidian. Принципы собраны в группы: базовая модель, команды, загрузка данных, статика и эффекты, сцены клиента, ассеты, чтение карты, комментарии. В каждом файле frontmatter, «Непоколебимые правила», «Почему» и «Связано» с `[[ссылками]]`.
- **Строка индекса — это хук, а не норма.** Сама норма, её оговорки и контрпримеры есть только в файле принципа.
- **Правило «сослаться ≠ свериться»** (`CLAUDE.md`). Упомянул `[[принцип]]` в обосновании — в этой сессии должен быть `Read` его файла. Без такого следа запрещено писать «гейт пройден» и «конституции соответствует».
- **Как исполняется:**
  - исполнители (`backend-dev`, `client-dev`, `statics-dev`) соблюдают её целиком без напоминаний;
  - страж `advisor-constitution` (opus, только чтение) читает весь каталог; его замечание действительно, только если в нём есть файл принципа, дословная цитата нормы и `file:line`; вердикты `VIOLATION` / `CLEAR` / `N/A`; `CLEAR` без списка прочитанного недействителен;
  - `/consilium` зовёт стража в каждом прогоне;
  - оркестратор сам сверяет дифф сабагента с конституцией перед MR.
- **Правка** — скилл `update-constitution`: факты подтверждает человек, в индекс пишется хук, а не норма.

### `.claude/rules/` — поведенческие правила

| Правило | Когда срабатывает | Что требует |
|---|---|---|
| `jira-task-claim` | узнал ключ задачи, которую берёшь | Сразу `In Progress` и себя исполнителем — до вопросов и ветвления. Не вышло — сказать вслух |
| `one-task-one-branch` | перед ветвлением | `git ls-remote '*WO-XXXX*'` и переиспользовать ветку. Лишние удалить, записав sha. Законны только `-preupdate` и ветка подзадачи |
| `on-git-branch-creation` | ветка создана | Один комментарий в Jira «Ветка → target, репо» и `branchReportedAt` в state |
| `on-git-branch-merge` | MR влит | Комментарий «Смержено в…, MR…, будет в сборках…» |
| `on-handoff-jira-task` | перевод в InReview / Ready To QA | Обязательный комментарий: сделано / как проверить / ограничения |
| `jira-delivery-flow` | любое движение задачи | Ветка `feature/WO-XXXX-slug`. Баг вливает AI-ревьюер (обе линии), фичу — QA. Отладка ≠ Баг. Назад по флоу не двигать |
| `subagent-delegation` | запуск сабагента | Дать точные якоря `file:line`. Сверку диффа с конституцией оркестратор делает сам |
| `unity-headless-build` | правка клиента, подтяжка транка | Гейт — Unity (MCP или `-batchmode`). Успех = код возврата 0 и ни одной строки `error CS`. `.meta` — в тот же коммит |
| `cross-platform` | новый скрипт или скилл | Windows и macOS, пути от корня репозитория, ветвление по ОС |
| `binary-contract` | правка контракта профиля | Только дописывание, обе копии. Упавший тест = откат правки |
| `local-statics-override` | проверка клиентского `condition` | Подменить `statics.zip` и подогнать хеш |
| `on-client-log-question` | вопрос о клиентской сессии | Звать `client-logs-investigator`, самому не грепать |
| `discord-task-links` | пост с задачей или MR | Ссылка с заголовком внутри, голые URL запрещены |

---

## 3. Агенты (`.claude/agents/`, 18 штук)

| Группа | Агент | Модель | Инструменты и жёсткие правила |
|---|---|---|---|
| **Исполнители** | `backend-dev`, `client-dev`, `statics-dev` | наследуется | Все инструменты. RAG первым шагом, конституция целиком, фикс первопричины. Клиенту — `/create-window`, гейт Unity, ast-index. Бэкенду — C# LSP |
| **Расследователи** | `feature-explorer` | sonnet | Только чтение. Начинает с `get_feature_flow`, ответ в 4 блоках |
| | `client-logs-investigator` | sonnet | Только чтение журналов клиента. Знает, что тела запросов лежат только в `artifacts_*.log` |
| | `bestprictice-finder` | haiku | Ищет прецеденты в `docs/specs/features\|fixes` |
| | `feature-fix-documentor` | haiku | Пишет итоговую документацию фикса или фичи |
| **Консилиум** | `advisor-constitution` | opus | Страж конституции, только чтение |
| | `advisor-skeptic` / `-engineer` / `-pragmatist` / `-user-advocate` / `-visionary` | sonnet | Только чтение: RAG, Jira, `git log/show/diff` |
| | `gd-bot-user-advocate`, `gd-bot-ux-specialist` | sonnet | Оценивают сыгранную сессию для `/gd-bot` |
| **QA / e2e** | `qa-bot` | sonnet | Единственный, кто работает с game-mcp. Только легитимные жесты игрока, адресация по DOM сцены. Пишет экшены и сценарии |
| | `e2e-hybrid` | sonnet | Гонит сценарий раннером без LLM. На затыке включает живой explore и кодифицирует пройденное |
| **Оркестратор** | `task-producer` | opus | Только Agent / SendMessage / Task* / AskUserQuestion. Сам код не читает и не пишет |

---

## 4. Скиллы (`.claude/skills/`, 96 папок)

| Группа | Состав |
|---|---|
| **Точка входа** | `work`. Указатели «НЕ ТОЧКА ВХОДА»: dev-workflow, dev-workflow-lite, dev-workflow-auto, bugfix, bugfix-preprod, bugfix-preupdate, batchfix, fix409, next-task, forge-triage |
| **Git, MR, поставка** | branch-repos, commit-work-result, gitlab-mr, merge, mr-review-post, ai-review-controller, already-fixed, patch-notes |
| **Jira и коммуникация** | jira-report, jira-link, jira-assets, jira-media, daily-report, estimate-task, ai-improvement-request, absence-request, meeting-followups-team, publish-page, skill-presentation, online-report |
| **Кодогенерация** | create-operation, create-condition, create-static, create-window, prefab-builder, ui-toolkit-editor, contract-check, asset-lock |
| **Статика и таблицы** | sql-patch, apply-sql-patch, create-mapper, create-control-table, statics-explorer, statics-sql-review, preupdate-static-diff, delete-config-table, edit-table-script, google-sheets(-install), update-terrain |
| **Окружения и инфра** | deploy, wipe-squad, fix-squad, squad-manager, squad-occupancy, squad-sync, kube-pf, infra-health-medic, mcp-admin, global-admin, bot-stress-test, install-ipa, test-stage |
| **Диагностика** | db-investigator, seq-investigator, teamcity-investigator, user-logs-investigator, bugreport-analyzer, api-explorer, get-user, player-state-dump, dynuser-state-editor, memory-profiler, rlm-recon, video-analyze, watch-video |
| **E2E и игра ботом** | e2e-game-play, e2e-create-scenario, e2e-create-mechanic-driver, e2e-scenario-review, e2e-artifacts, e2e-bugreport, e2e-docs-sync, gd-bot, gd-bot-autonomous, gd-bot-report |
| **Качество и мета** | consilium, bestpractice-gate, skill-review, update-constitution, auto-spec, add-client-repo, add-statics-repo |

### `/work` — единственная точка входа (`.claude/skills/work/SKILL.md` + `references/`)

Граница: точка входа отвечает на вопрос «что мы сейчас делаем», скилл-инструмент — на вопрос «чем это сделать».

**Вызов:**

```
/work [WO-XXXX] [client-N] [squad-N] [preprod|preupdate] [depth=auto|lite|full] [gates=plan,impl,check,mr|none] | status | list
```

Без ключа задача берётся из конвейера: сначала с меткой `ai-spec-ready`, иначе верхняя To Do. Повторный запуск продолжает прогон с фазы, записанной в state.

**Вопросы человеку — закрытый список:**
- стартовый вопрос о гейтах: 4 готовых набора, результат пишется в `humanGates{plan, impl, check, mr}`;
- неоднозначное окружение;
- нет свободной копии клиента или памяти под редактор;
- эскалация, если совет не сошёлся;
- выбранные человеком гейты;
- защитные гейты нижележащих скиллов;
- ручной запуск Unity.

Всё остальное агент решает сам.

**Фазы:**

| Фаза | Что происходит |
|---|---|
| **0. Интейк** | Транзит в `In Progress` и спринт, стартовый вопрос о гейтах. Детекторы: режим (feature / bugfix / subtask / batch), глубина (auto / lite / full), окружение (preprod / preupdate, с git-проверкой), scope через RAG (client / backend / statics / full). Сквад занимается в фоне через `/wipe-squad`, редактор — при клиентском scope. Ветвление по `/branch-repos`, запись state |
| **Совет-ревью** | Заменяет аппрув человека на невыбранных гейтах. Обязательны skeptic и engineer, остальные — по ситуации. Обязательные вопросы: критерий приёмки, чем мерить, кто ещё пишет это состояние, когда объект умирает. До 3 итераций на артефакт |
| **1. Спека** | Фича: `/speckit.specify` и проверка конфликтов с чужими спеками. Баг: сессия в `docs/specs/fixes/` с гипотезами первопричины (`references/bug-analysis.md`) |
| **2. План** | `/speckit.plan`, отчёт `/jira-report --mode plan\|bug` |
| **3. Реализация** | `/speckit.tasks` → `/speckit.implement`. Код правит только профильный сабагент, при scope full — параллельно по репозиториям. Гейт компиляции, `/statics-sql-review`, обязательный «красный прогон» теста на коде без фикса. Затем `/code-review`, сверка с конституцией, гигиена комментариев, `/commit-work-result`, деплой на сквад. Потолок — 5 кругов |
| **3а. Бот-тест** | `qa-bot` через game-mcp |
| **3б. Ресурсы** | Параллельно можно гнать чтение и работу в разных репозиториях. Одиночные ресурсы делить нельзя: копия клиента, редактор, сквад, ветка, транзиты Jira. `task-producer` — только для full, batch и независимых гипотез |
| **4. Проверка и сдача** | Гейт проверки требует назвать различающее предусловие. `deliveryPath`: `subtask-merge` / `reviewer-merge` / `qa-merge`. Подтяжка ветки до сборок, билды Android и iOS, `/gitlab-mr create`. Проверенный баг — проследить MR до влития и закрыть `DONE`. Фичу самому не мержить |
| **4.4. Ретро** | Правки `SKILL.md` и записи в память — только с одобрения человека |

**Справочники в `references/`:**
- `pipeline-full`, `pipeline-lite`;
- `bug-analysis` (+ `-upgrade`);
- `env-prep`, `bot-test`, `state`, `postmortems`;
- `intake-conveyor`, `intake-defer`;
- `playbooks/{env-preprod, env-preupdate, batch, 409}` со скриптами `find_op.py`, `find_409.py`, `fetch_attachment.py`.

### Spec-kit и прочие обёртки

- **Spec-kit.**
  - Команды `.claude/commands/speckit.*` — 9 команд: specify, clarify, plan, tasks, implement, analyze, checklist, constitution, taskstoissues.
  - Шаблоны — `.specify/templates/*`; `spec-template` использует и робот автоспецификации.
  - Скрипт `create-new-feature` пишет в `docs/specs/{features|fixes}/WO-XXXX-slug/`.
- **`/bestpractice`** — документ best practice плюс ссылки в `CLAUDE.md` и в память.
- **`/skills-wiki`** — `.claude/skills-wiki/generate.py` собирает HTML-навигатор по скиллам из frontmatter и `meta.json`.
- **`.claude/workflows/impl-test-pipeline/impl-test-pipeline.js`** — сценарий инструмента Workflow для конвейера Task Forge (см. §9).
- **`.claude/bugfix-knowledge.md`** — накопительная база фактов из bugfix-сессий.
- **`.claude/docs/`** — справочники по промптингу.

---

## 5. Хуки и права (`.claude/settings.json`)

| Событие | Скрипт | Что делает | Блокирует |
|---|---|---|---|
| PreToolUse: Bash / PowerShell | `git-commit-heredoc-guard.sh` | Видит `git` + `commit` + `@'` и запрещает вызов: сообщение коммита — только через файл и `git commit -F` | **да** |
| PostToolUse: Bash / PowerShell | `migration-order-guard.sh` | Видит `git merge/rebase` транка с `backend-migrations` и подсказывает вызвать `check_migration_order` и сделать rebump | нет, подсказка в контекст |
| SessionStart | `name_reminder.sh` → `.py` | Если в `git user.name` нет кириллицы — советует исправить | нет |
| PostToolUse[Skill], PostToolUseFailure[Skill], UserPromptSubmit, Stop (async) | `skill_tracker.sh` → `.js` / `.py` | Телеметрия в `mcp-tools.lastoasisgame.com/skill-leaderboard/ingest`: git email, скилл, кто вызвал, исход. Недоставленное ждёт в `~/.claude/skill_leaderboard_spool.jsonl` | нет |
| не подключены | `session_title.*`, `game_mcp_up.*` | Заголовок сессии «WO-XXXX summary»; ручной подъём стека game-mcp | — |

**Права.**
- Allow-лист почти всё открывает: `Bash(*)`, `PowerShell(*)`, `Read/Write/Edit(//**)`, `Skill(*)`, `WebFetch`, `WebSearch`, MCP-серверы по маске. Правил `ask` и `deny` нет.
- Защиту держат один блокирующий хук, текст правил и защиты на стороне серверов (§7).
- `settings.local.json` — личные доп. разрешения (плагин atlassian, IDE-диагностика).

---

## 6. Состояние и память

- **State-файл задачи.** `workflow-states/WO-XXXX.json`: фаза, режим, окружение, scope, ветки, копия клиента, сквад, `humanGates`, `implRounds`, `deliveryPath`, `branchReportedAt`, решения совета. Схема описана в `references/state.md`, на практике поля плавают. Мест хранения два:
  - `.claude/skills/work/workflow-states/` — в git, 109 файлов;
  - корневой `workflow-states/` — в `.gitignore`, 47 файлов.

  Читают и пишут state: work, branch-repos, commit-work-result, gitlab-mr, jira-report, contract-check, mr-review-post, patch-notes, squad-sync.
- **Курсор `jira-report`.** `.claude/skills/jira-report/state/WO-XXXX.json` вида `{lastReportedAt, cursors{repo: sha}}`, помнит, что уже отписано. Тумблер авто-отправки — `state/config.json`, свой на каждой машине. Авто-трек после `[WO-XXXX]`-коммита запускает `commit-work-result`, отключается флагом `reportOptOut`.
- **Память агента.** Файлы с frontmatter (`name`, `description`, `metadata.type`: user / feedback / project / reference) плюс индекс `MEMORY.md`. Личная, в репозиторий не попадает.

---

## 7. MCP-серверы и доступ

**Откуда подключены.** В репозитории `.mcp.json` нет. На этой машине:
- `mcp-tools`, `discord`, `game-mcp`, `sequential-thinking` — на уровне пользователя (`~/.claude.json`);
- `UnityMCP` — на уровне проекта.

Централизованно роли и `.mcp.json` раздаёт `mcp-agents-config-server` из репозитория `backend-vibecode`.

| Сервер | Для чего |
|---|---|
| **mcp-tools** | Единый шлюз. Jira, Confluence, GitLab/git (`git_search_code`, `git_read_file`, MR), TeamCity, k8s и Knowledge Graph (`kg_*`), ClickHouse, игровые БД (`gamedb_*`), статика (`statics_*`), метрики, сквады (`squad_diagnose/fix`), RAG (`search_context`, `get_feature_flow`, `get_related_docs`, `search_metrics_context`), `check_migration_order`, `archive_config_table`, публикация, озвучка |
| **game-mcp** | Управление живым клиентом через TestBridge: DOM сцены, тапы, ожидания, скриншоты, снапшоты |
| **UnityMCP** | Открытый редактор Unity: компиляция, консоль, сцены |
| **discord** | Чтение и публикация в каналы |
| прочие | context7 (документация библиотек), miro, figma, atlassian-плагин, sequential-thinking |

**Авторизация в mcp-tools.**
- Bearer-ключ, права прямые или через группы (`developers`, `ai_agents`, `mcp-admins`), кэш прав 5 минут.
- Ключами, группами и правами управляет скилл `/mcp-admin` (право `mcp-admin:manage`). Каждое изменение пишется в журнал аудита.
- **Jira от своего имени.** Один раз проходишь `/jira-link` (OAuth в браузере). Refresh-токен хранится на сервере зашифрованным. Без привязки jira-инструменты человеку недоступны.
- Боты с правом `jira:service` (группа `ai_agents`) работают под сервисным AI-аккаунтом.

**Защиты на стороне сервера.**
- `gamedb_*` и `statics_*` только читают: `default_transaction_read_only=on`, запись отклоняется даже внутри CTE.
- На проде перед запросом стоит EXPLAIN-страж по стоимости плана, плюс `statement_timeout` и `lock_timeout`.
- Ответы маскируются от персональных данных.
- Пишущие инструменты: `jira_*` на запись, `teamcity_trigger_build`, `squad_fix`, `confluence_update`, `archive_config_table`.

---

## 8. RAG

### Зачем и как течёт

Весь код в контекст не помещается, а без контекста агент выдумывает имена, пути и номера операций. Поэтому код заранее описан документами в `docs/knowledge/` (ветка `preprod` репозитория vibecode). Документы нарезаны на куски и векторизованы. Сам код не векторизуется: его ищут `git_search_code` или локально.

```
merge MR в preprod (backend-services, chat, bots-script, staticsutils, statics-validator,
                    backend-migrations, client-unity; + push в wo/ai-reviewer/main)
  → webhook → mcp-knowledge-generator
      diff (GitLab Compare) → matcher (src_globs из _index.json, cooldown 1 ч)
      → generator (headless `claude -p` + MCP) → 7 валидаторов (до 2 повторов)
      → writer: ветка kb/regen-run-<id> → MR в vibecode/preprod
  → merge → push-webhook → индексатор: нарезка по H2 → OpenAI embeddings → Qdrant `knowledge`
  → search_context / get_feature_flow / get_related_docs
 + дрейф схемы статики: cron каждые 30 мин;   + ночной reconcile 03:00 UTC
```

По git это видно: в `preprod` 51 влитие веток `kb/regen-run-*`, 9 в мае и 42 в сентябре. В июне–августе регенерации не было.

### Слои документов (`docs/knowledge/`)

| Папка | Файлов | Что |
|---|---:|---|
| `features/` | 206 | Фичи |
| `services/` | 50 | Сервисы |
| `mechanics/` | 114 | Сквозные паттерны |
| `statics-tables/` | 360 | Таблицы статики |
| `analytics-tables/` | 463 | Факты ClickHouse |
| `ai-mappers/` | 69 | Мапперы статики |
| `architecture/`, `skills/`, `agents/` | 75 / 96 / 19 | Зеркала `docs/specs/architecture`, `.claude/skills`, `.claude/agents` |

**Формат документа** (`_CONVENTIONS.md`).
- Frontmatter: `layer`, `title`, `services/features/mechanics`, `discovery_source` (`code+confluence` / `code+specs` / `code-only` / `confluence-driven`), `backend_verified` (обязательно), `client_verified`, `src_globs`, `prompt_version`, `last_regenerated(_sha)`, `confluence_pages`, `specs_referenced`.
- Секции идут в строгом порядке по слою. Для фичи: Назначение → Поток данных (mermaid) → Компоненты → Инварианты и валидации → Конфигурация → Источники → Связанные документы.
- Ссылки на код — кликабельные `file#Lnn`.
- Только русский, первый абзац без имён классов, запрещены «было/стало/legacy».

**`_index.json`** (схема `knowledge-index/v1`) — таблица «код → документы» для matcher и reconcile.
- 1190 записей.
- `regen_policy`: `auto` / `human_review` / не задана.
- Зеркала (skills, agents, architecture, ai-mappers) в индекс не входят, и фабрика их не перегенерирует.
- `_validate_index.py` проверяет индекс против диска.

### Фабрика `mcp-knowledge-generator` (по документации)

- **Триггер.**
  - `POST /webhooks/gitlab/merge`: `merge_request` → регенерация;
  - push в `docs/knowledge/**` → только переиндексация;
  - прогон пишется в `/data/runs.jsonl`.
- **Планировщик.**
  - `matcher` сверяет изменённые файлы с `src_globs`;
  - `classifier`: `frozen` пропускается; `human_review`, больше 5 файлов или удалённый файл → HUMAN_REVIEW;
  - дубли отсекаются по `merge_commit_sha`, cooldown — 1 час.
- **Генератор.**
  - `claude -p --append-system-prompt-file --allowedTools --max-turns` с MCP `mcp-tools` / `mcp-context`;
  - локальные клоны в PVC (client-unity — только `Assets/Sources`);
  - в промпт идут шаблон слоя (STRICT RULES из `LESSONS_FROM_BOOTSTRAP.md`), текущий документ, топ-20 файлов кода и фидбэк прошлой попытки.
  - Версии промпта на диске: v1 — 1226 файлов, v2 — 43, v3 — 172.
- **Валидаторы:**

  | Валидатор | Проверка | Режим |
  |---|---|---|
  | `frontmatter_schema` | Enum полей; `confluence-driven` ⇒ `backend_verified=false` и непустые `confluence_pages` | блок |
  | `heading_consistency` | Секции соответствуют слою | блок |
  | `file_refs` | Файл существует, символ резолвится (номер строки не гарантирует) | блок |
  | `cross_refs` | Мёртвые ссылки на документы | блок + автоисправление текста |
  | `duplicate_detection` | Векторный поиск похожего документа | блок |
  | `mermaid_fence` | Диаграммы в fence | автоисправление |
  | `numbers_traceable` | Числа должны браться из кода | предупреждение |

  Смысл текста не проверяется. После двух неудачных повторов документ уходит с меткой `needs-human-review`. Там, где модель не смогла проверить факт, она ставит в тексте маркер `(HUMAN-REVIEW)`, сейчас он есть в 122 файлах.
- **Флаги verified.** `backend_verified` изначально проставлен скриптом `_tools/_apply_backend_verified.py` по эвристике: `confluence-driven` → false, остальное → true. Это не проверка каждого документа.
- **Writer.** Ветка `kb/regen-run-<id>`, MR в `preprod`, для таблиц статики пересчитывается `db_schema_hash`. Уведомления уходят в Discord: `#mcp-knowledge-auto` (дайджест) и `#mcp-knowledge-alerts`.
- **Ночной reconcile.**
  - Сравнивает хеши файла, индекса и Qdrant.
  - Если последний коммит документа сделал человек — только переиндексация, без регенерации.
  - Ищет устаревшие `last_regenerated_sha`, мёртвые ссылки, фичи без реализации, у которых появился код.

### Индексация и поиск

- **Нарезка:** по H2, с H3 в заголовке куска, примерно 2К символов; текст до первого H2 становится куском «Введение».
- **Эмбеддинги:** OpenAI `text-embedding-3-large`, 3072 измерения, только dense. В плане был BGE-M3 с гибридом и реранкером, фактически их нет: инструмент отвечает `reranker_used: false`.
- **Коллекции Qdrant:**
  - `knowledge` — 1174 документа, 7993 куска;
  - `docs` — Confluence, specs, bestpractices;
  - `analytics`.
- **Payload куска:** поля из frontmatter плюс `doc_path`, `doc_id`, `section`, `chunk_index`, `text`, `embedding_model`, `indexed_at`, `lang`.
- **Инструменты:**
  - `search_context(query, sources, limit≤100)` — top-N по косинусу из выбранных коллекций; параметра фильтра по payload у инструмента нет;
  - `get_feature_flow(query, hops)` — поиск плюс обход графа связей;
  - `get_related_docs(doc_path, hops, kinds)` — соседи документа по детерминированному графу (`_tools/_build_graph.py`); рёбра `links_to`, `refers_code`, `uses_mechanic`, `in_service`, `about_feature`, `refers_spec`;
  - `search_metrics_context(fact)` — описание аналитического факта.
- **Кто пользуется:** Claude Code, `feature-explorer`, исполнители, советники, шаг интейка `/work`, Discord-бот, AI-ревьюер (ищет конвенции), робот автоспецификации, сама фабрика.

### Правила использования (`CLAUDE.md`)

- Любой вопрос «как устроено X» начинается с `search_context(sources=["knowledge"])`.
- Оркестратор ищет сам до запуска сабагента и требует в промпте: «начни с `search_context`, потом grep по якорям».
- RAG дополняет код и не заменяет его: найденное проверяется Read/Grep.
- Конституция важнее RAG.

### Качество

- `golden-queries.json` — 60 запросов, всё ещё `draft`, ожидаемые документы не заполнены. Регулярных прогонов нет.
- Аудит дрейфа `RAG_GAP_AUDIT_2026-06-01.md`: 125 подтверждённых пробелов, 49 из них high. Среди них выдуманные идентификаторы в документах с `backend_verified=true`. По итогам написаны документы v2.
- **Слабые места:**
  - нет реранкера и фильтров — отбор релевантного целиком на агенте;
  - смысл текста не проверяется;
  - `backend_verified` проставлен эвристикой;
  - зеркала вне индекса расходятся с оригиналами;
  - все ссылки на клиент ведут в `client-unity/`, а локально копии называются `client-unity-N`.

---

## 9. Конвейер задачи

```
Jira To Do
 → [робот] автоспецификация: комментарий-спека + label ai-spec-ready (задача остаётся в To Do)
 → [человек] /work: забор, In Progress, clarify-сессия → спека в docs/specs/features/… + label ai-spec-clarified
 → реализация: /work локально   или   Task Forge impl-test-pipeline (фоном, ночью)
 → MR → [робот] AI-ревьюер: детерминированные валидаторы → содержательное ревью → замечания / approve / merge
 → баг, проверенный исполнителем: ревьюер вливает (preprod и preupdate), ставит Ready To QA; исполнитель закрывает DONE
   фича: билды, стенд, Ready To QA → QA тестирует и вливает после апрува продюсера
```

Человек участвует в трёх местах:
- уточнение и аппрув спеки;
- разбор отложенных задач (defer);
- приёмка QA и продюсером.

### Автоспецификация

- **Исходно** — k8s-сервис `mcp-preplanner`. Запускался CronJob каждые 15 минут, брал JQL по To Do задачам технических специалистов (ростер из Confluence), максимум 5 задач за прогон.
  - Headless `claude -p` с `ALLOWED_TOOLS` только на чтение: Read/Glob/Grep, `search_context`, `git_*`, `jira_get_issue`, `jira_search`, `statics_*`.
  - Публикует оркестратор, а не агент: markdown → wiki markup, комментарий от бота.
  - Finalization-sweep менял старый комментарий на заглушку и снимал `ai-spec-ready` после `ai-spec-clarified`.
- **Сейчас** сервис отключён (`task-forge/ROADMAP.md`). Его промпт переиспользует локальный демон `autospec_daemon.py` со скиллом `/auto-spec`: на каждую задачу — свежий `claude -p`. Дополнительно пишутся `docs/specs/drafts/<KEY>/` и `e2e/qa-queue/<KEY>.md`.

### Task Forge (`docs/specs/architecture/task-forge/`)

- **Цель:** человек только уточняет и аппрувит спеку.
- **Реализовано (WO-14620)** — двухрежимный контур на машине разработчика:
  - днём — аппрув-сессии;
  - ночью — только одобренные задачи; план утверждает Plan-Council, сомнительное уходит в defer (`ai-needs-human`).
- **Диспетчер** (`SPEC_forge_dispatcher.md`):
  - очередь `queue.json`, ресурсные локи (сквады, копии клиента, один слот Unity), бюджет;
  - исход прогона берётся из следа в журнале, а не из кода возврата.
- **Конвейер** (`SPEC_impl_test_pipeline.md`, сценарий `.claude/workflows/impl-test-pipeline/impl-test-pipeline.js`). Фазы: Spec prep → Plan → Plan-Council → Squad prep → Reproduce (для фичи — DoD-тест с «красным» базлайном) → Implement → Deploy → Verify → Handoff (Draft-MR и один отчёт в Jira) → Release.
  - Проверку ведёт `qa-bot`, который не участвовал в реализации. Вердикт `confirmed` / `retry` / `defer` со сверкой по каждому пункту DoD.
  - Нужны два канала: данные и видео.
  - В MR первой строкой ставится маркер «⚠️ Автопроверка Task Forge — человеком не проверено».
- **План, не сделано:** облачный execution-слой, оркестратор возвратов, фазовая реализация, консилиум по готовому решению перед MR (`SPEC_harness.md`).
- Демоны и данные прогонов (`task-forge/`, `scripts/`, `.task-forge/`) вынесены из-под git. В репозитории остался только сценарий Workflow.

### AI-ревьюер (`docs/knowledge/services/ai-reviewer.md`, спека `docs/specs/features/WO-12070-ai-mr-reviewer/`)

- **Триггер.** Опрос GitLab каждые 30 с. Дедуп по `(project, iid, sha)`. Черновики и нецелевые ветки отсекаются, целевые — `preprod` и `preupdate`. Повторное ревью запускается пушем, ответом в треде или упоминанием бота.
- **Репозитории:** client-unity, backend-services, backend-migrations, wo-k8s, chat.
- **Группа MR по ключу задачи.** Ревьюер собирает все MR задачи из разных репозиториев. Ветка по ключу задачи без MR оставляет группу несобранной: прогон не начинается вовсе, без записи и диагностики, и форс не помогает. Отсюда правило `one-task-one-branch`.
- **Детерминированные валидаторы.** Первый же отказ останавливает прогон до вызова модели.
  - для всех репозиториев: `mr_naming`, `jira_status`, `branch_target_env`, `branch_up_to_date`, `conflict_markers`, `release_bugfix_target`;
  - client-unity: `meta_files`, `meta_guid_churn`, `statics_codegen`, `shader_style`, `texture_size`;
  - backend-migrations: `migration_order`, `migration_empty_statement`.
- **LLM-правила** (`checks/prompts/*`):
  - `logic`, `task-intent`;
  - `contract-sync` — контракты клиент↔сервер; перед тревогой ищет парную сторону в чужих MR;
  - `spec-conformance` — полнота по плану и подзадачам Jira;
  - `loc-key` — ключи локализации сверяются через `statics_query`;
  - `db-migration-sync`, `env-config-sync`.

  Отдельной проверки конституции нет.
- **Блокировка.** Мерж блокируют любой отказ валидатора, любая находка модели и ошибка разбора вердикта. Кнопка merge людям доступна всегда.
- **Повторное ревью.** История идёт по `discussion_id`: исправленное — ответ и resolve; на возражение разработчика — контраргумент в тот же тред.
- **Discord.** Карточка с пингом автора, повторы — в тред под первым сообщением.
- **Эндпоинты:**
  - `/status?limit=N` — прогоны с полями `outcome`, `gate`, `verdict`, `reject_checks`, `actions`; по `actions` видно `merged` и `jira WO-XXXX→Ready To QA`;
  - `/review/{proj}/{iid}` — форс;
  - `/diagnostics/{run_id}` — ходы, токены, цена;
  - `/health`.
- **Управление с машины разработчика** — скилл `ai-review-controller`.

---

## 10. Валидация — все гейты в одном месте

| Где | Гейт | Что проверяет | Как | Блокирует |
|---|---|---|---|---|
| Локально, до вызова | `git-commit-heredoc-guard` | Формат вызова `git commit` | хук | да |
| Локально, после вызова | `migration-order-guard` | Порядок миграций после подтяжки транка | хук | подсказка |
| Внутри сессии | Правило «сослаться ≠ свериться» | Ссылка на принцип подкреплена чтением его файла | дисциплина агента | запрещает формулировку «пройдено» |
| Внутри сессии | **Консилиум** `/consilium` | Любая рекомендация, гипотеза, диагноз, «на кого назначить» | 4–6 агентов параллельно; обязательны skeptic, engineer, страж конституции и quality-gate | одно BLOCK/WORRY с доказательствами — рекомендации нет |
| Внутри консилиума | Страж конституции | Нарушение принципов | цитата нормы + `file:line`, проверка грепом | закрывается только доработкой или эскалацией человеку, не больше 2 повторов |
| Внутри консилиума | **Quality-gate** (`consilium/quality-gate-cascade.md`) | Качество решения по `quality-rubric.md` | Уровень 0 детерминированный: линт миграций, `check_migration_order`, `statics-sql-review`, сборка, `contract-check`, гигиена комментариев. Уровень 1 — LLM-судья на другой модели. На 46 кейсах: 77% дефектов найдено, 0% ложных блоков | `block` → MR не создаётся (`gitlab-mr` §5.1c) |
| `/work` | Совет-ревью | Спека, план, реализация | skeptic + engineer, до 3 итераций | вместо аппрува человека |
| `/work` фаза 3 | «Красный прогон» | Тест действительно ловит дефект | тест на коде без фикса обязан упасть | да |
| `/work` фаза 3 | Сверка диффа сабагента | Конституция, комментарии без `WO-\d+` и «было→стало» | оркестратор сам, грепом | да |
| Клиент | Гейт Unity | Компиляция, `.meta` | UnityMCP или `-batchmode`: код 0 и ни одного `error CS` | да |
| Статика | `statics-sql-review` | Сгенерированный SQL маппера | dry-run `save-sql` + линт | только советует |
| Контракты | `contract-check` | Клиент↔сервер: типы, поля, опкоды, дрейф статики | только чтение | только советует, фикс после аппрува |
| Прецеденты | `bestpractice-gate` | Похожие решения и риски регрессий | `bestprictice-finder` | нет |
| Поведение | `qa-bot` / e2e / `test-stage` | Фича работает в живой игре | PASS только при двух сигналах: видео и данные | вердикт |
| MR | AI-ревьюер | см. §9 | детерминированные валидаторы + LLM | да |
| Статика на CI | `statics-validator` (шаг TeamCity) | Ссылки, циклы, `condition`, фолбэк AB-групп | детерминированно | нет, решает человек по WRN-строкам |
| Knowledge | 7 валидаторов фабрики | Формат, ссылки, дубли | детерминированно + векторно | да (для MR документа) |
| Внешний | Codex | Всё | задекларировано в `CLAUDE.md` | механизм запуска не описан |

---

## 11. Прочие серверные компоненты

- **`mcp-discord-bot`**:
  - консультант на headless Claude: sonnet, opus по ключевым словам, до 20 ходов, инструменты только на чтение;
  - автотриаж форума багов в Jira;
  - суточный контроль сквадов с кнопками «Продлить» и «Снести».
- **`mcp-jira-bridge`.** Назначение задачи на `ai@juicybuttons.com` создаёт задачу CEO-агенту во внешней AI-системе. Статус и комментарий возвращаются обратно в Jira.
- **`mcp-agents-config-server`.** Раздаёт `CLAUDE.md` ролей и `.mcp.json` из `backend-vibecode` через git-sync.
- **`mcp-seq-server`, `mcp-teamcity-server`.** Логи Seq; TeamCity (в том числе запуск и отмена сборок под одним токеном).
- **game-mcp.** MCP-сервер, раннер e2e без LLM (JUnit и артефакты) и пул-прокси игр (порты 8787 и 8790). Связан с клиентом WS-мостом TestBridge.

---

## 12. Известные несостыковки

1. **Две конституции.** `speckit.plan` и `speckit.analyze` сверяются с `.specify/memory/constitution.md` («Backend Constitution»), а не с `docs/specs/constitution/`.
2. **Разный состав советов.**
   - `/consilium`: страж конституции и quality-gate обязательны.
   - Совет-ревью `/work`: только skeptic и engineer.
   - Plan-Council в спеке Task Forge — без стража, а в `impl-test-pipeline.js` страж есть.
3. **Ссылки на прежние точки входа.**
   - `CLAUDE.md`: «через `/dev-workflow next`»;
   - `on-git-branch-merge`: «`/dev-workflow` шаг 13»;
   - `on-handoff-jira-task`: `bugfix-preprod` §6.6;
   - зеркало Codex всё ещё предлагает `/dev-workflow` и `/bugfix` и не знает `/work`.
4. **Автоспецификация.** Документ `mcp-preplanner.md` помечен `active`, хотя сервис отключён. Метку `ai-spec-ready` после уточнения теперь никто не снимает: `intake-conveyor.md` поручает это роботу, а `/auto-spec` финализацию не делает.
5. **Кто ставит финальный статус после мержа.**
   - ROADMAP: «auto-merge + Done»;
   - `jira-delivery-flow`: ревьюер ставит только `Ready To QA`;
   - внутри того же правила «Открытые вопросы» п.2 противоречат основному тексту.
6. **AI-ревьюер: документ против спеки.**
   - Документ описывает webhook, спека — опрос GitLab.
   - Условие автомержа в спеке (`Ready to Merge`, только preupdate) не совпадает с фактом: статуса больше нет, влития идут в обе линии.
7. **Документы про сам RAG устарели.**
   - `CLAUDE.md` говорит «MR в ветку `rag`», факт — `preprod`.
   - Описаны фильтры по payload, а у `search_context` их нет.
   - Описан реранкер, его нет.
   - `mcp-qdrant.md` описывает 1024 измерения, факт — 3072.
   - `REGISTRY.md` застыл на 01.06.
8. **Хуки.**
   - `session_title` и `game_mcp_up` не подключены.
   - `migration-order-guard` матчит весь payload, включая вывод команды, и срабатывает ложно.
9. **State в двух местах.** Корневой `workflow-states/` игнорируется git'ом, `.claude/skills/work/workflow-states/` коммитится, путь в `/work` записан неоднозначно.
10. **Советники** объявляют инструмент `mcp__mcp-tools__search_code`, которого нет (есть `git_search_code`). `/consilium` отсылает их к `/repos/checkouts/…`, а на машине разработчика такого пути нет.
11. **Указатели прежних скиллов** видны модели как обычные скиллы: `disable-model-invocation` у них нет.
12. **Права.** Allow-лист открыт почти полностью, `ask` и `deny` нет. Защиту держат один блокирующий хук, текст правил и серверные ограничения mcp-tools.
