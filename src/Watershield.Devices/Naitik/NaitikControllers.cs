using Watershield.Core.Contracts;

namespace Watershield.Devices.Naitik;

public abstract class CsvController<TState> : IActuator where TState : class
{
    private readonly string csvPath;
    private readonly TimeProvider clock;
    private LoopedCsvDataSource? source;
    private TState? currentState;
    private TState? commandedState;

    protected CsvController(
        string deviceId, string csvPath, TimeProvider? clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);

        DeviceId = deviceId;
        this.csvPath = csvPath;
        this.clock = clock ?? TimeProvider.System;
    }

    public string DeviceId { get; }
    public abstract string Name { get; }
    public DeviceStatus Status { get; private set; } = DeviceStatus.Offline;

    public event Action<DeviceEvent>? EventRaised;

    protected abstract string[] Columns { get; }
    protected abstract TState? ParseRow(string[] fields);
    protected abstract IReadOnlyDictionary<string, TelemetryValue> GetValues(TState state);
    protected abstract string GetSegment(TState state);
    protected abstract string Describe(TState state);

    protected abstract TState? ApplyCommand(
        ControlCommand command, TState current, out string message);

    protected virtual bool ReturnCompleted(TState commanded, TState baseline) => false;

    public void Initialize()
    {
        source = null;
        currentState = null;
        commandedState = null;

        try
        {
            source = new LoopedCsvDataSource(csvPath, Columns);
            Status = DeviceStatus.Online;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Status = DeviceStatus.Fault;
            Report($"Cannot load simulation data: {ex.Message}");
        }
    }

    public TelemetryPacket ReadTelemetry()
    {
        if (source is null)
        {
            throw new InvalidOperationException(
                "Initialize a valid simulation file before polling.");
        }

        for (int row = 0; row < source.Count; row++)
        {
            var baseline = ParseRow(source.Next());

            if (baseline is null)
            {
                Report("Skipped an invalid simulation row.");
                continue;
            }

            if (commandedState is { } commanded && ReturnCompleted(commanded, baseline))
            {
                commandedState = baseline;
            }

            currentState = commandedState ?? baseline;
            Status = DeviceStatus.Online;

            return new TelemetryPacket(
                DeviceId, GetType().Name, GetSegment(currentState), clock.GetUtcNow(),
                GetValues(currentState), TelemetryQuality.Ok);
        }

        Status = DeviceStatus.Fault;

        return new TelemetryPacket(
            DeviceId, GetType().Name, "", clock.GetUtcNow(),
            new Dictionary<string, TelemetryValue>(), TelemetryQuality.Fault);
    }

    public CommandResult Execute(ControlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TargetDeviceId != DeviceId)
        {
            return Reject(command, "Command targets another device.");
        }

        if (command.CommandId == Guid.Empty || string.IsNullOrWhiteSpace(command.IssuedBy))
        {
            return Reject(command, "A command ID and issuing user are required.");
        }

        if (Status != DeviceStatus.Online || currentState is not { } current)
        {
            return Reject(command, "Poll an online controller before sending commands.");
        }

        var next = ApplyCommand(command, current, out string message);

        if (next is null)
        {
            return Reject(command, message);
        }

        currentState = next;
        commandedState = next;
        Report(message);

        return new CommandResult(
            command.CommandId, CommandOutcome.Success, Describe(next), message);
    }

    public void ResetScenario()
    {
        if (source is null)
        {
            throw new InvalidOperationException("Initialize before resetting the scenario.");
        }

        source.Reset();
        currentState = null;
        commandedState = null;
        Status = DeviceStatus.Online;
    }

    public void Shutdown()
    {
        source = null;
        currentState = null;
        commandedState = null;
        Status = DeviceStatus.Offline;
    }

    private CommandResult Reject(ControlCommand command, string reason)
    {
        Report(reason);
        string state = currentState is { } current ? Describe(current) : "";

        return new CommandResult(
            command.CommandId, CommandOutcome.Rejected, state, reason);
    }

    private void Report(string message)
    {
        EventRaised?.Invoke(new DeviceEvent(DeviceId, clock.GetUtcNow(), message));
    }
}

public sealed record SignState(bool Active, string Message);

public sealed class VariableWarningSignController : CsvController<SignState>
{
    private readonly string segmentId;
    private readonly HashSet<string> presetMessages;

    public VariableWarningSignController(
        string deviceId,
        string segmentId,
        string csvPath,
        IEnumerable<string> presetMessages,
        TimeProvider? clock = null)
        : base(deviceId, csvPath, clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segmentId);
        ArgumentNullException.ThrowIfNull(presetMessages);

        this.segmentId = segmentId;
        this.presetMessages = new HashSet<string>(presetMessages, StringComparer.Ordinal);
        if (this.presetMessages.Count == 0 || this.presetMessages.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one nonblank configured value is required.", nameof(presetMessages));
    }

    public override string Name => "Variable warning sign";
    protected override string[] Columns => ["active", "message"];

    protected override SignState? ParseRow(string[] fields)
    {
        if (fields.Length != 2 || !bool.TryParse(fields[0], out bool active))
        {
            return null;
        }

        if (!active)
        {
            return new SignState(false, "");
        }

        return presetMessages.Contains(fields[1]) ? new SignState(true, fields[1]) : null;
    }

    protected override SignState? ApplyCommand(
        ControlCommand command, SignState current, out string message)
    {
        if (command.Action == "Deactivate")
        {
            message = "Warning sign turned off.";
            return new SignState(false, "");
        }

        if (command.Action == "SetMessage" &&
            command.Parameter?.Trim() is { Length: > 0 } selected &&
            presetMessages.Contains(selected))
        {
            message = $"Warning sign activated with '{selected}'.";
            return new SignState(true, selected);
        }

        message = "Use Deactivate or SetMessage with a preset message.";
        return null;
    }

    protected override IReadOnlyDictionary<string, TelemetryValue> GetValues(SignState state)
    {
        return new Dictionary<string, TelemetryValue>
        {
            ["sign_active"] = new(state.Active, ""),
            ["sign_message"] = new(state.Message, "")
        };
    }

    protected override string GetSegment(SignState state) => segmentId;
    protected override string Describe(SignState state) => state.Active ? state.Message : "INACTIVE";
}

