using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using PeterHan.PLib.Options;

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
            TemperatureLimit limit = TemperatureLimit.Get( destination.gameObject );
            if( limit == null || limit.IsDisabled())
                return;
            __result = limit.AllowedByTemperature( pickup.PrimaryElement.Temperature );
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
                    typeof( ClearableManager_Patch ).GetMethod( nameof( CollectChores ))));
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
                    break;
                }
            }
            if(!found)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch ClearableManager.CollectChores()");
            return codes;
        }

        public static bool CollectChores_Hook( FetchChore chore, Pickupable pickupable )
        {
            TemperatureLimit limit = TemperatureLimit.Get( chore.destination?.gameObject );
            if( limit == null || limit.IsDisabled())
                return true;
            if( pickupable?.PrimaryElement != null )
                return limit.AllowedByTemperature( pickupable.PrimaryElement.Temperature );
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
            TemperatureLimit limit = TemperatureLimit.Get( rootChore.destination?.gameObject );
            if( limit == null || limit.IsDisabled())
                return true;
            return limit.AllowedByTemperature( pickupable2.PrimaryElement.Temperature );
        }

        public static bool Begin_Hook2( FetchChore rootChore, FetchChore fetchChore2 )
        {
            // This checks whether the second chore can be handled as a part of the root chore.
            // Therefore add a check if the second chore's range is compatible.
            TemperatureLimit limit = TemperatureLimit.Get( rootChore?.destination?.gameObject );
            TemperatureLimit limit2 = TemperatureLimit.Get( fetchChore2?.destination?.gameObject );
            if( limit == limit2 || limit2 == null || limit2.IsDisabled())
                return true;
            if( limit == null )
                return false; // by now limit2 is a valid range
            return limit2.LowLimit >= limit.LowLimit && limit2.HighLimit <= limit.HighLimit;
        }
    }

    [HarmonyPatch(typeof(GlobalChoreProvider))]
    public class GlobalChoreProvider_Patch
    {
        // List of allowed temperature ranges for each tag.
        private class TagData
        {
            // Low/high limit. If not set (null), there's no limit.
            public List< ValueTuple< int, int >> limits;
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
            int temperature = (int)pickupable.PrimaryElement.Temperature;
            foreach( ValueTuple< int, int > limit in tagData.limits )
            {
                if( limit.Item1 <= temperature && temperature < limit.Item2 )
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
                    break;
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
            TemperatureLimit limit = TemperatureLimit.Get( chore.destination.gameObject );
            if( limit == null || limit.IsDisabled())
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
                    tagData.limits = new List< ValueTuple< int, int >>();
                    currentProvider.tagData[ tag ] = tagData;
                }
                if( tagData.limits == null ) // All allowed.
                    continue;
                bool found = false;
                foreach( ValueTuple< int, int > limitItem in tagData.limits )
                {
                    if( limitItem.Item1 <= limit.LowLimit && limit.HighLimit < limitItem.Item2 )
                    {
                        found = true;
                        break; // ok, included in another range
                    }
                }
                if( !found )
                    tagData.limits.Add( ValueTuple.Create( limit.LowLimit, limit.HighLimit ));
            }
        }
    }

    // FetchManager keeps a list of available Pickupable's, and (for presumably performance reasons)
    // it sorts them by tag+priority+cost, and then keeps only the cheapest one for each tag+priority.
    // This needs to be changed to keep one for each tag+priority+temperatureindex, otherwise
    // the game wouldn't find a further pickupable with suitable temperature if there would be
    // a closer one with an unsuitable temperature.
    [HarmonyPatch(typeof(FetchManager.FetchablesByPrefabId))]
    public class FetchManager_FetchablesByPrefabId_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(UpdatePickups))]
        public static IEnumerable<CodeInstruction> UpdatePickups(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            int pickupLoad = -1;
            int pickup2Load = -1;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                if( i > 0 && codes[ i ].opcode == OpCodes.Ldfld && codes[ i ].operand.ToString() == "System.Int32 tagBitsHash" )
                {
                    if( pickupLoad < 0 )
                        pickupLoad = i - 1;
                    else
                        pickup2Load = i - 1;
                }
                // The function has code:
                // if (pickup.masterPriority == pickup2.masterPriority && tagBitsHash == num)
                // Change to:
                // if (.. && tagBitsHash == num
                //     && UpdatePickups_Hook( pickup, pickup2 ) == 1 )
                if( codes[ i ].opcode == OpCodes.Ldfld && codes[ i ].operand.ToString() == "System.Int32 masterPriority"
                    && i + 4 < codes.Count && pickupLoad != -1 && pickup2Load != -1
                    && codes[ i + 1 ].opcode == OpCodes.Bne_Un_S
                    && codes[ i + 2 ].opcode == OpCodes.Ldloc_S
                    && CodeInstructionExtensions.IsLdloc( codes[ i + 3 ] )
                    && codes[ i + 4 ].opcode == OpCodes.Bne_Un_S )
                {
                    codes.Insert( i + 5, codes[ pickupLoad ].Clone()); // load 'pickup'
                    codes.Insert( i + 6, codes[ pickup2Load ].Clone()); // load 'pickup2'
                    codes.Insert( i + 7, new CodeInstruction( OpCodes.Call,
                        typeof( FetchManager_FetchablesByPrefabId_Patch ).GetMethod( nameof( UpdatePickups_Hook ))));
                    codes.Insert( i + 8, new CodeInstruction( OpCodes.Ldc_I4_1 )); // load '1' (so that the == test can be reused)
                    codes.Insert( i + 9, codes[ i + 4 ].Clone()); // if not equal
                    found = true;
                    break;
                }
            }
            if(!found)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch FetchManager.FetchablesByPrefabId.UpdatePickups()");
            return codes;
        }

        public static int UpdatePickups_Hook( FetchManager.Pickup pickup, FetchManager.Pickup pickup2 )
        {
            TemperatureLimit.TemperatureIndexData data = TemperatureLimit.getTemperatureIndexData();
            return data.TemperatureIndex( pickup.pickupable.PrimaryElement.Temperature )
                 == data.TemperatureIndex( pickup2.pickupable.PrimaryElement.Temperature ) ? 1 : 0;
        }
    }

    public class FetchManager_PickupComparerIncludingPriority_Patch
    {
        // The class is private, so patch manually.
        public static void Patch( Harmony harmony )
        {
            MethodInfo info = AccessTools.Method( "FetchManager.PickupComparerIncludingPriority:Compare");
            if( info == null ) // For some reason the name has to use '+' instead of '.' for the private class.
                info = AccessTools.Method( "FetchManager+PickupComparerIncludingPriority:Compare");
            if( info != null )
                harmony.Patch( info, transpiler: new HarmonyMethod(
                    typeof( FetchManager_PickupComparerIncludingPriority_Patch ).GetMethod( nameof( Compare ))));
            else
                Debug.LogError( "DeliveryTemperatureLimit: Failed to find"
                    + " FetchManager.PickupComparerIncludingPriority.Compare() for patching" );
        }

        public static IEnumerable<CodeInstruction> Compare(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // num = a.masterPriority.CompareTo(b.masterPriority);
                // if (num != 0)
                //    return num;
                // Append:
                // num = Compare_Hook( a, b );
                // if (num != 0)
                //     return num;
                if( codes[ i ].opcode == OpCodes.Ldarga_S && codes[ i ].operand.ToString() == "2"
                    && i + 9 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Ldflda && codes[ i + 1 ].operand.ToString() == "System.Int32 masterPriority"
                    && codes[ i + 2 ].opcode == OpCodes.Ldarg_1
                    && codes[ i + 3 ].opcode == OpCodes.Ldfld && codes[ i + 3 ].operand.ToString() == "System.Int32 masterPriority"
                    && codes[ i + 4 ].opcode == OpCodes.Call && codes[ i + 4 ].operand.ToString() == "Int32 CompareTo(Int32)"
                    && CodeInstructionExtensions.IsStloc( codes[ i + 5 ] )
                    && CodeInstructionExtensions.IsLdloc( codes[ i + 6 ] )
                    && codes[ i + 7 ].opcode == OpCodes.Brfalse_S
                    && CodeInstructionExtensions.IsLdloc( codes[ i + 8 ] )
                    && codes[ i + 9 ].opcode == OpCodes.Ret )
                {
                    codes.Insert( i + 10, new CodeInstruction( OpCodes.Ldarg_1 )); // load 'a'
                    codes.Insert( i + 11, new CodeInstruction( OpCodes.Ldarg_2 )); // load 'b'
                    codes.Insert( i + 12, new CodeInstruction( OpCodes.Call,
                        typeof( FetchManager_PickupComparerIncludingPriority_Patch ).GetMethod( nameof( Compare_Hook ))));
                    codes.Insert( i + 13, codes[ i + 5 ].Clone()); // stloc
                    codes.Insert( i + 14, codes[ i + 6 ].Clone()); // ldloc
                    codes.Insert( i + 15, codes[ i + 7 ].Clone()); // brfalse
                    codes.Insert( i + 16, codes[ i + 8 ].Clone()); // ldloc
                    codes.Insert( i + 17, codes[ i + 9 ].Clone()); // ret
                    found = true;
                    break;
                }
            }
            if(!found)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch FetchManager.PickupComparerIncludingPriority.Compare()");
            return codes;
        }

        public static int Compare_Hook( FetchManager.Pickup a, FetchManager.Pickup b )
        {
            TemperatureLimit.TemperatureIndexData data = TemperatureLimit.getTemperatureIndexData();
            return data.TemperatureIndex( a.pickupable.PrimaryElement.Temperature )
                .CompareTo( data.TemperatureIndex( b.pickupable.PrimaryElement.Temperature ));
        }

// ONI's Harmony is too old to have LocalLocal() and StoreLocal(), so copy&paste from Harmony.
/*
MIT License

Copyright (c) 2017 Andreas Pardeike

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
        public static CodeInstruction LoadLocal(int index, bool useAddress = false)
        {
            if (useAddress)
            {
                if (index < 256) return new CodeInstruction(OpCodes.Ldloca_S, Convert.ToByte(index));
                else return new CodeInstruction(OpCodes.Ldloca, index);
            }
            else
            {
                if (index == 0) return new CodeInstruction(OpCodes.Ldloc_0);
                else if (index == 1) return new CodeInstruction(OpCodes.Ldloc_1);
                else if (index == 2) return new CodeInstruction(OpCodes.Ldloc_2);
                else if (index == 3) return new CodeInstruction(OpCodes.Ldloc_3);
                else if (index < 256) return new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(index));
                else return new CodeInstruction(OpCodes.Ldloc, index);
            }
        }
        public static CodeInstruction StoreLocal(int index)
        {
            if (index == 0) return new CodeInstruction(OpCodes.Stloc_0);
            else if (index == 1) return new CodeInstruction(OpCodes.Stloc_1);
            else if (index == 2) return new CodeInstruction(OpCodes.Stloc_2);
            else if (index == 3) return new CodeInstruction(OpCodes.Stloc_3);
            else if (index < 256) return new CodeInstruction(OpCodes.Stloc_S, Convert.ToByte(index));
            else return new CodeInstruction(OpCodes.Stloc, index);
        }
    }
}
