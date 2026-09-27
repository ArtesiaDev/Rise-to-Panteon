# Реестр фич

> Карта всех фич игры. ID стабильны и не перенумеровываются.
> Правила, параметры и связи фичи живут в её файле в `Features/` (по [шаблону](_FeatureTemplate.md)) — это источник
> истины. Документы систем (ссылки в заголовках разделов) — обзор и карта фич.

**Статусы:** ✅ решено (детали — параметры баланса) · 🟡 основа есть, детали открыты · ❓ ждёт ответа автора ·
⏳ позже.
**Этапы** ([Scope.md](Scope.md)): Прототип · Срез — вертикальный срез · Гл1 — глава 1.

## Мир — [World.md](World.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| W01 | [Иерархия мира: ярус → этаж → локации](Features/W01-WorldHierarchy.md) | Прототип | 🟡 размеры — в прототипе |
| W02 | [Генератор планировки](Features/W02-LayoutGenerator.md) | Прототип | 🟡 алгоритм, валидация |
| W03 | [Граф маршрутов, свободный спуск](Features/W03-RouteGraph.md) | Прототип | 🟡 |
| W04 | [Ручные вставки в процедурный мир](Features/W04-HandcraftedInserts.md) | Срез | ✅ |
| W05 | [Плотность среды и запас жизненной силы](Features/W05-EnvironmentDensity.md) | Прототип | ✅ |
| W06 | [Распад](Features/W06-Decay.md) | Прототип | ✅ |
| W07 | [Давление: скудная добыча, страх, давление на реальность](Features/W07-Pressure.md) | Прототип | ✅ |
| W08 | [Приглушение плотности](Features/W08-DensitySuppression.md) | Прототип | ✅ |
| W09 | [Врата Резонанса: два ключа](Features/W09-ResonanceGates.md) | Срез | ✅ |
| W10 | [Постоянство мира](Features/W10-WorldPersistence.md) | Прототип | ✅ |
| W11 | [Биомы ярусов](Features/W11-TierBiomes.md) | Срез | ✅|
| W12 | [Замки и ключи](Features/W12-LocksAndKeys.md) | Прототип | ✅ |
| W13 | [Ловушки](Features/W13-Traps.md) | Прототип | ✅ |
| W14 | [Лут в мире](Features/W14-WorldLoot.md) | Прототип | ✅ |
| W15 | [Тайники и тайные ходы](Features/W15-SecretsAndPassages.md) | Срез | ✅ |
| W16 | [Переходы между локациями](Features/W16-LocationTransitions.md) | Прототип | ✅ |
| W17 | [Эффекты среды (огонь, яд, вода)](Features/W17-EnvironmentEffects.md) | Срез | ✅ |
| W18 | [Рубеж](Features/W18-Frontier.md) | Гл1 | ✅ |
| W19 | [Нестабильные измерения](Features/W19-UnstableDimensions.md) | Гл1 | ✅ |

## Персонаж и эволюция — [Evolution.md](Evolution.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| E01 | [Поглощение](Features/E01-Absorption.md) | Прототип | ✅ |
| E02 | [Уровни и плотность](Features/E02-LevelsAndDensity.md) | Прототип | ✅ кривая — баланс |
| E03 | [Характеристики](Features/E03-Attributes.md) | Прототип | ✅ формулы — баланс |
| E04 | [Эссенции, семейства, состав](Features/E04-Essences.md) | Прототип | 🟡 каталог |
| E05 | [Черты](Features/E05-Traits.md) | Прототип | 🟡 каталог |
| E06 | [Линька и варианты](Features/E06-Molt.md) | Прототип | ✅ |
| E07 | [Метаморфоза и стадии](Features/E07-Metamorphosis.md) | Прототип | ✅ |
| E08 | [Кокон](Features/E08-Cocoon.md) | Прототип | ✅ |
| E09 | [Гнёзда как места кокона](Features/E09-CocoonNests.md) | Срез | ✅ |
| E10 | [Полюса и гибриды](Features/E10-PolesAndHybrids.md) | Гл1 | ✅ |
| E11 | [Перерастание черт](Features/E11-TraitOutgrowing.md) | Срез | ✅ |
| E12 | [Эволюционные услуги](Features/E12-EvolutionServices.md) | Срез | ✅ |

## Бой — [Combat.md](Combat.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| C01 | [Модель боя](Features/C01-CombatModel.md) | Прототип | ✅ |
| C02 | [Размер облика](Features/C02-FormSize.md) | Прототип | ✅ множители — баланс |
| C03 | [Базовая атака](Features/C03-BaseAttack.md) | Прототип | ✅ |
| C04 | [Передвижение и рывок](Features/C04-MovementAndDash.md) | Прототип | ✅ |
| C05 | [Способности и слоты](Features/C05-Abilities.md) | Прототип | ✅ |
| C06 | [Урон и статусы](Features/C06-DamageAndStatuses.md) | Прототип | 🟡 список типов |
| C07 | [Лечение](Features/C07-Healing.md) | Прототип | ✅ |
| C08 | [Скрытность](Features/C08-Stealth.md) | Срез | ✅ |
| C09 | [Призыв](Features/C09-Summoning.md) | Гл1 | ✅ |
| C10 | [Камера](Features/C10-Camera.md) | Прототип | ✅ |

## Существа — [Creatures.md](Creatures.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| B01 | [Виды существ](Features/B01-Species.md) | Прототип | 🟡 каталог |
| B02 | [ИИ и пищевая сеть](Features/B02-CreatureAI.md) | Прототип | ✅ |
| B03 | [Мини-боссы](Features/B03-MiniBosses.md) | Прототип | ✅ |
| B04 | [Изначальные Стражи](Features/B04-OriginalGuardians.md) | Срез | ✅|
| B05 | [Преемники Стражей](Features/B05-GuardianSuccessors.md) | Срез | ✅ |
| B06 | [Мегабосс Рубежа](Features/B06-FrontierMegaboss.md) | Гл1 | ✅|

## Живой мир — [LivingWorld.md](LivingWorld.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| L01 | [Время мира](Features/L01-WorldClock.md) | Прототип | ✅ |
| L02 | [Симуляция вдали](Features/L02-FarSimulation.md) | Срез | ✅ |
| L03 | [Правила шага мира](Features/L03-WorldStepRules.md) | Прототип (охота, рост) | 🟡 формулы |
| L04 | [Истощение](Features/L04-Depletion.md) | Срез | ✅ |
| L05 | [Следы](Features/L05-Traces.md) | Прототип (минимум) | 🟡 реестр |
| L06 | [Слухи](Features/L06-Rumors.md) | Срез | ✅|
| L07 | [NPC в мире](Features/L07-WorldNPCs.md) | Срез | ✅ |
| L08 | [Преемники функций NPC](Features/L08-NPCFunctionSuccession.md) | Срез | ✅ |
| L09 | [Соперники](Features/L09-Rivals.md) | Срез | ✅|
| L10 | [Фракции](Features/L10-Factions.md) | Срез | ✅|
| L11 | [Подчинение](Features/L11-Subjugation.md) | Срез | ✅ |
| L12 | [Страховки](Features/L12-Guardrails.md) | Срез | ✅ |
| L13 | [Рычаги игрока](Features/L13-PlayerLevers.md) | Срез | ✅ |

## Смерть — [Death.md](Death.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| D01 | [Смерть и возрождение](Features/D01-DeathAndRespawn.md) | Прототип | ✅ |
| D02 | [Эхо Души](Features/D02-SoulEcho.md) | Прототип | ✅ |
| D03 | [Наследие](Features/D03-Legacy.md) | Гл1 | ✅|

## Лагерь — [Camp.md](Camp.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| K01 | [Локация Лагеря](Features/K01-CampLocation.md) | Срез | ✅|
| K02 | [Якоря](Features/K02-Anchors.md) | Прототип (якорь на каждом этаже), Срез | ✅ |
| K03 | [Приведение NPC](Features/K03-BringingNPCs.md) | Срез | ✅|
| K04 | [Активности](Features/K04-CampActivities.md) | Срез | ✅ |
| K05 | [Хранилище](Features/K05-Storage.md) | Срез | ✅ |
| K06 | [Персонализация](Features/K06-CampPersonalization.md) | Гл1 | ✅|
| K07 | [Улучшения Лагеря](Features/K07-CampUpgrades.md) | Срез | ✅ |

## Предметы и экономика — [Items.md](Items.md), [Economy.md](Economy.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| I01 | [Обычные предметы и источники](Features/I01-Items.md) | Прототип | 🟡 |
| I02 | [Реликвии](Features/I02-Relics.md) | Срез | ✅|
| I03 | [Слоты: внутренние и внешние](Features/I03-ItemSlots.md) | Прототип | ✅ |
| I04 | [Свойства и редкость](Features/I04-ItemPropertiesAndRarity.md) | Срез | ✅ |
| I05 | [Расходники](Features/I05-Consumables.md) | Прототип | ✅ |
| I06 | [Видимость предметов](Features/I06-ItemVisibility.md) | Прототип | ✅ |
| I07 | [Сила предмета и плотность](Features/I07-ItemPowerCap.md) | Прототип | ✅ |
| I08 | [Валюты](Features/I08-Currencies.md) | Срез | ✅|
| I09 | [Материалы](Features/I09-Materials.md) | Срез | ✅ |
| I10 | [Стоки и защита от арбитража](Features/I10-SinksAndArbitrage.md) | Срез | ✅ |
| I11 | [Крафт и улучшение](Features/I11-CraftAndUpgrade.md) | Срез | ✅ |

## Нарратив — [Narrative.md](Narrative.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| N01 | [Фрагменты лора](Features/N01-LoreFragments.md) | Срез | ✅|
| N02 | [Диалоги](Features/N02-Dialogues.md) | Срез | ✅|
| N03 | [Просьбы](Features/N03-Requests.md) | Срез | ✅ |
| N04 | [Катсцены](Features/N04-Cutscenes.md) | Срез | ✅ |
| N05 | [Реакции персонажа](Features/N05-CharacterReactions.md) | Прототип | ✅ |
| N06 | [Откровения](Features/N06-Revelations.md) | Срез | ✅|
| N07 | [Концовки](Features/N07-Endings.md) | Гл1 | ✅|

## Интерфейс — [Interface.md](Interface.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| U01 | [Управление, ориентация, бюджет кнопок](Features/U01-Controls.md) | Прототип | ✅ |
| U02 | [Наведение](Features/U02-Aiming.md) | Прототип | ✅ |
| U03 | [HUD](Features/U03-HUD.md) | Прототип | ✅ |
| U04 | [Экраны и пауза](Features/U04-ScreensAndPause.md) | Прототип | ✅ |
| U05 | [Карта и туман войны](Features/U05-MapAndFog.md) | Прототип | ✅ |
| U06 | [Журнал](Features/U06-Journal.md) | Срез | ✅ |
| U07 | [Обучение](Features/U07-Onboarding.md) | Прототип | ✅ |
| U08 | [Настройки](Features/U08-Settings.md) | Срез | ✅ |
| U09 | [Звук](Features/U09-Sound.md) | Срез | ✅ |

## Мета — [Meta.md](Meta.md)

| ID | Фича | Этап | Статус |
|----|------|------|--------|
| M01 | [Сохранение](Features/M01-SaveSystem.md) | Прототип | ✅ |
| M02 | [Создание мира и режимы](Features/M02-WorldCreation.md) | Прототип | ✅ |
| M03 | [Главы и продолжение](Features/M03-ChaptersAndContinuation.md) | Гл1 | ✅ |
| M04 | [Локализация](Features/M04-Localization.md) | Прототип | ✅ |
| M05 | [Между мирами](Features/M05-BetweenWorlds.md) | Гл1 | ✅ |
