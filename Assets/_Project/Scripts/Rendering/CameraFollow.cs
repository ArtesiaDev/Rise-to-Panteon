using RuntimeRoguelike.Configs;
using UnityEngine;

namespace RuntimeRoguelike
{
    public class CameraFollow : MonoBehaviour
    {
        private float _smoothTime;
        private Transform _target;
        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var targetPosition = new Vector3(_target.position.x, _target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
        }

        public void Initialize(Transform followTarget, CameraFollowConfig config)
        {
            _target = followTarget;
            _smoothTime = config != null ? config.SmoothTime : 0.15f;
        }
    }
}
