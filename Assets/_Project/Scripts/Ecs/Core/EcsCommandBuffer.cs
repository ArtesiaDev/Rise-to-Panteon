using System;
using System.Collections.Generic;

namespace RuntimeRoguelike.Ecs
{
    public class EcsCommandBuffer
    {
        private readonly List<ICommand> _commands = new List<ICommand>(128);

        public int PendingCount => _commands.Count;

        public void CreateEntity(Action<EcsWorld, EcsEntity> initializer)
        {
            _commands.Add(new CreateEntityCommand(initializer));
        }

        public void DestroyEntity(EcsEntity entity)
        {
            _commands.Add(new DestroyEntityCommand(entity));
        }

        public void AddComponent<T>(EcsEntity entity, T component) where T : struct
        {
            _commands.Add(new AddComponentCommand<T>(entity, component));
        }

        public void RemoveComponent<T>(EcsEntity entity) where T : struct
        {
            _commands.Add(new RemoveComponentCommand<T>(entity));
        }

        public void Clear()
        {
            _commands.Clear();
        }

        public void Playback(EcsWorld world)
        {
            for (var i = 0; i < _commands.Count; i++)
            {
                _commands[i].Apply(world);
            }

            _commands.Clear();
        }

        private interface ICommand
        {
            void Apply(EcsWorld world);
        }

        private class CreateEntityCommand : ICommand
        {
            private readonly Action<EcsWorld, EcsEntity> _initializer;

            public CreateEntityCommand(Action<EcsWorld, EcsEntity> initializer)
            {
                _initializer = initializer;
            }

            public void Apply(EcsWorld world)
            {
                var entity = world.CreateEntity();
                _initializer?.Invoke(world, entity);
            }
        }

        private class DestroyEntityCommand : ICommand
        {
            private readonly EcsEntity _entity;

            public DestroyEntityCommand(EcsEntity entity)
            {
                _entity = entity;
            }

            public void Apply(EcsWorld world)
            {
                world.DestroyEntity(_entity);
            }
        }

        private class AddComponentCommand<T> : ICommand where T : struct
        {
            private readonly EcsEntity _entity;
            private readonly T _component;

            public AddComponentCommand(EcsEntity entity, T component)
            {
                _entity = entity;
                _component = component;
            }

            public void Apply(EcsWorld world)
            {
                var pool = world.GetPool<T>();
                pool.Add(_entity.Id, _component);
            }
        }

        private class RemoveComponentCommand<T> : ICommand where T : struct
        {
            private readonly EcsEntity _entity;

            public RemoveComponentCommand(EcsEntity entity)
            {
                _entity = entity;
            }

            public void Apply(EcsWorld world)
            {
                var pool = world.GetPool<T>();
                pool.RemoveEntity(_entity.Id);
            }
        }
    }
}
