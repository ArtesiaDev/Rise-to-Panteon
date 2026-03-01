using System;
using System.Reflection;
using RuntimeRoguelike.Dots.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RuntimeRoguelike.Dots.Hybrid
{
    /// <summary>
    /// Создаёт ECS config entities из Authoring MonoBehaviours на DotsConfig при старте.
    /// Заменяет SubScene baking для гибридного подхода (ECS-симуляция + GameObject-рендеринг).
    /// Использует рефлексию для доступа к Authoring компонентам без зависимости от Authoring assembly.
    /// </summary>
    public class ConfigBridge : MonoBehaviour
    {
        private const BindingFlags FieldFlags =
            BindingFlags.NonPublic | BindingFlags.Instance;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[ConfigBridge] DefaultGameObjectInjectionWorld is null");
                return;
            }

            var em = world.EntityManager;

            // Защита от повторного создания конфига
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<RunConfigData>());
            if (!query.IsEmpty)
            {
                Debug.Log("[ConfigBridge] Config entity уже существует, пропускаем");
                return;
            }

            var configEntity = em.CreateEntity();
#if UNITY_EDITOR
            em.SetName(configEntity, "DotsConfig");
#endif

            // --- RunConfigData ---
            var runAuth = FindAuthoring("RunConfigAuthoring");
            if (runAuth != null)
            {
                em.AddComponentData(configEntity, new RunConfigData
                {
                    RandomizeSeedOnStart = GetField<bool>(runAuth, "_randomizeSeedOnStart"),
                    InitialSeed = GetField<int>(runAuth, "_initialSeed"),
                    MapSize = ToInt2(GetField<Vector2Int>(runAuth, "_mapSize"))
                });
            }

            // --- GridConfigData ---
            var gridAuth = FindAuthoring("GridConfigAuthoring");
            if (gridAuth != null)
            {
                em.AddComponentData(configEntity, new GridConfigData
                {
                    CellSize = GetField<float>(gridAuth, "_cellSize")
                });
            }

            // --- MapGenerationConfigData ---
            var mapAuth = FindAuthoring("MapGenerationConfigAuthoring");
            if (mapAuth != null)
            {
                em.AddComponentData(configEntity, new MapGenerationConfigData
                {
                    RoomAttempts = GetField<int>(mapAuth, "_roomAttempts"),
                    MinRoomSize = GetField<int>(mapAuth, "_minRoomSize"),
                    MaxRoomSize = GetField<int>(mapAuth, "_maxRoomSize"),
                    SafeRadius = GetField<int>(mapAuth, "_safeRadius"),
                    FallbackRoomSize = ToInt2(GetField<Vector2Int>(mapAuth, "_fallbackRoomSize"))
                });
            }

            // --- PlayerConfigData ---
            var playerAuth = FindAuthoring("PlayerConfigAuthoring");
            if (playerAuth != null)
            {
                em.AddComponentData(configEntity, new PlayerConfigData
                {
                    MaxHealth = GetField<int>(playerAuth, "_maxHealth"),
                    MoveSpeed = GetField<float>(playerAuth, "_moveSpeed")
                });
            }

            // --- EnemyConfigData ---
            var enemyAuth = FindAuthoring("EnemyConfigAuthoring");
            if (enemyAuth != null)
            {
                em.AddComponentData(configEntity, new EnemyConfigData
                {
                    MaxHealth = GetField<int>(enemyAuth, "_maxHealth"),
                    BaseDamage = GetField<int>(enemyAuth, "_baseDamage"),
                    MoveSpeed = GetField<float>(enemyAuth, "_moveSpeed"),
                    AggroRange = GetField<float>(enemyAuth, "_aggroRange"),
                    AttackRange = GetField<float>(enemyAuth, "_attackRange"),
                    AttackCooldown = GetField<float>(enemyAuth, "_attackCooldown"),
                    PathRefreshInterval = GetField<float>(enemyAuth, "_pathRefreshInterval"),
                    IdleMoveInterval = GetField<float>(enemyAuth, "_idleMoveInterval")
                });
            }

            // --- EnemySpawnConfigData ---
            var spawnAuth = FindAuthoring("EnemySpawnConfigAuthoring");
            if (spawnAuth != null)
            {
                em.AddComponentData(configEntity, new EnemySpawnConfigData
                {
                    InitialCount = GetField<int>(spawnAuth, "_initialCount"),
                    MaxCount = GetField<int>(spawnAuth, "_maxCount"),
                    SpawnInterval = GetField<float>(spawnAuth, "_spawnInterval"),
                    SpawnAttempts = GetField<int>(spawnAuth, "_spawnAttempts")
                });
            }

            // --- PlayerAttackConfigData ---
            var attackAuth = FindAuthoring("PlayerAttackConfigAuthoring");
            if (attackAuth != null)
            {
                em.AddComponentData(configEntity, new PlayerAttackConfigData
                {
                    BaseDamage = GetField<int>(attackAuth, "_baseDamage"),
                    AttackCooldown = GetField<float>(attackAuth, "_attackCooldown"),
                    AttackRange = GetField<float>(attackAuth, "_attackRange"),
                    AttackOffset = GetField<float>(attackAuth, "_attackOffset")
                });
            }

            // --- LevelConfigData ---
            var levelAuth = FindAuthoring("LevelConfigAuthoring");
            if (levelAuth != null)
            {
                em.AddComponentData(configEntity, new LevelConfigData
                {
                    BaseXpToLevel = GetField<int>(levelAuth, "_baseXpToLevel"),
                    XpIncreasePerLevel = GetField<int>(levelAuth, "_xpIncreasePerLevel")
                });
            }

            // --- DifficultyConfigData ---
            var diffAuth = FindAuthoring("DifficultyConfigAuthoring");
            if (diffAuth != null)
            {
                em.AddComponentData(configEntity, new DifficultyConfigData
                {
                    TimeToMaxDifficulty = GetField<float>(diffAuth, "_timeToMaxDifficulty"),
                    MaxEnemyStatMultiplier = GetField<float>(diffAuth, "_maxEnemyStatMultiplier"),
                    MaxSpawnRateMultiplier = GetField<float>(diffAuth, "_maxSpawnRateMultiplier"),
                    LevelStatBonus = GetField<float>(diffAuth, "_levelStatBonus")
                });
            }

            // --- HazardConfigData ---
            var hazardAuth = FindAuthoring("HazardConfigAuthoring");
            if (hazardAuth != null)
            {
                em.AddComponentData(configEntity, new HazardConfigData
                {
                    HazardChance = GetField<float>(hazardAuth, "_hazardChance"),
                    SpikeChance = GetField<float>(hazardAuth, "_spikeChance"),
                    SpikeTickInterval = GetField<float>(hazardAuth, "_spikeTickInterval"),
                    SpikeDamage = GetField<int>(hazardAuth, "_spikeDamage"),
                    PoisonDuration = GetField<float>(hazardAuth, "_poisonDuration"),
                    PoisonDps = GetField<float>(hazardAuth, "_poisonDps"),
                    PoisonTickInterval = GetField<float>(hazardAuth, "_poisonTickInterval")
                });
            }

            // --- LootConfigData + DynamicBuffer<LootEntryData> ---
            var lootAuth = FindAuthoring("LootConfigAuthoring");
            if (lootAuth != null)
            {
                em.AddComponentData(configEntity, new LootConfigData
                {
                    DropChance = GetField<float>(lootAuth, "_dropChance")
                });

                var buffer = em.AddBuffer<LootEntryData>(configEntity);
                var table = GetField<Array>(lootAuth, "_lootTable");
                if (table != null)
                {
                    foreach (var entry in table)
                    {
                        buffer.Add(new LootEntryData
                        {
                            Type = GetField<PickupType>(entry, "_type"),
                            Amount = GetField<int>(entry, "_amount"),
                            Weight = GetField<int>(entry, "_weight")
                        });
                    }
                }
            }

            // --- PerkConfigData + DynamicBuffer<PerkData> ---
            var perkAuth = FindAuthoring("PerkConfigAuthoring");
            if (perkAuth != null)
            {
                em.AddComponentData(configEntity, new PerkConfigData
                {
                    ChoicesCount = GetField<int>(perkAuth, "_choicesCount")
                });

                var perkBuffer = em.AddBuffer<PerkData>(configEntity);
                var perks = GetField<Array>(perkAuth, "_perks");
                if (perks != null)
                {
                    foreach (var perk in perks)
                    {
                        perkBuffer.Add(new PerkData
                        {
                            Type = GetField<PerkType>(perk, "_type"),
                            Value = GetField<float>(perk, "_value")
                        });
                    }
                }
            }

            // --- PrefabConfigData (без Entity-префабов в гибридном режиме) ---
            em.AddComponentData(configEntity, new PrefabConfigData
            {
                Player = Entity.Null,
                Enemy = Entity.Null,
                Loot = Entity.Null
            });

            Debug.Log("[ConfigBridge] Config entity создан успешно");
        }

        /// <summary>
        /// Ищет MonoBehaviour по имени типа среди компонентов на этом GameObject.
        /// Позволяет не зависеть от Authoring assembly.
        /// </summary>
        private Component FindAuthoring(string typeName)
        {
            foreach (var component in GetComponents<MonoBehaviour>())
            {
                if (component != null && component.GetType().Name == typeName)
                    return component;
            }

            Debug.LogWarning($"[ConfigBridge] Компонент '{typeName}' не найден на {gameObject.name}");
            return null;
        }

        private static T GetField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, FieldFlags);
            if (field == null)
            {
                // Пробуем public поля (для struct полей в LootEntryAuthoring и PerkDefinitionAuthoring)
                field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            }

            if (field == null)
            {
                Debug.LogWarning($"[ConfigBridge] Поле '{fieldName}' не найдено в {obj.GetType().Name}");
                return default;
            }

            return (T)field.GetValue(obj);
        }

        private static int2 ToInt2(Vector2Int v) => new(v.x, v.y);
    }
}
