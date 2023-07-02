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

        public const int MinValue = 0;

        public const int MaxValue = 5000; // diamond melts at ~4200K

        public bool IsDisabled() => ( highLimit == 0 );
        public int LowLimit => lowLimit;
        public int HighLimit => highLimit;

        // GetComponent() calls may add up being somewhat expensive when called often,
        // so instead cache the mapping.
        private static Dictionary< GameObject, TemperatureLimit > fastMap
            = new Dictionary< GameObject, TemperatureLimit >();

        public static TemperatureLimit Get( GameObject gameObject )
        {
            if( fastMap.TryGetValue( gameObject, out TemperatureLimit limit ))
                return limit;
            return null;
        }

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
            CopySettings( TemperatureLimit.Get((GameObject)data));
        }

        public void CopySettings( TemperatureLimit source )
        {
            if (source != null)
            {
                lowLimit = source.lowLimit;
                highLimit = source.highLimit;
                SetDirty();
            }
        }

        public void SetLowLimit(int value)
        {
            lowLimit = Math.Max( value, MinValue );
            SetDirty();
        }

        public void SetHighLimit(int value)
        {
            highLimit = Math.Min( value, MaxValue );
            SetDirty();
        }

        public void Disable()
        {
            highLimit = 0;
            SetDirty();
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
            fastMap[ gameObject ] = this;
            if( highLimit == 0 )
            {
                TemperatureLimits oldLimit = GetComponent< TemperatureLimits >();
                if( oldLimit != null && !oldLimit.IsDisabled())
                {
                    lowLimit = oldLimit.LowLimit;
                    highLimit = oldLimit.HighLimit;
                    oldLimit.Reset();
                }
            }
            allLimits.Add( this );
            SetDirty();
        }

        protected override void OnCleanUp()
        {
            allLimits.Remove( this );
            SetDirty();
            fastMap.Remove( gameObject );
            base.OnCleanUp();
        }

        // Some game code groups resources by their tag (material) and then processes
        // such groups as a whole. That wouldn't work with temperature limits, as resources
        // with different temperatures may need different handling. But grouping by each
        // existing temperature would be expensive, and some of this code is called very
        // often, so instead only group them by existing limits: For example if there
        // is storage #1 with limit 10C-30C and storage #2 with limit 20C-30C, then it's
        // enough to group by min-10C, 10C-20C, 20C-30C and 30C-max, as e.g. 15C is different
        // from 25C (accepted vs not accepted by #2), but 21C vs 29C doesn't matter. For that
        // reason this code collects all such groups and gives them integer indexes,
        // which is then easier and faster to handle.
        // Additional complication is that some of the relevant game code is run in threads,
        // so this needs to be thread-safe. This is handled by the hot code being lockless
        // if possible, only checking one volatile atomic flag and then accessing pre-built data.
        // If something changes, the flag is set, and then the data is rebuilt with a lock
        // held. Race conditions in the lockless code don't matter, the code is called repeatedly
        // (temperatures change over time), so if it works with data that is out of date,
        // it'll be updated again later.
        private static object lockObject = new object();
        private static List< TemperatureLimit > allLimits = new List< TemperatureLimit >();
        private volatile static bool limitsDirty = true;
        // A list of temperature values where groups end (in the example above, this would
        // be { 10, 20, 30, max }.
        private static List< int > indexTemperatures;
        // The inverse of indexTemperatures. There are only 5000 (MinValue <= t < MaxValue) integer
        // temperatures, and TemperatureIndex() seems to be called for hot code, so replace
        // lookup in a loop with O(1) indexing.
        private static System.Int16[] temperaturesToIndex;

        private static void SetDirty()
        {
            lock( lockObject )
            {
                limitsDirty = true;
            }
        }

        private static void UpdateIndexes()
        {
            lock( lockObject )
            {
                if( !limitsDirty )
                    return;
                List< int > tmp = new List< int >();
                tmp.Add( TemperatureLimit.MaxValue );
                foreach( TemperatureLimit limit in allLimits )
                {
                    if( !limit.IsDisabled())
                    {
                        tmp.Add( limit.LowLimit );
                        tmp.Add( limit.HighLimit );
                    }
                }
                tmp.Sort();
                tmp = tmp.Distinct().ToList();
                if( !tmp.Equals( indexTemperatures ))
                {
                    indexTemperatures = tmp;
                    System.Int16[] newTemperaturesToIndex = new System.Int16[ MaxValue - MinValue ];
                    int pos = 0;
                    for( System.Int16 i = 0; i < indexTemperatures.Count; ++i )
                    {
                        int value = indexTemperatures[ i ];
                        while( pos < value )
                            newTemperaturesToIndex[ pos++ ] = i;
                    }
                    Interlocked.Exchange( ref temperaturesToIndex, newTemperaturesToIndex );
                }
                limitsDirty = false;
            }
        }

        public static int TemperatureIndex( float temperature )
        {
            if( limitsDirty )
                UpdateIndexes();
            if( temperature >= MinValue && temperature < MaxValue )
                return temperaturesToIndex[ (int) temperature ];
            return -1;
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
        public void Reset()
        {
            lowLimit = 0;
            highLimit = 0;
        }
    }
}
