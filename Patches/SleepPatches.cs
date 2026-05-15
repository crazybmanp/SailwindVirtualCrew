using HarmonyLib;

namespace SailwindVirtualCrew
{
    [HarmonyPatch(typeof(Sleep), "FallAsleep")]
    internal static class SleepPatches
    {
        private static void Postfix()
        {
            CrewNavigationCoordinator.Instance.ResetLookoutBellCooldown();
        }
    }
}
