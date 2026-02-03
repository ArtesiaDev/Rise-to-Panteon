using System;
using System.Collections.Generic;
using System.Reflection;

namespace RuntimeRoguelike.Ecs
{
    public class EcsSystemsPipeline
    {
        private readonly List<IEcsAwakeSystem> _awakeSystems = new List<IEcsAwakeSystem>();
        private readonly List<IEcsEnableSystem> _enableSystems = new List<IEcsEnableSystem>();
        private readonly List<IEcsStartSystem> _startSystems = new List<IEcsStartSystem>();
        private readonly List<IEcsDisableSystem> _disableSystems = new List<IEcsDisableSystem>();
        private readonly List<IEcsPreInitSystem> _preInitSystems = new List<IEcsPreInitSystem>();
        private readonly List<IEcsInitSystem> _initSystems = new List<IEcsInitSystem>();
        private readonly List<IEcsLateInitSystem> _lateInitSystems = new List<IEcsLateInitSystem>();
        private readonly List<IEcsPreUpdateSystem> _preUpdateSystems = new List<IEcsPreUpdateSystem>();
        private readonly List<IEcsUpdateSystem> _updateSystems = new List<IEcsUpdateSystem>();
        private readonly List<IEcsPostUpdateSystem> _postUpdateSystems = new List<IEcsPostUpdateSystem>();
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
            if (system is IEcsAwakeSystem awake)
            {
                _awakeSystems.Add(awake);
            }

            if (system is IEcsEnableSystem enable)
            {
                _enableSystems.Add(enable);
            }

            if (system is IEcsStartSystem start)
            {
                _startSystems.Add(start);
            }

            if (system is IEcsDisableSystem disable)
            {
                _disableSystems.Add(disable);
            }

            if (system is IEcsPreInitSystem preInit)
            {
                _preInitSystems.Add(preInit);
            }

            if (system is IEcsInitSystem init)
            {
                _initSystems.Add(init);
            }

            if (system is IEcsLateInitSystem lateInit)
            {
                _lateInitSystems.Add(lateInit);
            }

            if (system is IEcsPreUpdateSystem preUpdate)
            {
                _preUpdateSystems.Add(preUpdate);
            }

            if (system is IEcsUpdateSystem update)
            {
                _updateSystems.Add(update);
            }

            if (system is IEcsPostUpdateSystem postUpdate)
            {
                _postUpdateSystems.Add(postUpdate);
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
            SortByOrder(_awakeSystems);
            SortByOrder(_enableSystems);
            SortByOrder(_startSystems);
            SortByOrder(_disableSystems);
            SortByOrder(_preInitSystems);
            SortByOrder(_initSystems);
            SortByOrder(_lateInitSystems);
            SortByOrder(_preUpdateSystems);
            SortByOrder(_updateSystems);
            SortByOrder(_postUpdateSystems);
            SortByOrder(_fixedSystems);
            SortByOrder(_lateSystems);
            SortByOrder(_disposeSystems);
        }

        public void Awake()
        {
            for (var i = 0; i < _awakeSystems.Count; i++)
            {
                _awakeSystems[i].Awake(World);
            }
        }

        public void OnEnable()
        {
            for (var i = 0; i < _enableSystems.Count; i++)
            {
                _enableSystems[i].OnEnable(World);
            }
        }

        public void Start()
        {
            for (var i = 0; i < _startSystems.Count; i++)
            {
                _startSystems[i].Start(World);
            }
        }

        public void OnDisable()
        {
            for (var i = 0; i < _disableSystems.Count; i++)
            {
                _disableSystems[i].OnDisable(World);
            }
        }

        public void PreInit()
        {
            for (var i = 0; i < _preInitSystems.Count; i++)
            {
                _preInitSystems[i].PreInit(World, CommandBuffer);
            }

            CommandBuffer.Playback(World);
        }

        public void Init()
        {
            for (var i = 0; i < _initSystems.Count; i++)
            {
                _initSystems[i].Init(World, CommandBuffer);
            }

            CommandBuffer.Playback(World);
        }

        public void LateInit()
        {
            for (var i = 0; i < _lateInitSystems.Count; i++)
            {
                _lateInitSystems[i].LateInit(World, CommandBuffer);
            }

            CommandBuffer.Playback(World);
        }

        public void Update(float deltaTime)
        {
            for (var i = 0; i < _preUpdateSystems.Count; i++)
            {
                _preUpdateSystems[i].PreUpdate(World, CommandBuffer, deltaTime);
            }

            CommandBuffer.Playback(World);

            for (var i = 0; i < _updateSystems.Count; i++)
            {
                _updateSystems[i].Update(World, CommandBuffer, deltaTime);
            }

            CommandBuffer.Playback(World);

            for (var i = 0; i < _postUpdateSystems.Count; i++)
            {
                _postUpdateSystems[i].PostUpdate(World, CommandBuffer, deltaTime);
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
