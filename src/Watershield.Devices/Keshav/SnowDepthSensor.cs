using System.Globalization;

using Watershield.Core.Contracts;

namespace Watershield.Devices.Keshav;

public sealed class SnowDepthSensor : ISensor
{

    private readonly string? scenarioPath;

    public SnowDepthSensor() { }

    public SnowDepthSensor(string deviceId, string segmentId, string csvPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(segmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);

        DeviceId = deviceId;
        SegmentId = segmentId;
        scenarioPath = csvPath;
    }

    public string DeviceId { get; } = "snow-depth";
    public string SegmentId { get; } = "";
    public string Name => "Snow depth sensor";
    public DeviceStatus Status { get; private set; } = DeviceStatus.Offline;

    public event Action<DeviceEvent>? EventRaised;

    public void Initialize()
    {
        if (scenarioPath is null)
        {
            ReportFault("A scenario path is required.");
            return;
        }

        try
        {
            loadedPath = null;
            ReadSnowDepthCm(scenarioPath!);
            nextReadingIndex = 0;
            Status = DeviceStatus.Online;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ReportFault(exception.Message);
        }
    }

    public void Shutdown() => Status = DeviceStatus.Offline;

    public TelemetryPacket ReadTelemetry()
    {
        var values = new Dictionary<string, TelemetryValue>();
        bool online = Status == DeviceStatus.Online;
        if (online)
        {
            values["snow_depth"] = new(ReadSnowDepthCm(scenarioPath!), "cm");
        }

        return new TelemetryPacket(
            DeviceId, nameof(SnowDepthSensor), SegmentId, DateTimeOffset.UtcNow, values,
            online ? TelemetryQuality.Ok : TelemetryQuality.Fault);
    }

    private void ReportFault(string message)
    {
        Status = DeviceStatus.Fault;
        EventRaised?.Invoke(new DeviceEvent(DeviceId, DateTimeOffset.UtcNow, message));
    }

    private string? loadedPath;
    private List<double> readings = [];
    private int nextReadingIndex;

    public double ReadSnowDepthCm(string csvPath)
    {
        string fullPath = Path.GetFullPath(csvPath);

        if (loadedPath != fullPath)
        {
            var validReadings = new List<double>();

            foreach (string row in File.ReadLines(fullPath).Skip(1))
            {
                if (double.TryParse(row, NumberStyles.Float, CultureInfo.InvariantCulture, out double depth)
                    && double.IsFinite(depth)
                    && depth >= 0)
                {
                    validReadings.Add(depth);
                }
            }

            if (validReadings.Count == 0)
            {
                throw new InvalidDataException("The snow-depth CSV contains no usable readings.");
            }

            readings = validReadings;
            loadedPath = fullPath;
            nextReadingIndex = 0;
        }

        double nextReading = readings[nextReadingIndex];
        nextReadingIndex = (nextReadingIndex + 1) % readings.Count;
        return nextReading;
    }
}
