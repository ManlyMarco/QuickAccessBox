using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.Text;

namespace KK_QuickAccessBox
{
    internal class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.AddNode))]
        private static void AddNodePostFix(ref TreeNodeObject __result)
        {
            QuickAccessBox.SpawnedNode = __result;
        }
    }
}
