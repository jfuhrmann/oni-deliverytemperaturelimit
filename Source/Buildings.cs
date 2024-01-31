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
               nameof(BottleEmptierConfig),
               nameof(BottleEmptierGasConfig),
               nameof(CreatureFeederConfig),
               nameof(PlanterBoxConfig),
               nameof(FarmTileConfig),
               nameof(HydroponicFarmConfig),
               nameof(AirFilterConfig),
               nameof(WaterPurifierConfig),
               nameof(RockCrusherConfig),
               nameof(OuthouseConfig),
               nameof(SludgePressConfig),
               nameof(SuitFabricatorConfig),
               nameof(MetalRefineryConfig),
               nameof(GlassForgeConfig),
               nameof(SublimationStationConfig),
               nameof(LonelyMinionHouseConfig),
               nameof(ResearchCenterConfig),
#if false
               nameof(WaterCoolerConfig),
               nameof(JuicerConfig),
               nameof(AlgaeHabitatConfig),
               nameof(WoodGasGeneratorConfig),
               nameof(AdvancedResearchCenterConfig),
               nameof(RustDeoxidizerConfig),
               nameof(MechanicalSurfboardConfig),
               nameof(IceMachineConfig),
               nameof(WashBasinConfig),
               nameof(FarmStationConfig),
               nameof(EspressoMachineConfig),
               nameof(SodaFountainConfig),
               nameof(CompostConfig),
               nameof(AlgaeDistilleryConfig),
               nameof(OxyliteRefineryConfig),
               nameof(MineralDeoxidizerConfig),
               nameof(HandSanitizerConfig),
               nameof(FertilizerMakerConfig),
               nameof(DiningTableConfig),

               nameof(MicrobeMusherConfig),
               nameof(ClothingFabricatorConfig),
               nameof(ManualHighEnergyParticleSpawnerConfig),
               nameof(OrbitalResearchCenterConfig),
               nameof(CraftingTableConfig),
               nameof(DiamondPressConfig),
               nameof(ApothecaryConfig),
               nameof(EggCrackerConfig),
               nameof(FossilDigSiteConfig),
               nameof(ClothingAlterationStationConfig),
               nameof(AdvancedApothecaryConfig),
               nameof(GenericFabricatorConfig),
               nameof(GourmetCookingStationConfig),
               nameof(CookingStationConfig),
               nameof(SupermaterialRefineryConfig),
               nameof(MissileFabricatorConfig),
               nameof(UraniumCentrifugeConfig),
               nameof(KilnConfig),
#endif
            };
            foreach( string config in configs )
            {
                MethodInfo info = AccessTools.Method( config + ":DoPostConfigureComplete");
                // HACK: Using prefix, postfix or finalizer randomly(?) makes the game crash,
                // probably a Harmony bug (even enabling 'Harmony.DEBUG = true;' avoids
                // the problem ). Use whatever seems to work.
                if( info != null )
                    harmony.Patch( info, postfix: new HarmonyMethod( typeof( Buildings_Patch ).GetMethod( "DoPostConfigureComplete" )));
                else
                    Debug.LogError( "DeliveryTemperatureLimit: Failed to patch DoPostConfigureComplete() for " + config );
            }

            string[] methods =
            {
                // Move This Here
                "MoveThisHere.HaulingPointConfig",
                // Storage Pod
                "StoragePod.StoragePodConfig",
            };
            foreach( string method in methods )
            {
                MethodInfo info = AccessTools.Method( method + ":DoPostConfigureComplete");
                if( info != null )
                    harmony.Patch( info, postfix: new HarmonyMethod( typeof( Buildings_Patch ).GetMethod( "DoPostConfigureComplete" )));
            }
        }

        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<TemperatureLimit>();
            go.AddOrGet<TemperatureLimits>(); // TODO backwards compatibility
        }
    }
}
