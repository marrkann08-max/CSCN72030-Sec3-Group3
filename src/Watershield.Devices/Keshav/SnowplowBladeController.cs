using Watershield.Core.Contracts;

namespace Watershield.Devices.Keshav;

public enum BladePosition
{
    Up,
    Down
}

public sealed class SnowplowBladeController : IActuator
{

    private readonly string? scenarioPath;

    public SnowplowBladeController() { }

    public SnowplowBladeController(string deviceId, string segmentId, string csvPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(segmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);

        DeviceId = deviceId;
        SegmentId = segmentId;
        scenarioPath = csvPath;
    }

    public string DeviceId { get; } = "blade";
    public string SegmentId { get; } = "";
    public string Name => "Snowplow blade controller";
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
            LoadScenario(scenarioPath!);
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
            values["blade_state"] = new(PollFeedback().ToString().ToUpperInvariant(), "");
        }

        return new TelemetryPacket(
            DeviceId, nameof(SnowplowBladeController), SegmentId, DateTimeOffset.UtcNow, values,
            online ? TelemetryQuality.Ok : TelemetryQuality.Fault);
    }

    private void ReportFault(string message)
    {
        Status = DeviceStatus.Fault;
        EventRaised?.Invoke(new DeviceEvent(DeviceId, DateTimeOffset.UtcNow, message));
    }

    public CommandResult Execute(ControlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (Status != DeviceStatus.Online ||
            command.TargetDeviceId != DeviceId ||
            command.CommandId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.IssuedBy) ||
            command.Action != "SetBlade" ||
            !TrySetPosition(command.Parameter))
        {
            return new CommandResult(
                command.CommandId, CommandOutcome.Rejected,
                Position.ToString().ToUpperInvariant(), "Expected an online blade and a valid SetBlade command.");
        }

        return new CommandResult(
            command.CommandId, CommandOutcome.Success,
            Position.ToString().ToUpperInvariant(), "Blade position updated.");
    }

    private BladePosition[] scenarioReadings = [];
    private int nextReadingIndex;
    private BladePosition? commandedPosition;

    public BladePosition Position { get; private set; } = BladePosition.Up;

    public void LoadScenario(string csvPath)
    {
        using var rows = File.ReadLines(csvPath).GetEnumerator();

        if (!rows.MoveNext() || rows.Current.Trim() != "blade_state")
        {
            throw new InvalidDataException("Expected a blade_state CSV header.");
        }

        var validReadings = new List<BladePosition>();

        while (rows.MoveNext())
        {
            if (ParsePosition(rows.Current.Trim()) is { } position)
            {
                validReadings.Add(position);
            }
        }

        if (validReadings.Count == 0)
        {
            throw new InvalidDataException("The blade CSV contains no usable readings.");
        }

        scenarioReadings = validReadings.ToArray();
        ResetScenario();
    }

    public BladePosition PollFeedback()
    {
        EnsureScenarioLoaded();

        Position = commandedPosition ?? scenarioReadings[nextReadingIndex];
        nextReadingIndex = (nextReadingIndex + 1) % scenarioReadings.Length;
        return Position;
    }

    public void ResetScenario()
    {
        EnsureScenarioLoaded();

        nextReadingIndex = 0;
        commandedPosition = null;
        Position = scenarioReadings[0];
    }

    public bool TrySetPosition(string? command)
    {
        if (ParsePosition(command) is not { } position)
        {
            return false;
        }

        Position = position;
        commandedPosition = position;
        return true;
    }

    private void EnsureScenarioLoaded()
    {
        if (scenarioReadings.Length == 0)
        {
            throw new InvalidOperationException("Load a blade scenario before polling or resetting it.");
        }
    }

    private static BladePosition? ParsePosition(string? value) => value switch
    {
        "UP" => BladePosition.Up,
        "DOWN" => BladePosition.Down,
        _ => null
    };
}
