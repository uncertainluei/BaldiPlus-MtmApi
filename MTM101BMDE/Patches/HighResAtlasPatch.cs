using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MTM101BaldAPI.Patches
{
    // TBA
    [HarmonyPatch(typeof(RoomController))]
    [HarmonyPatch("GenerateTextureAtlas")]
    [HarmonyPatch(new Type[0])]
    static class HighResAtlasPatch
    {
    }
}
