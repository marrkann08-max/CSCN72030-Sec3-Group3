using Watershield.Core.Contracts;

namespace Watershield.Devices.Prisha;

public sealed class SnowplowRouteController
{
    private readonly string csvPath;
    private readonly HashSet<string> allowedRoutes;
    private LoopedCsvDataSource? source;

    public SnowplowRouteController(
        string deviceId,
        string segmentId,
        string csvPath,
        IEnumerable<string> allowedRoutes)
    {
        DeviceId = deviceId;
        SegmentId = segmentId;
        this.csvPath = csvPath;
        this.allowedRoutes = new HashSet<string>(allowedRoutes, StringComparer.Ordinal);
    }

    public string DeviceId { get; }
    public string SegmentId { get; }
    public string? AssignedRoute { get; private set; }

    public void Initialize()
    {
        source = null;
        source = new LoopedCsvDataSource(csvPath, "route_id");
    }

    public void Shutdown()
    {
        source = null;
        AssignedRoute = null;
    }

    public void ResetScenario()
    {
        if (source is null)
        {
            throw new InvalidOperationException("Initialize before resetting the scenario.");
        }

        source.Reset();
        AssignedRoute = null;
    }


    public TelemetryPacket ReadTelemetry()
    {
        if (source is null)
        {
            throw new InvalidOperationException("Initialize before reading route telemetry.");
        }

        for (int row = 0; row < source.Count; row++)
        {
            string[] fields = source.Next();

            if (fields.Length != 1 || !allowedRoutes.Contains(fields[0]))
            {
                continue;
            }

            string route = AssignedRoute ?? fields[0];

            return new TelemetryPacket(
                DeviceId,
                nameof(SnowplowRouteController),
                SegmentId,
                DateTimeOffset.UtcNow,
                new Dictionary<string, TelemetryValue>
                {
                    ["route_id"] = new(route, "")
                },
                TelemetryQuality.Ok);
        }

        return new TelemetryPacket(
            DeviceId,
            nameof(SnowplowRouteController),
            SegmentId,
            DateTimeOffset.UtcNow,
            new Dictionary<string, TelemetryValue>(),
            TelemetryQuality.Fault);
    }
    public CommandResult Execute(ControlCommand command)
    {
        string? route = command.Parameter;

        if (source is null ||
    command.TargetDeviceId != DeviceId ||
    command.Action != "AssignRoute" ||
    command.CommandId == Guid.Empty ||
    string.IsNullOrWhiteSpace(command.IssuedBy) ||
    route is null ||
    !allowedRoutes.Contains(route))
        {
            return new CommandResult(
                command.CommandId,
                CommandOutcome.Rejected,
                AssignedRoute ?? "",
                "Route assignment rejected.");
        }

        AssignedRoute = route;

        return new CommandResult(
            command.CommandId,
            CommandOutcome.Success,
            route,
            "Route assigned.");
    }
}