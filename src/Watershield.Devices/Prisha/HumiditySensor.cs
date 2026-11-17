using Watershield.Core.Contracts;
using Watershield.Devices.Muhammad;

namespace Watershield.Devices.Prisha;

public sealed class HumiditySensor : CsvWeatherSensor
{
    public HumiditySensor(
        string deviceId,
        string segmentId,
        string csvPath,
        TimeProvider? clock = null)
        : base(deviceId, segmentId, csvPath, clock)
    {
    }

    public override string Name => "Humidity sensor";

    protected override string[] CsvColumns => ["relative_humidity_percent"];

    protected override Dictionary<string, TelemetryValue>? ParseRow(string[] fields)
    {
        if (fields.Length != 1 ||
            !TryNumber(fields[0], out double humidity) ||
            humidity is < 0 or > 100)
        {
            return null;
        }

        return new Dictionary<string, TelemetryValue>
        {
            ["relative_humidity"] = new(humidity, "%")
        };
    }
}