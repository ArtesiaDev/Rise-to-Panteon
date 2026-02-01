using System.Collections.Generic;

namespace RuntimeRoguelike.Ecs
{
    public class EntityViewRegistry
    {
        private readonly Dictionary<int, EntityView> _views = new Dictionary<int, EntityView>();

        public IEnumerable<EntityView> Views => _views.Values;

        public bool Contains(int entityId)
        {
            return _views.ContainsKey(entityId);
        }

        public bool TryGet(int entityId, out EntityView view)
        {
            return _views.TryGetValue(entityId, out view);
        }

        public void Register(int entityId, EntityView view)
        {
            _views[entityId] = view;
        }

        public bool Unregister(int entityId, out EntityView view)
        {
            if (_views.TryGetValue(entityId, out view))
            {
                _views.Remove(entityId);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _views.Clear();
        }
    }
}
