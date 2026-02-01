using System.Collections.Generic;
using UnityEngine;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class EntityViewPool
    {
        private readonly EntityViewFactory _factory;
        private readonly Dictionary<SpriteKey, Stack<EntityView>> _pool = new Dictionary<SpriteKey, Stack<EntityView>>();

        public EntityViewPool(EntityViewFactory factory)
        {
            _factory = factory;
        }

        public EntityView Get(SpriteKey key, Transform parent)
        {
            if (_pool.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                var view = stack.Pop();
                view.GameObject.SetActive(true);
                view.Transform.SetParent(parent, false);
                _factory.ApplySprite(view, key);
                return view;
            }

            return _factory.Create(key, parent);
        }

        public void Release(EntityView view, Transform poolRoot)
        {
            view.GameObject.SetActive(false);
            view.Transform.SetParent(poolRoot, false);

            if (!_pool.TryGetValue(view.Key, out var stack))
            {
                stack = new Stack<EntityView>();
                _pool[view.Key] = stack;
            }

            stack.Push(view);
        }
    }
}
