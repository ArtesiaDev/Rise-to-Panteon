using UnityEngine;

namespace RuntimeRoguelike
{
    public static class GodObjectBootstrapper
    {
        private const string GodObjectName = "GodObject";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGodObject()
        {
            var godObject = GameObject.Find(GodObjectName);
            if (godObject == null)
            {
                godObject = new GameObject(GodObjectName);
            }

            if (godObject.GetComponent<RunController>() == null)
            {
                godObject.AddComponent<RunController>();
            }
        }
    }
}
