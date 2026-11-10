using System.Globalization;
using Watershield.Core.Contracts;

namespace Watershield.Devices.Muhammad;

// base for my three sensors - they all just read a looped csv, so the loading/skipping
// logic lives here and each sensor only says what its header is and how to parse a row
public abstract class CsvWeatherSensor : ISensor
{
    private readonly string csvPath;
    private readonly TimeProvider clock;
    private LoopedCsvDataSource? source;

    protected CsvWeatherSensor(string deviceId, string segmentId, string csvPath, TimeProvider? clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(segmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);
        DeviceId = deviceId;
        SegmentId = segmentId;
        this.csvPath = csvPath;
        this.clock = clock ?? TimeProvider.System;
    }

    public string DeviceId { get; }
    public string SegmentId { get; }
    public abstract string Name { get; }
    public DeviceStatus Status { get; private set; } = DeviceStatus.Offline;
    public TelemetryPacket? LastValidReading { get; private set; }
    public event Action<DeviceEvent>? EventRaised;
    protected abstract string[] CsvColumns { get; }

    public void Initialize()
    {
        source = null;
        LastValidReading = null;
        source = new LoopedCsvDataSource(csvPath, CsvColumns);
        Status = DeviceStatus.Online;
    }

    public TelemetryPacket ReadTelemetry()
    {
        if (source is null) throw new InvalidOperationException("Initialize a valid simulation file before polling.");
        // only go around the file once, otherwise a file full of junk would spin forever
        for (int row = 0; row < source.Count; row++)
        {
            var values = ParseRow(source.Next());
            if (values is not null)
            {
                Status = DeviceStatus.Online;
                var reading = CreatePacket(values, TelemetryQuality.Ok);
                LastValidReading = reading;
                return reading;
            }
            ReportEvent("Skipped an invalid simulation row.");
        }
        throw new InvalidDataException("The simulation file has no valid rows.");
    }

    public void Shutdown()
    {
        source = null;
        Status = DeviceStatus.Offline;
    }

    protected abstract Dictionary<string, TelemetryValue>? ParseRow(string[] fields);

    // invariant culture so "-8.4" parses the same on every machine, and NaN/Infinity count as bad
    protected static bool TryNumber(string value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);

    private TelemetryPacket CreatePacket(IReadOnlyDictionary<string, TelemetryValue> values, TelemetryQuality quality) =>
        new(DeviceId, GetType().Name, SegmentId, clock.GetUtcNow(), values, quality);

    private void ReportEvent(string message) => EventRaised?.Invoke(new DeviceEvent(DeviceId, clock.GetUtcNow(), message));
}

public sealed class RoadTemperatureSensor : CsvWeatherSensor
{
    public RoadTemperatureSensor(string deviceId, string segmentId, string csvPath, TimeProvider? clock = null)
        : base(deviceId, segmentId, csvPath, clock) { }

    public override string Name => "Road temperature sensor";
    protected override string[] CsvColumns => ["road_temperature_c"];

    protected override Dictionary<string, TelemetryValue>? ParseRow(string[] fields) =>
        fields.Length != 1 || !TryNumber(fields[0], out double t)
            ? null
            : new() { ["road_temperature"] = new(t, "°C") };
}

public sealed class SurfaceTemperatureSensor : CsvWeatherSensor
{
    public SurfaceTemperatureSensor(string deviceId, string segmentId, string csvPath, TimeProvider? clock = null)
        : base(deviceId, segmentId, csvPath, clock) { }

    public override string Name => "Surface temperature sensor";
    protected override string[] CsvColumns => ["surface_temperature_c"];

    protected override Dictionary<string, TelemetryValue>? ParseRow(string[] fields) =>
        fields.Length != 1 || !TryNumber(fields[0], out double t)
            ? null
            : new() { ["surface_temperature"] = new(t, "°C") };
}

public sealed class WeatherStation : ISensor
{
    public WeatherStation(string deviceId, string segmentId, string csvPath, TimeProvider? clock = null)
    {
        DeviceId = deviceId;
    }

    public string DeviceId { get; }
    public string Name => "Weather station";
    public DeviceStatus Status => DeviceStatus.Offline;
    public event Action<DeviceEvent>? EventRaised;

    public void Initialize() { }
    public TelemetryPacket ReadTelemetry() => throw new NotImplementedException();
    public void Shutdown() { }
}
