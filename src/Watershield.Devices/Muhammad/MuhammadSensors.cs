using Watershield.Core.Contracts;

namespace Watershield.Devices.Muhammad;

public sealed class RoadTemperatureSensor : ISensor
{
    public RoadTemperatureSensor(string deviceId, string segmentId, string csvPath, TimeProvider? clock = null)
    {
        DeviceId = deviceId;
    }

    public string DeviceId { get; }
    public string Name => "Road temperature sensor";
    public DeviceStatus Status => DeviceStatus.Offline;
    public event Action<DeviceEvent>? EventRaised;

    public void Initialize() { }
    public TelemetryPacket ReadTelemetry() => throw new NotImplementedException();
    public void Shutdown() { }
}
