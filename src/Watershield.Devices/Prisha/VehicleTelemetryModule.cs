using Watershield.Core.Contracts;
using Watershield.Devices.Muhammad;

namespace Watershield.Devices.Prisha;

public sealed class VehicleTelemetryModule : CsvWeatherSensor
{
    public VehicleTelemetryModule(
        string deviceId,
        string segmentId,
        string csvPath,
        TimeProvider? clock = null)
        : base(deviceId, segmentId, csvPath, clock)
    {
    }

    public override string Name => "Vehicle GPS and telemetry";

    protected override string[] CsvColumns =>
        ["vehicle_id", "latitude", "longitude", "speed_kmh", "vehicle_status"];

    protected override Dictionary<string, TelemetryValue>? ParseRow(string[] fields)
    {
        if (fields.Length != 5 ||
            string.IsNullOrWhiteSpace(fields[0]) ||
            !TryNumber(fields[1], out double latitude) ||
            latitude is < -90 or > 90 ||
            !TryNumber(fields[2], out double longitude) ||
            longitude is < -180 or > 180 ||
            !TryNumber(fields[3], out double speed) ||
            speed < 0)
        {
            return null;
        }

        string status = fields[4].ToUpperInvariant();

        if (status is not ("AVAILABLE" or "DISPATCHED" or "RETURNING" or "OUT OF SERVICE"))
        {
            return null;
        }

        return new Dictionary<string, TelemetryValue>
        {
            ["vehicle_id"] = new(fields[0], ""),
            ["latitude"] = new(latitude, "°"),
            ["longitude"] = new(longitude, "°"),
            ["speed"] = new(speed, "km/h"),
            ["vehicle_status"] = new(status, "")
        };
    }
}