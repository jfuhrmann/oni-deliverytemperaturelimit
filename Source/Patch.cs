using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DeliveryTemperatureLimit
{
    [HarmonyPatch(typeof(FetchManager))]
    public class FetchManager_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(IsFetchablePickup))]
        public static void IsFetchablePickup(ref bool __result, Pickupable pickup, FetchChore chore, Storage destination)
        {
            if( !__result )
                return;
            TemperatureLimits limits = destination.GetComponent< TemperatureLimits >();
            if( limits == null || limits.IsDisabled())
                return;
            __result = limits.AllowedByTemperature( pickup.PrimaryElement.Temperature );
        }
    }

    // Clearable means objects explicitly marked for sweeping. The code apparently does not
    // use IsFetchablePickup() and somehow only compares fetches, so patch it to check too.
    // Class is internal, needs to be patched manually.
    public class ClearableManager_Patch
    {
        public static void Patch( Harmony harmony )
        {
            MethodInfo info = AccessTools.Method( "ClearableManager:CollectChores" );
            if( info != null )
                harmony.Patch( info, transpiler: new HarmonyMethod(
                    typeof( ClearableManager_Patch ).GetMethod( "CollectChores" )));
        }

        public static IEnumerable<CodeInstruction> CollectChores(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            int pickupableLoad = -1;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                if( codes[ i ].opcode == OpCodes.Ldloc_S
                    && codes[ i ].operand.ToString().StartsWith( "Pickupable (" ))
                {
                    pickupableLoad = i;
                }
                // The function has code:
                // if (... && kPrefabID.HasTag(fetch.chore.tagsFirst)))
                // Add:
                // if (... && CollectChores_Hook( fetch.chore, pickupable ))
                // Note that the original code is '(c1 && c2) || (c3 && c4))', so the evaluation
                // of the condition is a bit more complex.
                if( pickupableLoad != -1
                    && codes[ i ].opcode == OpCodes.Ldloc_S && codes[ i ].operand.ToString().StartsWith( "KPrefabID (" )
                    && i + 9 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Ldloc_S
                    && codes[ i + 1 ].operand.ToString().StartsWith( "GlobalChoreProvider+Fetch (" )
                    && codes[ i + 2 ].opcode == OpCodes.Ldfld && codes[ i + 2 ].operand.ToString() == "FetchChore chore"
                    && codes[ i + 3 ].opcode == OpCodes.Ldfld && codes[ i + 3 ].operand.ToString() == "Tag tagsFirst"
                    && codes[ i + 4 ].opcode == OpCodes.Callvirt && codes[ i + 4 ].operand.ToString() == "Boolean HasTag(Tag)"
                    && codes[ i + 5 ].opcode == OpCodes.Br_S
                    && codes[ i + 6 ].opcode == OpCodes.Ldc_I4_0
                    && codes[ i + 7 ].opcode == OpCodes.Br_S
                    && codes[ i + 8 ].opcode == OpCodes.Ldc_I4_1
                    && codes[ i + 9 ].opcode == OpCodes.Brfalse_S )
                {
                    codes.Insert( i + 10, codes[ i + 1 ].Clone());
                    codes.Insert( i + 11, codes[ i + 2 ].Clone()); // load 'fetch.chore'
                    codes.Insert( i + 12, codes[ pickupableLoad ].Clone()); // load 'pickupable'
                    codes.Insert( i + 13, new CodeInstruction( OpCodes.Call,
                        typeof( ClearableManager_Patch ).GetMethod( nameof( CollectChores_Hook ))));
                    codes.Insert( i + 14, codes[ i + 9 ].Clone()); // if false
                    found = true;
                }
            }
            if(!found)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch ClearableManager.CollectChores()");
            return codes;
        }

        public static bool CollectChores_Hook( FetchChore chore, Pickupable pickupable )
        {
            TemperatureLimits limits = chore.destination?.GetComponent< TemperatureLimits >();
            if( limits == null || limits.IsDisabled())
                return true;
            if( pickupable?.PrimaryElement != null )
                return limits.AllowedByTemperature( pickupable.PrimaryElement.Temperature );
            return true;
        }
    }

    // If something to fetch is found, this class tries to find similar objects and add them
    // to the fetch, and it doesn't use IsFetchablePickup(), it only compares the two fetches,
    // so patch the code to check as well.
    [HarmonyPatch(typeof(FetchAreaChore.StatesInstance))]
    public static class FetchAreaChore_StatesInstance_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Begin))]
        public static IEnumerable<CodeInstruction> Begin(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found1 = false;
            bool found2 = false;
            int rootChoreLoad = -1;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                if( codes[ i ].opcode == OpCodes.Ldarg_0 && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Ldfld && codes[ i + 1 ].operand.ToString() == "FetchChore rootChore" )
                {
                    rootChoreLoad = i;
                }
                // The function has code:
                // if (... && rootContext.consumerState.consumer.CanReach(pickupable2))
                // Add:
                // if (... && Begin_Hook1( rootChore, pickupable2 ))
                if( rootChoreLoad != -1 && codes[ i ].opcode == OpCodes.Ldloc_S && i + 2 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Callvirt && codes[ i + 1 ].operand.ToString() == "Boolean CanReach(IApproachable)"
                    && codes[ i + 2 ].opcode == OpCodes.Brfalse_S )
                {
                    codes.Insert( i + 3, codes[ rootChoreLoad ].Clone());
                    codes.Insert( i + 4, codes[ rootChoreLoad + 1 ].Clone()); // load 'rootChore'
                    codes.Insert( i + 5, codes[ i ].Clone()); // load 'pickupable2'
                    codes.Insert( i + 6, new CodeInstruction( OpCodes.Call,
                        typeof( FetchAreaChore_StatesInstance_Patch ).GetMethod( nameof( Begin_Hook1 ))));
                    codes.Insert( i + 7, codes[ i + 2 ].Clone()); // if false
                    found1 = true;
                }
                // The function has code:
                // if (... && fetchChore2.forbidHash == rootChore.forbidHash)
                // Add:
                // if (... && Begin_Hook2( rootChore, fetchChore2 ))
                if( codes[ i ].opcode == OpCodes.Brfalse_S && i + 6 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Ldloc_S && codes[ i + 1 ].operand.ToString().StartsWith( "FetchChore" )
                    && codes[ i + 2 ].opcode == OpCodes.Ldfld && codes[ i + 2 ].operand.ToString() == "System.Int32 forbidHash"
                    && codes[ i + 3 ].opcode == OpCodes.Ldarg_0
                    && codes[ i + 4 ].opcode == OpCodes.Ldfld && codes[ i + 4 ].operand.ToString() == "FetchChore rootChore"
                    && codes[ i + 5 ].opcode == OpCodes.Ldfld && codes[ i + 5 ].operand.ToString() == "System.Int32 forbidHash"
                    && codes[ i + 6 ].opcode == OpCodes.Bne_Un_S )
                {
                    codes.Insert( i + 7, codes[ i + 3 ].Clone());
                    codes.Insert( i + 8, codes[ i + 4 ].Clone()); // load 'rootChore'
                    codes.Insert( i + 9, codes[ i + 1 ].Clone()); // load 'fetchChore2'
                    codes.Insert( i + 10, new CodeInstruction( OpCodes.Call,
                        typeof( FetchAreaChore_StatesInstance_Patch ).GetMethod( nameof( Begin_Hook2 ))));
                    codes.Insert( i + 11, codes[ i ].Clone()); // if false
                    found2 = true;
                }
            }
            if(!found1 || !found2)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch FetchAreaChore.StatesInstance.Begin()");
            return codes;
        }

        public static bool Begin_Hook1( FetchChore rootChore, Pickupable pickupable2 )
        {
            TemperatureLimits limits = rootChore.destination?.GetComponent< TemperatureLimits >();
            if( limits == null || limits.IsDisabled())
                return true;
            return limits.AllowedByTemperature( pickupable2.PrimaryElement.Temperature );
        }

        public static bool Begin_Hook2( FetchChore rootChore, FetchChore fetchChore2 )
        {
            TemperatureLimits limits = rootChore?.destination?.GetComponent< TemperatureLimits >();
            Pickupable pickupable2 = fetchChore2?.fetchTarget;
            if( limits == null || limits.IsDisabled() || pickupable2 == null )
                return true;
            return limits.AllowedByTemperature( pickupable2.PrimaryElement.Temperature );
        }
    }

    [HarmonyPatch(typeof(GlobalChoreProvider))]
    public class GlobalChoreProvider_Patch
    {
        // List of allowed temperature ranges for each tag.
        private class TagData
        {
            // Low/high limit. If not set (null), there's no limit.
            public List< ValueTuple< float, float >> limits;
        }
        // Stored for each GlobalChoreProvider.
        private class PerProviderData
        {
            public Dictionary< Tag, TagData > tagData = new Dictionary< Tag, TagData >();
        }
        private static Dictionary< GlobalChoreProvider, PerProviderData > storageFetchableTagsWithTemperature
            = new Dictionary< GlobalChoreProvider, PerProviderData >();
        // Optimization, look it up just once in the first hook.
        private static PerProviderData currentProvider = null;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ClearableHasDestination))]
        public static void ClearableHasDestination(GlobalChoreProvider __instance, ref bool __result, Pickupable pickupable)
        {
            if( !__result ) // Has no destination already without temperature check.
                return;
            PerProviderData perProvider = storageFetchableTagsWithTemperature[ __instance ];
            if( perProvider == null )
                return;
            KPrefabID kPrefabID = pickupable.KPrefabID;
            TagData tagData;
            if( !perProvider.tagData.TryGetValue( kPrefabID.PrefabTag, out tagData ))
            {
                __result = false; // tag not included => not allowed
                return;
            }
            if( tagData.limits == null ) // All allowed.
                return;
            if( pickupable.PrimaryElement == null )
                return;
            float temperature = pickupable.PrimaryElement.Temperature;
            foreach( ValueTuple< float, float > limit in tagData.limits )
            {
                if( limit.Item1 <= temperature && temperature <= limit.Item2 )
                    return; // ok, found a valid range
            }
            __result = false; // no storage that'd allow the temperature
        }

        // This function updates a hash of allowed tags for ClearableHasDestination.
        // Patch it to build our information that includes temperature limits.
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(UpdateStorageFetchableBits))]
        public static IEnumerable<CodeInstruction> UpdateStorageFetchableBits(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            // Insert 'UpdateStorageFetchableBits_Hook1( this )' at the beginning.
            codes.Insert( 0, new CodeInstruction( OpCodes.Ldarg_0 )); // load 'this'
            codes.Insert( 1, new CodeInstruction( OpCodes.Call,
                typeof( GlobalChoreProvider_Patch ).GetMethod( nameof( UpdateStorageFetchableBits_Hook1 ))));
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // storageFetchableTags.UnionWith(fetchChore.tags);
                // Append:
                // UpdateStorageFetchableBits_Hook2(fetchChore);
                if( codes[ i ].opcode == OpCodes.Ldarg_0
                    && i + 4 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Ldfld
                    && codes[ i + 1 ].operand.ToString().EndsWith( "storageFetchableTags" )
                    && codes[ i + 2 ].opcode == OpCodes.Ldloc_S
                    && codes[ i + 2 ].operand.ToString().StartsWith( "FetchChore (" )
                    && codes[ i + 3 ].opcode == OpCodes.Ldfld
                    && codes[ i + 3 ].operand.ToString().EndsWith( "tags" )
                    && codes[ i + 4 ].opcode == OpCodes.Callvirt
                    && codes[ i + 4 ].operand.ToString().StartsWith( "Void UnionWith(" ))
                {
                    codes.Insert( i + 5, codes[ i + 2 ].Clone()); // load 'fetchChore'
                    codes.Insert( i + 6, new CodeInstruction( OpCodes.Call,
                        typeof( GlobalChoreProvider_Patch ).GetMethod( nameof( UpdateStorageFetchableBits_Hook2 ))));
                    found = true;
                }
            }
            if(!found)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch GlobalChoreProvider.UpdateStorageFetchableBits()");
            return codes;
        }

        public static void UpdateStorageFetchableBits_Hook1(GlobalChoreProvider provider)
        {
            if( !storageFetchableTagsWithTemperature.TryGetValue( provider, out currentProvider ))
            {
                currentProvider = new PerProviderData();
                storageFetchableTagsWithTemperature[ provider ] = currentProvider;
            }
            currentProvider.tagData.Clear();
        }

        public static void UpdateStorageFetchableBits_Hook2(FetchChore chore)
        {
            TemperatureLimits limits = chore.destination.GetComponent< TemperatureLimits >();
            if( limits == null || limits.IsDisabled())
            {
                foreach( Tag tag in chore.tags )
                {
                    TagData tagData;
                    if( !currentProvider.tagData.TryGetValue( tag, out tagData ))
                    {
                        tagData = new TagData(); // limits is set to null
                        currentProvider.tagData[ tag ] = tagData;
                    }
                    else if( tagData.limits != null )
                        tagData.limits = null; // All allowed.
                }
                return;
            }
            foreach( Tag tag in chore.tags )
            {
                TagData tagData;
                if( !currentProvider.tagData.TryGetValue( tag, out tagData ))
                {
                    tagData = new TagData();
                    // We will be adding a limit, so set up the list (which means not all are allowed).
                    tagData.limits = new List< ValueTuple< float, float >>();
                    currentProvider.tagData[ tag ] = tagData;
                }
                if( tagData.limits == null ) // All allowed.
                    continue;
                bool found = false;
                foreach( ValueTuple< float, float > limitItem in tagData.limits )
                {
                    if( limitItem.Item1 <= limits.LowLimit && limits.HighLimit <= limitItem.Item2 )
                    {
                        found = true;
                        break; // ok, included in another range
                    }
                }
                if( !found )
                    tagData.limits.Add( ValueTuple.Create( limits.LowLimit, limits.HighLimit ));
            }
        }
    }
}
