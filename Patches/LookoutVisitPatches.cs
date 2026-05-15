using HarmonyLib;
using UnityEngine;

namespace SailwindVirtualCrew
{
    [HarmonyPatch(typeof(IslandMarketWarehouseArea), "OnTriggerEnter")]
    internal static class LookoutVisitPatches
    {
        private static void Postfix(IslandMarket ___market, Collider other)
        {
            if (___market == null || other == null || !other.CompareTag("Player"))
                return;

            VirtualCrewManager.Instance.RegisterVisitedPort(___market.GetPortName());
        }
    }
}
