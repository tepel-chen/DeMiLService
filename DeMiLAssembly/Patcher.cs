using HarmonyLib;
using UnityEngine;

namespace DeMiLService
{
    static class Patcher
    {

        public static bool isActive;

        public static void Patch()
        {
            Debug.Log("[DeMiLService] Starting patch.");
            var harmony = new Harmony("tepel.DeMiLService");
            harmony.PatchAll();
            return;
        }
    }
}
