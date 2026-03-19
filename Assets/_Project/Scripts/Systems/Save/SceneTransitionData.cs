using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Systems
{
    public static class SceneTransitionData
    {
        // Return point — where to go when exiting back to the outer world
        public static string ReturnScene;
        public static Vector3 ReturnPosition;
        public static float ReturnRotationY;
        public static bool HasPendingReturn;

        // Destination spawn — where the player appears in the target scene
        public static Vector3 DestinationPosition;
        public static float DestinationRotationY;
        public static bool HasPendingDestination;

        // Minor Realm — biome and generation seed
        public static BiomeType RealmBiome;
        public static int RealmSeed;
        public static bool IsMinorRealm;

        // Reset static state on every play-mode start (even if Domain Reload is disabled).
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            HasPendingReturn      = false;
            HasPendingDestination = false;
            IsMinorRealm          = false;
            ReturnScene           = null;
            ReturnPosition        = Vector3.zero;
            ReturnRotationY       = 0f;
            DestinationPosition   = Vector3.zero;
            DestinationRotationY  = 0f;
            RealmBiome            = default;
            RealmSeed             = 0;
        }

        public static void SetRealm(BiomeType biome, int seed)
        {
            RealmBiome   = biome;
            RealmSeed    = seed;
            IsMinorRealm = true;
        }

        public static void SetReturn(string scene, Vector3 pos, float rotY)
        {
            ReturnScene = scene;
            ReturnPosition = pos;
            ReturnRotationY = rotY;
            HasPendingReturn = true;
        }

        public static void SetDestination(Vector3 pos, float rotY = 0f)
        {
            DestinationPosition = pos;
            DestinationRotationY = rotY;
            HasPendingDestination = true;
        }

        public static void ClearDestination() => HasPendingDestination = false;
        public static void ClearReturn() => HasPendingReturn = false;
    }
}
