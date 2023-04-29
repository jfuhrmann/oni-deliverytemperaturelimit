using KSerialization;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DeliveryTemperatureLimit
{
    public class TemperatureLimit : KMonoBehaviour
    {
        [Serialize]
        [SerializeField] // needed so that making copies of an instance copies the private field too
        private int lowLimit = 0; // 0 Kelvin

        [Serialize]
        [SerializeField]
        private int highLimit = 0; // if 0, then not active

        public int MinValue => 0;

        public int MaxValue => 5000; // diamond melts at ~4200K

        public bool IsDisabled() => ( highLimit == 0 );
        public int LowLimit => lowLimit;
        public int HighLimit => highLimit;

        private static readonly EventSystem.IntraObjectHandler<TemperatureLimit> OnCopySettingsDelegate
            = new EventSystem.IntraObjectHandler<TemperatureLimit>(delegate(TemperatureLimit component, object data)
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
            TemperatureLimit component = ((GameObject)data).GetComponent<TemperatureLimit>();
            if (component != null)
            {
                lowLimit = component.lowLimit;
                highLimit = component.highLimit;
            }
        }

        public void SetLowLimit(int value)
        {
            lowLimit = Math.Max( value, MinValue );
        }

        public void SetHighLimit(int value)
        {
            highLimit = Math.Min( value, MaxValue );
        }

        public void Disable()
        {
            highLimit = 0;
        }

        public bool AllowedByTemperature( float temperature )
        {
            if( highLimit == 0 ) // limit disabled
                return true;
            int t = (int) temperature;
            return lowLimit <= t && t < highLimit;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if( highLimit == 0 )
            {
                TemperatureLimits oldLimit = GetComponent< TemperatureLimits >();
                if( oldLimit != null && !oldLimit.IsDisabled())
                {
                    lowLimit = oldLimit.LowLimit;
                    highLimit = oldLimit.HighLimit;
                }
            }
        }
    }

    // Backwards compatibility.
    // TODO: remove?
    public class TemperatureLimits : KMonoBehaviour
    {
        [Serialize]
        [SerializeField]
        private float lowLimit = 0;

        [Serialize]
        [SerializeField]
        private float highLimit = 0;

        public bool IsDisabled() => ( highLimit == 0 );
        public int LowLimit => (int)lowLimit;
        public int HighLimit => (int)highLimit;
    }
}
