using HarmonyLib;
using UnityEngine;
using System;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using TMPro;
using STRINGS;

namespace DeliveryTemperatureLimit
{
    public class TemperatureLimitSideScreen : SideScreenContent
    {
        private GameObject lowInput;

        private GameObject highInput;

        private TemperatureLimit target;

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
                    Type = PTextField.FieldType.Integer,
                    OnTextChanged = OnTextChangedLow,
            };
            lowInputField.SetMinWidthInCharacters(6);
            lowInputField.AddOnRealize((obj) => lowInput = obj);
            PTextField highInputField = new PTextField( "highLimit" )
            {
                Type = PTextField.FieldType.Integer,
                OnTextChanged = OnTextChangedHigh,
            };
            highInputField.SetMinWidthInCharacters(6);
            highInputField.AddOnRealize((obj) => highInput = obj);
            PLabel label = new PLabel( "label" )
            {
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                Text = STRINGS.TEMPERATURELIMIT.LABEL
            };
            PLabel separator = new PLabel( "separator" )
            {
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                Text = STRINGS.TEMPERATURELIMIT.RANGE_SEPARATOR
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

        public override int GetSideScreenSortOrder()
        {
            return -1; // put below other normal sidescreen content
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target.GetComponent<TemperatureLimit>() != null;
        }

        public override void SetTarget(GameObject new_target)
        {
            if (new_target == null)
            {
                Debug.LogError("Invalid gameObject received");
                return;
            }
            target = new_target.GetComponent<TemperatureLimit>();
            if (target == null)
            {
                Debug.LogError("The gameObject received does not contain a TemperatureLimit component");
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
            UpdateToolTip();
        }

        private void OnTextChangedLow(GameObject source, string text)
        {
            if( target.IsDisabled())
                SetHighValue( target.MaxValue ); // fill in a value in the other one
            int value = OnTextChanged( text, (int v) => SetLowValue( v ), target.MinValue );
            if( value != -1 && value > target.HighLimit )
                SetHighValue( value );
            UpdateToolTip();
        }

        private void OnTextChangedHigh(GameObject source, string text)
        {
            if( target.IsDisabled())
                SetLowValue( target.MinValue ); // fill in a value in the other one
            int value = OnTextChanged( text, (int v) => SetHighValue( v ), target.MaxValue );
            if( value != -1 && value < target.LowLimit )
                SetLowValue( value );
            UpdateToolTip();
        }

        private int OnTextChanged( string text, Action< int > setValueFunc, int fallback )
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
            int result;
            if(int.TryParse(text, out result))
                result = (int) Math.Round( GameUtil.GetTemperatureConvertedToKelvin(result));
            else
                result = fallback;
            setValueFunc( result );
            return result;
        }

        private void SetLowValue( int value )
        {
            SetValue( value, lowInput,
                (int v) => target.SetLowLimit( v ),
                () => target.LowLimit );
        }

        private void SetHighValue( int value )
        {
            SetValue( value, highInput,
                (int v) => target.SetHighLimit( v ),
                () => target.HighLimit );
        }

        private void SetValue( int value, GameObject input, Action< int > setTargetFunc,
            Func< int > targetValueFunc )
        {
            setTargetFunc( value );
            value = targetValueFunc(); // maybe clamped, so re-read
            TMP_InputField field = input.GetComponent< TMP_InputField >();
            string text = GameUtil.GetFormattedTemperature(value, GameUtil.TimeSlice.None,
                GameUtil.TemperatureInterpretation.Absolute, true, true);
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

        private void UpdateToolTip()
        {
            string tooltip;
            if( !target.IsDisabled())
                tooltip  = string.Format(STRINGS.TEMPERATURELIMIT.TOOLTIP_RANGE,
                    GameUtil.GetFormattedTemperature(target.LowLimit, GameUtil.TimeSlice.None,
                        GameUtil.TemperatureInterpretation.Absolute, true, true),
                    GameUtil.GetFormattedTemperature(target.HighLimit, GameUtil.TimeSlice.None,
                        GameUtil.TemperatureInterpretation.Absolute, true, true));
            else
                tooltip = STRINGS.TEMPERATURELIMIT.TOOLTIP_NOTSET;
            PUIElements.SetToolTip( lowInput, tooltip );
            PUIElements.SetToolTip( highInput, tooltip );
        }

        public override string GetTitle()
        {
            return STRINGS.TEMPERATURELIMIT.SIDESCREEN_TITLE;
        }
    }

    [HarmonyPatch(typeof(DetailsScreen))]
    [HarmonyPatch("OnPrefabInit")]
    public static class DetailsScreen_OnPrefabInit_Patch
    {
        public static void Postfix()
        {
            PUIUtils.AddSideScreenContent<TemperatureLimitSideScreen>();
        }
    }
}
