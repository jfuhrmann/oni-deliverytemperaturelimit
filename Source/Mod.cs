using HarmonyLib;
using PeterHan.PLib.Core;

namespace DeliveryTemperatureLimit
{
    public class Mod : KMod.UserMod2
    {
        public override void OnLoad( Harmony harmony )
        {
            base.OnLoad( harmony );
            PUtil.InitLibrary( false );
        }
    }
}
