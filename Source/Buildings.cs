using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace DeliveryTemperatureLimit
{
    // Add to all buildings where this makes sense.
    public class Buildings_Patch
    {
        public static void Patch( Harmony harmony )
        {
            string[] configs =
            {
               nameof(StorageLockerSmartConfig),
               nameof(StorageLockerConfig),
               nameof(ObjectDispenserConfig),
               nameof(OrbitalCargoModuleConfig),
               nameof(SolidConduitInboxConfig),
#if false
               nameof(BottleEmptierConfig),
               nameof(BottleEmptierGasConfig),
               nameof(WaterCoolerConfig),
               nameof(JuicerConfig),
               nameof(SublimationStationConfig),
               nameof(AlgaeHabitatConfig),
               nameof(WoodGasGeneratorConfig),
               nameof(ResearchCenterConfig),
               nameof(AdvancedResearchCenterConfig),
               nameof(RustDeoxidizerConfig),
               nameof(MechanicalSurfboardConfig),
               nameof(IceMachineConfig),
               nameof(WashBasinConfig),
               nameof(FarmStationConfig),
               nameof(EspressoMachineConfig),
               nameof(OuthouseConfig),
               nameof(SodaFountainConfig),
               nameof(PlanterBoxConfig),
               nameof(CompostConfig),
               nameof(AirFilterConfig),
               nameof(AlgaeDistilleryConfig),
               nameof(WaterPurifierConfig),
               nameof(OxyliteRefineryConfig),
               nameof(MineralDeoxidizerConfig),
               nameof(HandSanitizerConfig),
               nameof(FertilizerMakerConfig),
               nameof(DiningTableConfig),
               nameof(CreatureFeederConfig),
#endif
            };
            foreach( string config in configs )
            {
                MethodInfo info = AccessTools.Method( config + ":DoPostConfigureComplete");
                if( info != null )
                    harmony.Patch( info, prefix: new HarmonyMethod( typeof( Buildings_Patch ).GetMethod( "DoPostConfigureComplete" )));
                else
                    Debug.LogError( "DeliveryTemperatureLimit: Failed to patch DoPostConfigureComplete() for " + config );
            }
        }

        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimits>();
        }
    }
}
