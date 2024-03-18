using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

using AmountByTagIndexDict = System.Collections.Generic.Dictionary< ( Tag, int ), float >;

// Update also status items such as 'Building lacks resources'.
// Support both game code and FastTrack.
// FastTrack replaces code both for updating the status itself and for collecting world inventory,
// both of which can be enabled independently, so the code should handle all combinations.
namespace DeliveryTemperatureLimit
{

    public static class StatusItemsUpdaterPatch
    {
        public static void Patch( Harmony harmony )
        {
            if( !Options.Instance.CheckTemperatureForStatusItems )
                return;

            MethodInfo infoRender200ms = AccessTools.Method(
                "FetchListStatusItemUpdater:Render200ms" );
            if( infoRender200ms != null )
                harmony.Patch( infoRender200ms,
                    transpiler: new HarmonyMethod( typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( Render200ms ))));
            else
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to find FetchListStatusItemUpdater.Render200ms().");

            MethodInfo infoUpdateStatus = AccessTools.Method(
                "PeterHan.FastTrack.GamePatches.FetchListStatusItemUpdater_Render200ms_Patch:UpdateStatus" );
            if( infoUpdateStatus != null )
                harmony.Patch( infoUpdateStatus, transpiler: new HarmonyMethod(
                    typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( UpdateStatus ))));

            MethodInfo infoWorldInventoryUpdate = AccessTools.Method( "WorldInventory:Update" );
            if( infoWorldInventoryUpdate != null )
                harmony.Patch( infoWorldInventoryUpdate, transpiler: new HarmonyMethod(
                    typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( WorldInventoryUpdate ))));
            else
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to find WorldInventory.Update().");

            MethodInfo infoRunUpdate = AccessTools.Method(
                "PeterHan.FastTrack.UIPatches.BackgroundWorldInventory:RunUpdate" );
            MethodInfo infoSumTotal = AccessTools.Method(
                "PeterHan.FastTrack.UIPatches.BackgroundWorldInventory:SumTotal" );
            if( infoRunUpdate != null && infoSumTotal != null )
            {
                harmony.Patch( infoRunUpdate, transpiler: new HarmonyMethod(
                    typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( RunUpdate ))));
                harmony.Patch( infoSumTotal, transpiler: new HarmonyMethod(
                    typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( SumTotal ))));
            }
        }

        // Game's FetchListStatusItemUpdater.Render200ms().
        public static IEnumerable<CodeInstruction> Render200ms(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // float minimumAmount = item8.GetMinimumAmount(key);
                // Append:
                // Render200ms_Hook( num3, ref num6, item8, id, key, value2, minimumAmount );
                if( codes[ i ].opcode == OpCodes.Callvirt && codes[ i ].operand.ToString() == "Single GetMinimumAmount(Tag)"
                    && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Stloc_S && codes[ i + 1 ].operand.ToString() == "System.Single (38)" )
                {
                    // Load all the arguments, this of course depends on the exact code (but this patch already in practice
                    // depends on the exact code anyway). The following code uses only 'num6' out of the values that we
                    // change, so that's the only one with a reference.
                    codes.Insert( i + 2, new CodeInstruction( OpCodes.Dup )); // load 'num3', the code leaves it on the stack
                    codes.Insert( i + 3, new CodeInstruction( OpCodes.Ldloca_S, 37 )); // ref 'num6'
                    codes.Insert( i + 4, new CodeInstruction( OpCodes.Ldloc_S, 28 )); // 'item8'
                    codes.Insert( i + 5, new CodeInstruction( OpCodes.Ldloc_1 )); // 'id'
                    codes.Insert( i + 6, new CodeInstruction( OpCodes.Ldloc_S, 33 )); // 'key'
                    codes.Insert( i + 7, new CodeInstruction( OpCodes.Ldloc_S, 34 )); // 'value2'
                    codes.Insert( i + 8, new CodeInstruction( OpCodes.Ldloc_S, 38 )); // 'minimumAmount'
                    codes.Insert( i + 9, new CodeInstruction( OpCodes.Call,
                        typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( Render200ms_Hook ))));
                    found = true;
                    break;
                }
            }
            if(!found)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch FetchListStatusItemUpdater.Render200ms()");
            return codes;
        }

        public static void Render200ms_Hook( float num3, ref float num6, FetchList2 item8, int id, Tag key,
            float value2, float minimumAmount )
        {
            UpdateAvailable( item8, id, key, ref num6, num3, value2, minimumAmount );
        }

        // FastTrack's code for updating status.
        public static IEnumerable<CodeInstruction> UpdateStatus(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // float minimumAmount = errand.GetMinimumAmount(tag);
                // Append:
                // UpdateStatus_Hook( errand, inventory, tag, inStorage, ref fetchable, minimumAmount, remaining );
                if( codes[ i ].opcode == OpCodes.Callvirt && codes[ i ].operand.ToString() == "Single GetMinimumAmount(Tag)"
                    && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Stloc_S && codes[ i + 1 ].operand.ToString() == "System.Single (13)" )
                {
                    // Load all the arguments, this of course depends on the exact code (but this patch already in practice
                    // depends on the exact code anyway).
                    codes.Insert( i + 2, new CodeInstruction( OpCodes.Ldarg_0 )); // 'errand'
                    codes.Insert( i + 3, new CodeInstruction( OpCodes.Ldarg_S, 4 )); // 'inventory'
                    codes.Insert( i + 4, new CodeInstruction( OpCodes.Ldloc_S, 7 )); // 'tag'
                    codes.Insert( i + 5, new CodeInstruction( OpCodes.Ldloca_S, 12 )); // 'fetchable'
                    codes.Insert( i + 6, new CodeInstruction( OpCodes.Ldloc_S, 9 )); // 'inStorage'
                    codes.Insert( i + 7, new CodeInstruction( OpCodes.Ldloc_S, 8 )); // 'remaining'
                    codes.Insert( i + 8, new CodeInstruction( OpCodes.Ldloc_S, 13 )); // 'minimumAmount'
                    codes.Insert( i + 9, new CodeInstruction( OpCodes.Call,
                        typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( UpdateStatus_Hook ))));
                    found = true;
                    break;
                }
            }
            if(!found)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch FetchListStatusItemUpdater_Render200ms_Patch.UpdateStatus()");
            return codes;
        }

        public static void UpdateStatus_Hook( FetchList2 errand, WorldInventory inventory, Tag tag,
            ref float fetchable, float inStorage, float remaining, float minimumAmount )
        {
            UpdateAvailable( errand, inventory.WorldContainer.id, tag, ref fetchable, inStorage, remaining, minimumAmount );
        }

        // Shared code for updating status. Possibly compute again available amount based on temperature
        // of items.
        public static void UpdateAvailable( FetchList2 errand, int worldId, Tag tag,
            ref float fetchable, float inStorage, float remaining, float minimumAmount )
        {
            if( inStorage + fetchable < minimumAmount )
                return; // No need to bother if there are no materials even without considering temperature.
            TemperatureLimit limit = TemperatureLimit.Get( errand.Destination?.gameObject );
            if( limit == null || limit.IsDisabled())
                return;
            TemperatureLimit.TemperatureIndexData data = TemperatureLimit.getTemperatureIndexData();
            ( int lowIndex, int highIndex ) = data.TemperatureIndexes( limit );
            float total = 0;
            // This should already include also sub-worlds, so only sum up amounts
            // for all indexes included in the range.
            // TODO: Would it be worth it to cache this?
            for( int index = lowIndex; index < highIndex; ++index )
            {
                // This is a race condition, as the indexes may change before the world amounts
                // info is updated, so cope with that. The proper value will eventually be calculated.
                try
                {
                    total += worldAmounts[ worldId ][ ( tag, index ) ];
                } catch( KeyNotFoundException )
                {
                }
            }
            // Treat total and available the same. The latter is the sooner, with reserved amounts removed,
            // but the MaterialNeeds class also does not include temperature, and tracking that would be a lot of work
            // for minimal gain. At worst this should result in insufficient resources getting reported with a delay,
            // after that need is served and the total amount also decreases.
            // (And yes, it seems broken to sum available and total, but that's what the game code does.)
            float available = total;
            // And fix the available amount.
            fetchable = available + Mathf.Min(remaining, total);
        }

        // Patch WorldInventory.Update() to track amounts also per temperature index, not just tag.
        public static IEnumerable<CodeInstruction> WorldInventoryUpdate(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found1 = false;
            bool found2 = false;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // num3 = 0;
                // Append:
                // WorldInventoryUpdate_Hook1( num2, key );
                if( codes[ i ].opcode == OpCodes.Ldc_R4 && codes[ i ].operand.ToString() == "0"
                    && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Stloc_S && codes[ i + 1 ].operand.ToString() == "System.Single (5)" )
                {
                    codes.Insert( i + 2, new CodeInstruction( OpCodes.Ldloc_S, 2 )); // load 'num2'
                    codes.Insert( i + 3, new CodeInstruction( OpCodes.Ldloc_S, 4 )); // load 'key'
                    codes.Insert( i + 4, new CodeInstruction( OpCodes.Call,
                        typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( WorldInventoryUpdate_Hook1 ))));
                    found1 = true;
                }
                // The function has code:
                // num3 += item.TotalAmount;
                // Append:
                // WorldInventoryUpdate_Hook2( item, key );
                if( codes[ i ].opcode == OpCodes.Ldloc_S && codes[ i ].operand.ToString() == "Pickupable (7)"
                    && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Callvirt && codes[ i + 1 ].operand.ToString() == "Single get_TotalAmount()" )
                {
                    codes.Insert( i + 1, new CodeInstruction( OpCodes.Dup )); // create a copy of 'item'
                    codes.Insert( i + 2, new CodeInstruction( OpCodes.Ldloc_S, 4 )); // load 'key'
                    codes.Insert( i + 3, new CodeInstruction( OpCodes.Call,
                        typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( WorldInventoryUpdate_Hook2 ))));
                    found2 = true;
                    break;
                }
            }
            if(!found1 || !found2)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch WorldInventory.Update()");
            return codes;
        }

        public static void WorldInventoryUpdate_Hook1( int worldId, Tag key )
        {
            InitializeInventory( worldId, key );
        }

        public static void WorldInventoryUpdate_Hook2( Pickupable item, Tag key )
        {
            UpdateInventory( item, key );
        }

        // FastTrack's world inventory code. RunUpdate() calls into SumTotal() for each world+tag combo.
        public static IEnumerable<CodeInstruction> RunUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            int foundCount = 0;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code (two times):
                // accessibleAmounts[pair.Key] = SumTotal(pair.Value, worldId);
                // Prepend:
                // RunUpdate_Hook( pair.Key, worldId );
                if( codes[ i ].opcode == OpCodes.Ldloc_0
                    && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Call
                    && codes[ i + 1 ].operand.ToString() == "Single SumTotal(System.Collections.Generic.IEnumerable`1[Pickupable], Int32)" )
                {
                    // Both pair.Key and pair.Value are already on the stack, and so getting to pair.Key
                    // requires popping pair.Value and saving it.
                    LocalBuilder localValue = generator.DeclareLocal( typeof( Dictionary<Tag, HashSet<Pickupable>> ));
                    codes.Insert( i, new CodeInstruction( OpCodes.Stloc_S, localValue.LocalIndex )); // store 'pair.Value'
                    codes.Insert( i + 1, new CodeInstruction( OpCodes.Dup )); // create a copy of 'pair.Key'
                    codes.Insert( i + 2, codes[ i + 2 ].Clone()); // load 'worldId'
                    codes.Insert( i + 3, new CodeInstruction( OpCodes.Call,
                        typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( RunUpdate_Hook ))));
                    // Now 'pair.Key' is still on the stack, load 'pair.Value', and 'worldId' will be loaded by the original code.
                    codes.Insert( i + 4, new CodeInstruction( OpCodes.Ldloc_S, localValue.LocalIndex ));
                    ++foundCount;
                    i = i + 5; // Move past the generated code to the first original instruction in order to avoid an infinite loop.
                }
            }
            if( foundCount != 2 )
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch BackgroundWorldInventory.RunUpdate()");
            return codes;
        }

        // The code is in a thread, but it itself is running as a single thread, so static should be fine.
        private static Tag currentKey;

        public static void RunUpdate_Hook( Tag key, int worldId )
        {
            currentKey = key;
            InitializeInventory( worldId, key );
        }

        public static IEnumerable<CodeInstruction> SumTotal(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code (two times):
                // total += pickupable.TotalAmount;
                // Append:
                // SumTotal_Hook( pickupable );
                if( codes[ i ].opcode == OpCodes.Ldloc_S && codes[ i ].operand.ToString() == "Pickupable (4)"
                    && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Callvirt && codes[ i + 1 ].operand.ToString() == "Single get_TotalAmount()" )
                {
                    codes.Insert( i + 1, new CodeInstruction( OpCodes.Dup )); // create a copy of 'pickupable'
                    codes.Insert( i + 2, new CodeInstruction( OpCodes.Call,
                        typeof( StatusItemsUpdaterPatch ).GetMethod( nameof( SumTotal_Hook ))));
                    found = true;
                    break;
                }
            }
            if( !found )
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch BackgroundWorldInventory.SumTotal()");
            return codes;
        }

        public static void SumTotal_Hook( Pickupable pickupable )
        {
            UpdateInventory( pickupable, currentKey );
        }

        // Shared code for keeping track of amounts for each world+tag+temperature combo.
        private static Dictionary< int, AmountByTagIndexDict > worldAmounts = new Dictionary< int, AmountByTagIndexDict >();
        private static AmountByTagIndexDict currentAmounts;

        public static void InitializeInventory( int worldId, Tag key )
        {
            if( !worldAmounts.TryGetValue( worldId, out currentAmounts ))
            {
                currentAmounts = new AmountByTagIndexDict();
                worldAmounts[ worldId ] = currentAmounts;
            }
            TemperatureLimit.TemperatureIndexData data = TemperatureLimit.getTemperatureIndexData();
            for( int i = 0; i <= data.MaxTemperatureIndex(); ++i )
                currentAmounts[ ( key, i ) ] = 0;
        }

        public static void UpdateInventory( Pickupable item, Tag key )
        {
            if( item.PrimaryElement == null )
                return;
            TemperatureLimit.TemperatureIndexData data = TemperatureLimit.getTemperatureIndexData();
            currentAmounts[ ( key, data.TemperatureIndex( item.PrimaryElement.Temperature )) ] += item.TotalAmount;
        }
    }
}
