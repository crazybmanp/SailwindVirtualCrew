using HarmonyLib;
using UnityEngine;

namespace SailwindVirtualCrew.Patches
{
    internal static class GPButtonRopeWinchPatches
    {
        private static void InvokeMethod(object obj, string methodName)
            => AccessTools.Method(obj.GetType(), methodName).Invoke(obj, null);

        private static void ApplyWinchMotion(GPButtonRopeWinch instance, LineRenderer ropeEffect)
        {
            InvokeMethod(instance, "LimitInput");
            InvokeMethod(instance, "ApplyWindResistance");
            InvokeMethod(instance, "LimitTurning");
            InvokeMethod(instance, "AddQuickReleaseInput");
            InvokeMethod(instance, "ApplyRotation");
            InvokeMethod(instance, "GetRotationDelta");
            InvokeMethod(instance, "DeltaToLength");
            InvokeMethod(instance, "SendLengthToRope");
            if ((bool)ropeEffect)
                InvokeMethod(instance, "UpdateMaterial");
        }

        [HarmonyPatch(typeof(GPButtonRopeWinch), "Update")]
        public static class GPButtonRopeWinchUpdatePatch
        {
            [HarmonyPrefix]
            public static bool Update(GPButtonRopeWinch __instance, LineRenderer ___ropeEffect, ref float ___currentInput)
            {
                var manager = VirtualCrewManager.Instance;

                if (manager.isCrewActive)
                {
                    if (manager.winchInstructions.ContainsKey(__instance))
                    {
                        ___currentInput = manager.winchInstructions[__instance];
                        ApplyWinchMotion(__instance, ___ropeEffect);
                    }
                    return false;
                }

                // Individual crew work requests drive specific winches even when
                // global crew mode is off, letting the player control all other winches.
                // GetPower() is called every frame so the P controller output updates
                // continuously rather than being held fixed between ticks.
                if (manager.crewWinchInstructions.TryGetValue(__instance, out WinchTarget target))
                {
                    ___currentInput = target.GetPower();
                    ApplyWinchMotion(__instance, ___ropeEffect);
                    return false;
                }

                return true;
            }
        }
    }
}
