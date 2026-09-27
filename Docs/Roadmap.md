# Роадмап реализации

> Единственное место статуса реализации (Docs/Tech/Harness.md §6). Статусы: `—` не начато · `◐` частично · `✓` готово —
> правила фичи этого этапа реализованы, тесты зелёные. «Есть / нет» — коротко, что работает и чего не хватает.
> Строка обновляется тем же коммитом, что и код. Что делает фича — GDD по ссылке; где её код — CodeStructure §7.4.

## Инфраструктура

| Часть | Статус | Есть / нет |
|---|---|---|
| Сборки, asmdef, каркас `Code/` (Architecture README §4.1, CodeStructure §3) | — | |
| Архитектурные тесты `Arch` (CodeStructure §3.4) | — | |
| Шаблон среза `Features/_Template~` (CodeStructure §6.3) | — | |
| `WorldHost`, тик и группы систем (Simulation §2–3) | — | |
| Команды: `PlayerInputFrame`, операции (Simulation §5) | — | |
| Экспорт: снимок, события, read-модели (Simulation §6) | — | |
| Шаг мира: каркас `LocationTransitionGroup` (Simulation §7–8) | — | |
| Скоупы VContainer, инсталлеры фич, граф загрузки, сцены (Services §2–4) | — | |
| Логирование (Services §10) | — | |
| Dev-инструменты (Services §11) | — | |
| Репозиторий `Configs/`, схемы, пак конфигов (Content §2–4) | — | |
| Ассеты и `IAssetProvider` (Content §9) | — | |
| Редактор комнат (Content §7) | — | |
| `WorldPresenter`, вьюхи, пулы, интерполяция (Presentation §3–4) | — | |
| Анимация существ и облики (Presentation §5) | — | |
| Рендер локации, свет, VFX (Presentation §6–9) | — | |
| Маршрутизация событий в VFX и звук (Presentation §10) | — | |
| UI: экраны, `IScreenService`, тема (UI §2–3, §8) | — | |
| Приведение прототипа старой концепции (CodeStructure §2.4) | — | |

## Прототип

| Фича | Статус | Есть / нет |
|---|---|---|
| [W01 Иерархия мира: ярус → этаж → локации](GDD/Features/W01-WorldHierarchy.md) | — | |
| [W02 Генератор планировки](GDD/Features/W02-LayoutGenerator.md) | — | |
| [W03 Граф маршрутов, свободный спуск](GDD/Features/W03-RouteGraph.md) | — | |
| [W05 Плотность среды и запас жизненной силы](GDD/Features/W05-EnvironmentDensity.md) | — | |
| [W06 Распад](GDD/Features/W06-Decay.md) | — | |
| [W07 Давление: скудная добыча, страх, давление на реальность](GDD/Features/W07-Pressure.md) | — | |
| [W08 Приглушение плотности](GDD/Features/W08-DensitySuppression.md) | — | |
| [W10 Постоянство мира](GDD/Features/W10-WorldPersistence.md) | — | |
| [W12 Замки и ключи](GDD/Features/W12-LocksAndKeys.md) | — | |
| [W13 Ловушки](GDD/Features/W13-Traps.md) | — | |
| [W14 Лут в мире](GDD/Features/W14-WorldLoot.md) | — | |
| [W16 Переходы между локациями](GDD/Features/W16-LocationTransitions.md) | — | |
| [E01 Поглощение](GDD/Features/E01-Absorption.md) | — | |
| [E02 Уровни и плотность](GDD/Features/E02-LevelsAndDensity.md) | — | |
| [E03 Характеристики](GDD/Features/E03-Attributes.md) | — | |
| [E04 Эссенции, семейства, состав](GDD/Features/E04-Essences.md) | — | |
| [E05 Черты](GDD/Features/E05-Traits.md) | — | |
| [E06 Линька и варианты](GDD/Features/E06-Molt.md) | — | |
| [E07 Метаморфоза и стадии](GDD/Features/E07-Metamorphosis.md) | — | |
| [E08 Кокон](GDD/Features/E08-Cocoon.md) | — | |
| [C01 Модель боя](GDD/Features/C01-CombatModel.md) | — | |
| [C02 Размер облика](GDD/Features/C02-FormSize.md) | — | |
| [C03 Базовая атака](GDD/Features/C03-BaseAttack.md) | — | |
| [C04 Передвижение и рывок](GDD/Features/C04-MovementAndDash.md) | — | |
| [C05 Способности и слоты](GDD/Features/C05-Abilities.md) | — | |
| [C06 Урон и статусы](GDD/Features/C06-DamageAndStatuses.md) | — | |
| [C07 Лечение](GDD/Features/C07-Healing.md) | — | |
| [C10 Камера](GDD/Features/C10-Camera.md) | — | |
| [B01 Виды существ](GDD/Features/B01-Species.md) | — | |
| [B02 ИИ и пищевая сеть](GDD/Features/B02-CreatureAI.md) | — | |
| [B03 Мини-боссы](GDD/Features/B03-MiniBosses.md) | — | |
| [L01 Время мира](GDD/Features/L01-WorldClock.md) | — | |
| [L03 Правила шага мира](GDD/Features/L03-WorldStepRules.md) | — | |
| [L05 Следы](GDD/Features/L05-Traces.md) | — | |
| [D01 Смерть и возрождение](GDD/Features/D01-DeathAndRespawn.md) | — | |
| [D02 Эхо Души](GDD/Features/D02-SoulEcho.md) | — | |
| [K02 Якоря](GDD/Features/K02-Anchors.md) | — | |
| [I01 Обычные предметы и источники](GDD/Features/I01-Items.md) | — | |
| [I03 Слоты: внутренние и внешние](GDD/Features/I03-ItemSlots.md) | — | |
| [I05 Расходники](GDD/Features/I05-Consumables.md) | — | |
| [I06 Видимость предметов](GDD/Features/I06-ItemVisibility.md) | — | |
| [I07 Сила предмета и плотность](GDD/Features/I07-ItemPowerCap.md) | — | |
| [N05 Реакции персонажа](GDD/Features/N05-CharacterReactions.md) | — | |
| [U01 Управление, ориентация, бюджет кнопок](GDD/Features/U01-Controls.md) | — | |
| [U02 Наведение](GDD/Features/U02-Aiming.md) | — | |
| [U03 HUD](GDD/Features/U03-HUD.md) | — | |
| [U04 Экраны и пауза](GDD/Features/U04-ScreensAndPause.md) | — | |
| [U05 Карта и туман войны](GDD/Features/U05-MapAndFog.md) | — | |
| [U07 Обучение](GDD/Features/U07-Onboarding.md) | — | |
| [M01 Сохранение](GDD/Features/M01-SaveSystem.md) | — | |
| [M02 Создание мира и режимы](GDD/Features/M02-WorldCreation.md) | — | |
| [M04 Локализация](GDD/Features/M04-Localization.md) | — | |

## Срез

| Фича | Статус | Есть / нет |
|---|---|---|
| [W04 Ручные вставки в процедурный мир](GDD/Features/W04-HandcraftedInserts.md) | — | |
| [W09 Врата Резонанса: два ключа](GDD/Features/W09-ResonanceGates.md) | — | |
| [W11 Биомы ярусов](GDD/Features/W11-TierBiomes.md) | — | |
| [W15 Тайники и тайные ходы](GDD/Features/W15-SecretsAndPassages.md) | — | |
| [W17 Эффекты среды (огонь, яд, вода)](GDD/Features/W17-EnvironmentEffects.md) | — | |
| [E09 Гнёзда как места кокона](GDD/Features/E09-CocoonNests.md) | — | |
| [E11 Перерастание черт](GDD/Features/E11-TraitOutgrowing.md) | — | |
| [E12 Эволюционные услуги](GDD/Features/E12-EvolutionServices.md) | — | |
| [C08 Скрытность](GDD/Features/C08-Stealth.md) | — | |
| [B04 Изначальные Стражи](GDD/Features/B04-OriginalGuardians.md) | — | |
| [B05 Преемники Стражей](GDD/Features/B05-GuardianSuccessors.md) | — | |
| [L02 Симуляция вдали](GDD/Features/L02-FarSimulation.md) | — | |
| [L04 Истощение](GDD/Features/L04-Depletion.md) | — | |
| [L06 Слухи](GDD/Features/L06-Rumors.md) | — | |
| [L07 NPC в мире](GDD/Features/L07-WorldNPCs.md) | — | |
| [L08 Преемники функций NPC](GDD/Features/L08-NPCFunctionSuccession.md) | — | |
| [L09 Соперники](GDD/Features/L09-Rivals.md) | — | |
| [L10 Фракции](GDD/Features/L10-Factions.md) | — | |
| [L11 Подчинение](GDD/Features/L11-Subjugation.md) | — | |
| [L12 Страховки](GDD/Features/L12-Guardrails.md) | — | |
| [L13 Рычаги игрока](GDD/Features/L13-PlayerLevers.md) | — | |
| [K01 Локация Лагеря](GDD/Features/K01-CampLocation.md) | — | |
| [K03 Приведение NPC](GDD/Features/K03-BringingNPCs.md) | — | |
| [K04 Активности](GDD/Features/K04-CampActivities.md) | — | |
| [K05 Хранилище](GDD/Features/K05-Storage.md) | — | |
| [K07 Улучшения Лагеря](GDD/Features/K07-CampUpgrades.md) | — | |
| [I02 Реликвии](GDD/Features/I02-Relics.md) | — | |
| [I04 Свойства и редкость](GDD/Features/I04-ItemPropertiesAndRarity.md) | — | |
| [I08 Валюты](GDD/Features/I08-Currencies.md) | — | |
| [I09 Материалы](GDD/Features/I09-Materials.md) | — | |
| [I10 Стоки и защита от арбитража](GDD/Features/I10-SinksAndArbitrage.md) | — | |
| [I11 Крафт и улучшение](GDD/Features/I11-CraftAndUpgrade.md) | — | |
| [N01 Фрагменты лора](GDD/Features/N01-LoreFragments.md) | — | |
| [N02 Диалоги](GDD/Features/N02-Dialogues.md) | — | |
| [N03 Просьбы](GDD/Features/N03-Requests.md) | — | |
| [N04 Катсцены](GDD/Features/N04-Cutscenes.md) | — | |
| [N06 Откровения](GDD/Features/N06-Revelations.md) | — | |
| [U06 Журнал](GDD/Features/U06-Journal.md) | — | |
| [U08 Настройки](GDD/Features/U08-Settings.md) | — | |
| [U09 Звук](GDD/Features/U09-Sound.md) | — | |

## Гл1

| Фича | Статус | Есть / нет |
|---|---|---|
| [W18 Рубеж](GDD/Features/W18-Frontier.md) | — | |
| [W19 Нестабильные измерения](GDD/Features/W19-UnstableDimensions.md) | — | |
| [E10 Полюса и гибриды](GDD/Features/E10-PolesAndHybrids.md) | — | |
| [C09 Призыв](GDD/Features/C09-Summoning.md) | — | |
| [B06 Мегабосс Рубежа](GDD/Features/B06-FrontierMegaboss.md) | — | |
| [D03 Наследие](GDD/Features/D03-Legacy.md) | — | |
| [K06 Персонализация](GDD/Features/K06-CampPersonalization.md) | — | |
| [N07 Концовки](GDD/Features/N07-Endings.md) | — | |
| [M03 Главы и продолжение](GDD/Features/M03-ChaptersAndContinuation.md) | — | |
| [M05 Между мирами](GDD/Features/M05-BetweenWorlds.md) | — | |
