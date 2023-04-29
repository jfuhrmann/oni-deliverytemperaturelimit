using PeterHan.PLib.Options;
using Newtonsoft.Json;

namespace DeliveryTemperatureLimit
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("https://github.com/llunak/oni-deliverytemperaturelimit")]
    [ConfigFile(SharedConfigLocation: true)]
    [RestartRequired]  // BuildingDef instances are created on game load
    public sealed class Options
    {
        [Option("Under Construction Limit", "Limit also deliveries to buildings under construction.")]
        [JsonProperty]
        public bool UnderConstructionLimit { get; set; }

        [Option("Max Construction Temperature", "Maximum temperature of resources for building a building.")]
        [JsonProperty]
        public float MaxConstructionTemperature { get; set; }

        [Option("Min Construction Temperature", "Minimum temperature of resources for building a building.")]
        [JsonProperty]
        public float MinConstructionTemperature { get; set; }

        [Option("Max Small Construction Temperature", "Maximum temperature of resources for building a small building (25kg max).")]
        [JsonProperty]
        public float MaxSmallConstructionTemperature { get; set; }

        public Options()
        {
            UnderConstructionLimit = false;
            MaxConstructionTemperature = GameUtil.GetTemperatureConvertedFromKelvin(
                45 + 273.15f, GameUtil.temperatureUnit );
            MinConstructionTemperature = GameUtil.GetTemperatureConvertedFromKelvin(
                -50 + 273.15f, GameUtil.temperatureUnit );
            MaxSmallConstructionTemperature = GameUtil.GetTemperatureConvertedFromKelvin(
                95 + 273.15f, GameUtil.temperatureUnit );
        }

        public override string ToString()
        {
            return string.Format("DeliveryTemperatureLimit.Options[underconstructionlimit={0},"
                + "maxconstructiontemperature={1},minconstructiontemperature={2},maxsmallconstructiontemperature={3}]",
                UnderConstructionLimit, MaxConstructionTemperature, MinConstructionTemperature,
                MaxSmallConstructionTemperature);
        }
    }
}
