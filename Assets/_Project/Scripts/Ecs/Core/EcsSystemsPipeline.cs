using System;
using System.Collections.Generic;
using System.Reflection;

namespace RuntimeRoguelike.Ecs
{
    public class EcsSystemsPipeline
    {
        private readonly List<IEcsInitSystem> _initSystems = new List<IEcsInitSystem>();
        private readonly List<IEcsUpdateSystem> _updateSystems = new List<IEcsUpdateSystem>();
        private readonly List<IEcsFixedSystem> _fixedSystems = new List<IEcsFixedSystem>();
        private readonly List<IEcsLateSystem> _lateSystems = new List<IEcsLateSystem>();
        private readonly List<IEcsDisposeSystem> _disposeSystems = new List<IEcsDisposeSystem>();

        public EcsWorld World { get; }
        public EcsCommandBuffer CommandBuffer { get; }

        public EcsSystemsPipeline(EcsWorld world)
        {
            World = world;
            CommandBuffer = new EcsCommandBuffer();
        }

        public void AddSystem(object system)
        {
            if (system is IEcsInitSystem init)
            {
                _initSystems.Add(init);
            }

            if (system is IEcsUpdateSystem update)
            {
                _updateSystems.Add(update);
            }

            if (system is IEcsFixedSystem fixedSystem)
            {
                _fixedSystems.Add(fixedSystem);
            }

            if (system is IEcsLateSystem late)
            {
                _lateSystems.Add(late);
            }

            if (system is IEcsDisposeSystem dispose)
            {
                _disposeSystems.Add(dispose);
            }
        }

        public void SortSystems()
        {
            SortByOrder(_initSystems);
            SortByOrder(_updateSystems);
            SortByOrder(_fixedSystems);
            SortByOrder(_lateSystems);
            SortByOrder(_disposeSystems);
        }

        public void Init()
        {
            for (var i = 0; i < _initSystems.Count; i++)
            {
                _initSystems[i].Init(World, CommandBuffer);
            }

            CommandBuffer.Playback(World);
        }

        public void Update(float deltaTime)
        {
            for (var i = 0; i < _updateSystems.Count; i++)
            {
                _updateSystems[i].Update(World, CommandBuffer, deltaTime);
            }

            CommandBuffer.Playback(World);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            for (var i = 0; i < _fixedSystems.Count; i++)
            {
                _fixedSystems[i].FixedUpdate(World, CommandBuffer, fixedDeltaTime);
            }

            CommandBuffer.Playback(World);
        }

        public void LateUpdate(float deltaTime)
        {
            for (var i = 0; i < _lateSystems.Count; i++)
            {
                _lateSystems[i].LateUpdate(World, CommandBuffer, deltaTime);
            }

            CommandBuffer.Playback(World);
        }

        public void Dispose()
        {
            for (var i = 0; i < _disposeSystems.Count; i++)
            {
                _disposeSystems[i].Dispose(World);
            }

            CommandBuffer.Clear();
        }

        private void SortByOrder<TSystem>(List<TSystem> systems)
        {
            var hasAttribute = false;
            for (var i = 0; i < systems.Count; i++)
            {
                if (HasOrderAttribute(systems[i]))
                {
                    hasAttribute = true;
                    break;
                }
            }

            if (!hasAttribute)
            {
                return;
            }

            systems.Sort((a, b) => GetOrder(a).CompareTo(GetOrder(b)));
        }

        private int GetOrder<TSystem>(TSystem system)
        {
            var type = system.GetType();
            var attribute = type.GetCustomAttribute<EcsSystemOrderAttribute>();
            return attribute != null ? attribute.Order : 0;
        }

        private bool HasOrderAttribute<TSystem>(TSystem system)
        {
            var type = system.GetType();
            return type.GetCustomAttribute<EcsSystemOrderAttribute>() != null;
        }
    }
}
