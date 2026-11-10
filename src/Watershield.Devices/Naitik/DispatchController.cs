using Watershield.Core.Contracts;
namespace Watershield.Devices.Naitik;

public sealed record DispatchState(string Status, string SegmentId);

public sealed class DispatchController : CsvController<DispatchState>
{
    private readonly HashSet<string> knownSegments;

    public DispatchController(
        string vehicleId,
        string csvPath,
        IEnumerable<string> knownSegments,
        TimeProvider? clock = null)
        : base(vehicleId, csvPath, clock)
    {
        ArgumentNullException.ThrowIfNull(knownSegments);
        this.knownSegments = new HashSet<string>(knownSegments, StringComparer.Ordinal);
    }

    public override string Name => "Winter operations dispatch";
    protected override string[] Columns => ["vehicle_status", "destination_segment"];

    protected override DispatchState? ParseRow(string[] fields)
    {
        if (fields.Length != 2)
        {
            return null;
        }

        string status = fields[0].ToUpperInvariant();
        string destination = fields[1];

        if (status is "AVAILABLE" or "OUT OF SERVICE")
        {
            return destination.Length == 0 ? new DispatchState(status, "") : null;
        }

        if ((status is "DISPATCHED" or "RETURNING") && knownSegments.Contains(destination))
        {
            return new DispatchState(status, destination);
        }

        return null;
    }

    protected override DispatchState? ApplyCommand(
        ControlCommand command, DispatchState current, out string message)
    {
        if (command.Action == "Dispatch" && current.Status == "AVAILABLE" &&
            command.Parameter?.Trim() is { Length: > 0 } destination &&
            knownSegments.Contains(destination))
        {
            message = $"Vehicle dispatched to {destination}.";
            return new DispatchState("DISPATCHED", destination);
        }

        if (command.Action == "Recall" && current.Status == "DISPATCHED")
        {
            message = "Vehicle recalled.";
            return current with { Status = "RETURNING" };
        }

        message = "Dispatch requires an available vehicle and known destination. " +
                  "Recall requires a dispatched vehicle.";
        return null;
    }

    protected override bool ReturnCompleted(DispatchState commanded, DispatchState baseline)
    {
        return commanded.Status == "RETURNING" && baseline.Status == "AVAILABLE";
    }

    protected override IReadOnlyDictionary<string, TelemetryValue> GetValues(DispatchState state)
    {
        return new Dictionary<string, TelemetryValue>
        {
            ["vehicle_id"] = new(DeviceId, ""),
            ["vehicle_status"] = new(state.Status, ""),
            ["destination_segment"] = new(state.SegmentId, "")
        };
    }

    protected override string GetSegment(DispatchState state) => state.SegmentId;
    protected override string Describe(DispatchState state) => state.Status;
}
