using HarmonyLib;
using KSerialization;
using UnityEngine;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using TMPro;
using System;

namespace DeliveryTemperatureLimit
{
    public class TemperatureLimits : KMonoBehaviour
    {
        [Serialize]
        private float lowLimit = 0; // 0 Kelvin

        [Serialize]
        private float highLimit = 0; // if 0, then not active

        public float MinValue => 0f;

        public float MaxValue => 5000f; // diamond melts at ~4200K

        private static readonly EventSystem.IntraObjectHandler<TemperatureLimits> OnCopySettingsDelegate
            = new EventSystem.IntraObjectHandler<TemperatureLimits>(delegate(TemperatureLimits component, object data)
        {
            component.OnCopySettings(data);
        });

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        private void OnCopySettings(object data)
        {
            TemperatureLimits component = ((GameObject)data).GetComponent<TemperatureLimits>();
            if (component != null)
            {
                lowLimit = component.lowLimit;
                highLimit = component.highLimit;
            }
        }

        public bool IsDisabled() => ( highLimit == 0 );
        public float LowLimit => lowLimit;
        public float HighLimit => highLimit;

        public void SetLowLimit(float value)
        {
            lowLimit = Mathf.Max( value, MinValue );
        }

        public void SetHighLimit(float value)
        {
            highLimit = Mathf.Min( value, MaxValue );
        }

        public void Disable()
        {
            highLimit = 0;
        }

        public bool AllowedByTemperature( float temperature )
        {
            if( highLimit == 0 ) // limit disabled
                return true;
            return lowLimit <= temperature && temperature <= highLimit;
        }
    }

        [HarmonyPatch(typeof(DetailsScreen))]
        [HarmonyPatch("OnPrefabInit")]
        public static class DetailsScreen_OnPrefabInit_Patch
        {
            public static void Postfix()
            {
                PUIUtils.AddSideScreenContent<TemperatureLimitsSideScreen>();
            }
        }


    public class TemperatureLimitsSideScreen : SideScreenContent
    {
        private GameObject lowInput;

        private GameObject highInput;

        private TemperatureLimits target;

        protected override void OnPrefabInit()
        {
            var margin = new RectOffset(6, 6, 6, 6);
            var baseLayout = gameObject.GetComponent<BoxLayoutGroup>();
            if (baseLayout != null)
                baseLayout.Params = new BoxLayoutParams()
                {
                    Alignment = TextAnchor.MiddleLeft,
                    Margin = margin,
                };
            PPanel panel = new PPanel("MainPanel")
            {
                Direction = PanelDirection.Horizontal,
                Margin = margin,
                Spacing = 8,
                FlexSize = Vector2.right
            };
            PTextField lowInputField = new PTextField( "lowLimit" )
            {
                    Type = PTextField.FieldType.Float,
                    OnTextChanged = OnTextChangedLow,
                    ToolTip = "a"
            };
            lowInputField.SetMinWidthInCharacters(6);
            lowInputField.AddOnRealize((obj) => lowInput = obj);
            PTextField highInputField = new PTextField( "highLimit" )
                {
                    Type = PTextField.FieldType.Float,
                    OnTextChanged = OnTextChangedHigh,
                    ToolTip = "a"
                };
            highInputField.SetMinWidthInCharacters(6);
            highInputField.AddOnRealize((obj) => highInput = obj);
            PLabel label = new PLabel( "label" )
                {
                    TextStyle = PUITuning.Fonts.TextDarkStyle,
                    Text = STRINGS.TEMPERATURELIMITS.LABEL
                };
            PLabel separator = new PLabel( "separator" )
                {
                    TextStyle = PUITuning.Fonts.TextDarkStyle,
                    Text = STRINGS.TEMPERATURELIMITS.RANGE_SEPARATOR
                };
            panel.AddChild( label );
            panel.AddChild( lowInputField );
            panel.AddChild( separator );
            panel.AddChild( highInputField );
            panel.AddTo( gameObject );
            ContentContainer = gameObject;
            base.OnPrefabInit();
            UpdateInputs();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target.GetComponent<TemperatureLimits>() != null;
        }

        public override void SetTarget(GameObject new_target)
        {
            if (new_target == null)
            {
                Debug.LogError("Invalid gameObject received");
                return;
            }
            target = new_target.GetComponent<TemperatureLimits>();
            if (target == null)
            {
                Debug.LogError("The gameObject received does not contain a TemperatureLimits component");
                return;
            }
            UpdateInputs();
        }

        private void UpdateInputs()
        {
            if( target == null || lowInput == null )
                return;
            if( target.IsDisabled())
                EmptyInputs();
            else
            {
                SetLowValue( target.LowLimit );
                SetHighValue( target.HighLimit );
            }
        }

        private void OnTextChangedLow(GameObject source, string text)
        {
            if( target.IsDisabled())
                SetHighValue( target.MaxValue ); // fill in a value in the other one
            float value = OnTextChanged( text, (float v) => SetLowValue( v ), target.MinValue );
            if( value != -1 && value > target.HighLimit )
                SetHighValue( value );
        }

        private void OnTextChangedHigh(GameObject source, string text)
        {
            if( target.IsDisabled())
                SetLowValue( target.MinValue ); // fill in a value in the other one
            float value = OnTextChanged( text, (float v) => SetHighValue( v ), target.MaxValue );
            if( value != -1 && value < target.LowLimit )
                SetLowValue( value );
        }

        private float OnTextChanged( string text, Action< float > setValueFunc, float fallback )
        {
            text = text.Trim();
            if( string.IsNullOrEmpty( text ))
            {
                target.Disable();
                EmptyInputs();
                return -1;
            }
            // TryParse() can't handle extra text at the end (temperature unit),
            // so strip it if it's there
            if( text.EndsWith( GameUtil.GetTemperatureUnitSuffix()))
                text = text.Remove( text.Length - GameUtil.GetTemperatureUnitSuffix().Length );
            float result;
            if(float.TryParse(text, out result))
                result = GameUtil.GetTemperatureConvertedToKelvin(result);
            else
                result = fallback;
            setValueFunc( result );
            return result;
        }

        private void SetLowValue( float value )
        {
            SetValue( value, lowInput,
                (float v) => target.SetLowLimit( v ),
                () => target.LowLimit                );
        }

        private void SetHighValue( float value )
        {
            SetValue( value, highInput,
                (float v) => target.SetHighLimit( v ),
                () => target.HighLimit                );
        }

        private void SetValue( float value, GameObject input, Action< float > setTargetFunc,
            Func< float > targetValueFunc )
        {
            setTargetFunc( value );
            value = targetValueFunc(); // maybe clamped, so re-read
            TMP_InputField field = input.GetComponent< TMP_InputField >();
            string text = GameUtil.GetFormattedTemperature(value, GameUtil.TimeSlice.None,
                GameUtil.TemperatureInterpretation.Absolute, true);
            if( field.text != text )
                field.text = text;
        }

        private void EmptyInputs()
        {
            Action< TMP_InputField > resetInput = (TMP_InputField field) =>
            {
                if( !string.IsNullOrEmpty( field.text ))
                    field.text = "";
            };
            resetInput( lowInput.GetComponent< TMP_InputField >());
            resetInput( highInput.GetComponent< TMP_InputField >());
        }
    }

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

    // Now add to all buildings where this makes sense.
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
}
