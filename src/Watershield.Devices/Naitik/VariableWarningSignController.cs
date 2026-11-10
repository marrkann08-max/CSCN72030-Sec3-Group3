using Watershield.Core.Contracts;

namespace Watershield.Devices.Naitik;

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

