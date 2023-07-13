using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

// FastTrack mod replaces some of the game code, patch it too if present.
namespace DeliveryTemperatureLimit
{
    public class ChoreComparator_Patch
    {
        public static void Patch( Harmony harmony )
        {
            MethodInfo info = AccessTools.Method( "PeterHan.FastTrack.GamePatches.ChoreComparator:CheckFetchChore" );
            if( info != null )
                harmony.Patch( info, transpiler: new HarmonyMethod(
                    typeof( ChoreComparator_Patch ).GetMethod( nameof( CheckFetchChore ))));
        }

        public static IEnumerable<CodeInstruction> CheckFetchChore(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            int pickupableLoad = -1;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                if( codes[ i ].opcode == OpCodes.Ldfld
                    && codes[ i ].operand.ToString().StartsWith( "Pickupable pickupable" )
                    && i + 2 < codes.Count
                    && codes[ i + 1 ].IsStloc()
                    && codes[ i + 2 ].IsLdloc())
                {
                    pickupableLoad = i + 2;
                }
                // The function has code:
                // if ( ... && FastCheckPreconditions(chore, transport))
                // Change to:
                // if (... && CheckFetchChore_Hook( chore, pickupable ) && FastCheckPreconditions(...))
                // Note that the original code is '(c1 && c2) || (c3 && c4))', so the evaluation
                // of the condition is a bit more complex.
                if( pickupableLoad != -1
                    && codes[ i ].opcode == OpCodes.Ldarg_0
                    && i + 4 < codes.Count
                    && codes[ i + 1 ].IsLdarg()
                    && codes[ i + 2 ].opcode == OpCodes.Ldsfld
                    && codes[ i + 2 ].operand.ToString() == "ChoreType transport"
                    && codes[ i + 3 ].opcode == OpCodes.Call
                    && codes[ i + 3 ].operand.ToString() == "Boolean FastCheckPreconditions(Chore, ChoreType)"
                    && codes[ i + 4 ].opcode == OpCodes.Brfalse_S )
                {
                    CodeInstruction loadChore = codes[ i + 1 ].Clone();
                    CodeInstruction brfalse = codes[ i + 4 ].Clone();
                    // The original code has a label at the first instruction (because of the more complex
                    // condition), this code should be included, so reuse the first instruction even
                    // though it's useless, and create a duplicate of it at the end to start the original code.
                    codes.Insert( i + 1, new CodeInstruction( OpCodes.Pop )); // drop the useless 'this'
                    codes.Insert( i + 2, loadChore ); // load 'chore'
                    codes.Insert( i + 3, codes[ pickupableLoad ].Clone()); // load 'pickupable'
                    codes.Insert( i + 4, new CodeInstruction( OpCodes.Call,
                        typeof( ChoreComparator_Patch ).GetMethod( nameof( CheckFetchChore_Hook ))));
                    codes.Insert( i + 5, brfalse ); // if false
                    codes.Insert( i + 6, new CodeInstruction( OpCodes.Ldarg_0 )); // the original first instruction
                    found = true;
                    break;
                }
            }
            if(!found)
                Debug.LogWarning("DeliveryTemperatureLimit: Failed to patch FastTrack ChoreComparator:CheckFetchChore()");
            return codes;
        }

        public static bool CheckFetchChore_Hook( FetchChore chore, Pickupable pickupable )
        {
            TemperatureLimit limit = TemperatureLimit.Get( chore.destination?.gameObject );
            if( limit == null || limit.IsDisabled())
                return true;
            if( pickupable?.PrimaryElement != null )
                return limit.AllowedByTemperature( pickupable.PrimaryElement.Temperature );
            return true;
        }
    }
}
