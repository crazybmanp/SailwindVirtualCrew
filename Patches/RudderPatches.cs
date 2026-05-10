using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SailwindVirtualCrew.Patches
{
    internal static class RudderPatches
    {
        // No longer needed, just for simple testing
        /*
        [HarmonyPatch(typeof(GPButtonRopeWinch), "LimitInput")]
        public static class GPButtonRopeWinchLimitInputPatch
        {
            [HarmonyPrefix]
            public static void LimitInput(ref float ___currentInput)
            {
                if(VirtualCrewManager.Instance.isCrewActive)
                {
                    ___currentInput = -100;
                }
            }
        }*/
        /*
        [HarmonyPatch(typeof(Rudder), "ApplyCenteringForce")]
        public static class RudderApplyCenteringForcePatch
        {
            [HarmonyPrefix]
            public static bool ApplyCenteringForce()
            {
                return false;
            }

        }
        */
    }
}
