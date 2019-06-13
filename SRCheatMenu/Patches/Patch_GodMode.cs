using UnityEngine;
using HarmonyLib;

namespace SRCheatMenu.Patches
{
    [HarmonyPatch(typeof(KillOnTrigger))]
    [HarmonyPatch("OnTriggerEnter")]
    class Patch_GodMode
    {
        public static bool Prefix(KillOnTrigger __instance, Collider collider)
        {
            if (SRCheatMenu.infiniteHealth && collider.gameObject == SRSingleton<SceneContext>.Instance.Player)
            {
                //SRCheatMenu.Log("DEBUG: KillOnTrigger Player");//DEBUG
                return false;
            }
            return true;
        }
    }
}