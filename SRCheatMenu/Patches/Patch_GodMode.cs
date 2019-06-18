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
            if (SRCheatMenu.infiniteHealth && PhysicsUtil.IsPlayerMainCollider(collider))
            {
                //SRCheatMenu.Log("DEBUG: KillOnTrigger Player");//DEBUG
                return false;
            }
            return true;
        }
    }
}