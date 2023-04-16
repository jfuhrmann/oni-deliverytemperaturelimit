using KSerialization;
using UnityEngine;

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
}
