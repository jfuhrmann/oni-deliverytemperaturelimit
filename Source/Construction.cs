using HarmonyLib;
using UnityEngine;
using PeterHan.PLib.UI;
using System;

namespace DeliveryTemperatureLimit
{
    public static class Construction
    {
        // There is just one MaterialSelectionPanel instance, so keep one limit instance.
        public static TemperatureLimit limit = null;

        public static void ResetConstructionLimit()
        {
            if( !Options.Instance.UnderConstructionLimit || limit == null )
                return;
            limit.SetLowLimit( (int) Math.Round( GameUtil.GetTemperatureConvertedToKelvin(
                Options.Instance.MinConstructionTemperature, GameUtil.temperatureUnit )));
            limit.SetHighLimit( (int) Math.Round( GameUtil.GetTemperatureConvertedToKelvin(
                Options.Instance.MaxConstructionTemperature, GameUtil.temperatureUnit )));
        }
    }

    [HarmonyPatch(typeof(MaterialSelectionPanel))]
    public class MaterialSelectionPanel_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(OnPrefabInit))]
        public static void OnPrefabInit(MaterialSelectionPanel __instance)
        {
            if( !Options.Instance.UnderConstructionLimit )
                return;
            // Create and set the singleton instance.
            Construction.limit = __instance.gameObject.AddOrGet<TemperatureLimit>();
            Construction.ResetConstructionLimit();
            TemperatureLimitWidget widget = __instance.gameObject.AddOrGet<TemperatureLimitWidget>();
            widget.SetTarget( Construction.limit );
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ConfigureScreen))]
        public static void ConfigureScreen(MaterialSelectionPanel __instance)
        {
            if( !Options.Instance.UnderConstructionLimit )
                return;
            TemperatureLimitWidget widget = __instance.GetComponent<TemperatureLimitWidget>();
            widget.SetTarget( Construction.limit );
        }

    }

    // Reset the limit to the configured default whenever the build priority is reset.
    // It seems that there is just one priority screen instance shared by everything,
    // so reset when that is reset.
    [HarmonyPatch(typeof(PriorityScreen))]
    public class PriorityScreen_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ResetPriority))]
        public static void ResetPriority()
        {
            Construction.ResetConstructionLimit();
        }
    }

    [HarmonyPatch(typeof(BuildingDef))]
    public class BuildingDef_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Instantiate))]
        public static void Instantiate(BuildingDef __instance, GameObject __result)
        {
            if( !Options.Instance.UnderConstructionLimit || Construction.limit == null )
                return;
            __result.AddOrGet<TemperatureLimit>().CopySettings( Construction.limit );
        }
    }
}
