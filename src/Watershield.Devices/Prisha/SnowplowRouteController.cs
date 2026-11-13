using Watershield.Core.Contracts;

namespace Watershield.Devices.Prisha;

public sealed class SnowplowRouteController
{
    private readonly string csvPath;
    private readonly HashSet<string> allowedRoutes;
    private bool initialized;

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
        initialized = false;
        _ = new LoopedCsvDataSource(csvPath, "route_id");
        initialized = true;
    }

    public CommandResult Execute(ControlCommand command)
    {
        string? route = command.Parameter;

        if (!initialized ||
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