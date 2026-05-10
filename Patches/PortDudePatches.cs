using HarmonyLib;
using UnityEngine;

namespace SailwindVirtualCrew
{
    [HarmonyPatch(typeof(PortDude), "OnTriggerEnter")]
    class PortDudePatches
    {
        [HarmonyPostfix]
        static void Postfix(PortDude __instance, Collider other)
        {
            if (!other.CompareTag("Player")) return;
            VirtualCrewManager.Instance.SetCurrentPort(__instance.GetPort());
        }
    }
}
