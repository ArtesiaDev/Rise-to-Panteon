using UnityEngine;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class EntityViewFactory
    {
        private readonly SpriteFactory _spriteFactory;

        public EntityViewFactory(SpriteFactory spriteFactory)
        {
            _spriteFactory = spriteFactory;
        }

        public EntityView Create(SpriteKey key, Transform parent)
        {
            var viewObject = new GameObject($"View_{key}");
            viewObject.transform.SetParent(parent, false);
            var renderer = viewObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _spriteFactory.GetSprite(key);
            return new EntityView(viewObject, renderer, key);
        }

        public void ApplySprite(EntityView view, SpriteKey key)
        {
            view.SetSprite(_spriteFactory.GetSprite(key), key);
        }
    }
}
