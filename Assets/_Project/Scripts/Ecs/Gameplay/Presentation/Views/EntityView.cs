using UnityEngine;
using RuntimeRoguelike;

namespace RuntimeRoguelike.Ecs
{
    public class EntityView
    {
        public GameObject GameObject { get; }
        public Transform Transform { get; }
        public SpriteRenderer Renderer { get; }
        public SpriteKey Key { get; private set; }

        public EntityView(GameObject gameObject, SpriteRenderer renderer, SpriteKey key)
        {
            GameObject = gameObject;
            Transform = gameObject.transform;
            Renderer = renderer;
            Key = key;
        }

        public void SetSprite(Sprite sprite, SpriteKey key)
        {
            Renderer.sprite = sprite;
            Key = key;
        }
    }
}
