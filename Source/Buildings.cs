using HarmonyLib;
using UnityEngine;

namespace DeliveryTemperatureLimit
{
    // Add to all buildings where this makes sense.

    [HarmonyPatch(typeof(StorageLockerSmartConfig))]
    public class StorageLockerSmartConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(StorageLockerConfig))]
    public class StorageLockerConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(ObjectDispenserConfig))]
    public class ObjectDispenserConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(OrbitalCargoModuleConfig))]
    public class OrbitalCargoModuleConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(SolidConduitInboxConfig))]
    public class SolidConduitInboxConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

#if false
    [HarmonyPatch(typeof(BottleEmptierConfig))]
    public class BottleEmptierConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(BottleEmptierGasConfig))]
    public class BottleEmptierGasConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(WaterCoolerConfig))]
    public class WaterCoolerConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(JuicerConfig))]
    public class JuicerConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(SublimationStationConfig))]
    public class SublimationStationConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(AlgaeHabitatConfig))]
    public class AlgaeHabitatConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(WoodGasGeneratorConfig))]
    public class WoodGasGeneratorConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(ResearchCenterConfig))]
    public class ResearchCenterConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(AdvancedResearchCenterConfig))]
    public class AdvancedResearchCenterConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(RustDeoxidizerConfig))]
    public class RustDeoxidizerConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(MechanicalSurfboardConfig))]
    public class MechanicalSurfboardConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(IceMachineConfig))]
    public class IceMachineConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(WashBasinConfig))]
    public class WashBasinConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(FarmStationConfig))]
    public class FarmStationConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(EspressoMachineConfig))]
    public class EspressoMachineConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(OuthouseConfig))]
    public class OuthouseConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(SodaFountainConfig))]
    public class SodaFountainConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(PlanterBoxConfig))]
    public class PlanterBoxConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(CompostConfig))]
    public class CompostConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(AirFilterConfig))]
    public class AirFilterConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(AlgaeDistilleryConfig))]
    public class AlgaeDistilleryConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(WaterPurifierConfig))]
    public class WaterPurifierConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(OxyliteRefineryConfig))]
    public class OxyliteRefineryConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(MineralDeoxidizerConfig))]
    public class MineralDeoxidizerConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(HandSanitizerConfig))]
    public class HandSanitizerConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(FertilizerMakerConfig))]
    public class FertilizerMakerConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(DiningTableConfig))]
    public class DiningTableConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }

    [HarmonyPatch(typeof(CreatureFeederConfig))]
    public class CreatureFeederConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }
 #endif
}
