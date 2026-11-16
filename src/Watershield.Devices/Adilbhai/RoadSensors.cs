using Watershield.Core.Contracts;
using Watershield.Devices.Muhammad;

namespace Watershield.Devices.Adilbhai;

public sealed class RoadFrictionSensor : CsvWeatherSensor
{
    public RoadFrictionSensor(
        string deviceId,
        string segmentId,
        string csvPath,
        TimeProvider? clock = null)
        : base(deviceId, segmentId, csvPath, clock)
    {
    }

    public override string Name => "Road friction sensor";

    protected override string[] CsvColumns => ["friction_coefficient"];

    protected override Dictionary<string, TelemetryValue>? ParseRow(string[] fields)
    {
        if (fields.Length != 1 ||
            !TryNumber(fields[0], out double coefficient) ||
            coefficient is < 0 or > 1)
        {
            return null;
        }

        return new Dictionary<string, TelemetryValue>
        {
            ["road_friction"] = new(coefficient, "")
        };
    }
}

public sealed class RoadConditionSensor : CsvWeatherSensor
{
    public RoadConditionSensor(
        string deviceId,
        string segmentId,
        string csvPath,
        TimeProvider? clock = null)
        : base(deviceId, segmentId, csvPath, clock)
    {
    }

    public override string Name => "Road condition sensor";

    protected override string[] CsvColumns => ["road_condition"];

    protected override Dictionary<string, TelemetryValue>? ParseRow(string[] fields)
    {
        if (fields.Length != 1)
        {
            return null;
        }

        string condition = fields[0].ToUpperInvariant();

        if (condition is not ("DRY" or "WET" or "SNOW" or "ICE"))
        {
            return null;
        }

        return new Dictionary<string, TelemetryValue>
        {
            ["road_condition"] = new(condition, "")
        };
    }
}
