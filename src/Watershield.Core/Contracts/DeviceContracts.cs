namespace Watershield.Core.Contracts;

public enum DeviceStatus { Offline, Online, Fault }
public enum TelemetryQuality { Ok, Stale, Fault }
public enum CommandOutcome { Success, Failed, Rejected }
public sealed record TelemetryValue(object Value, string Unit);
public sealed record TelemetryPacket(string DeviceId, string DeviceType, string SegmentId,
    DateTimeOffset TimestampUtc, IReadOnlyDictionary<string, TelemetryValue> Values, TelemetryQuality Quality);
public sealed record ControlCommand(Guid CommandId, string TargetDeviceId, string Action,
    string? Parameter, string IssuedBy, DateTimeOffset TimestampUtc);
public sealed record CommandResult(Guid CommandId, CommandOutcome Outcome, string NewState, string Message);
public sealed record DeviceEvent(string DeviceId, DateTimeOffset TimestampUtc, string Message);
public interface IDevice
{
    string DeviceId { get; }
    string Name { get; }
    DeviceStatus Status { get; }
    event Action<DeviceEvent>? EventRaised;
    void Initialize();
    void Shutdown();
}
public interface ISensor : IDevice { TelemetryPacket ReadTelemetry(); }
public interface IActuator : ISensor { CommandResult Execute(ControlCommand command); }
