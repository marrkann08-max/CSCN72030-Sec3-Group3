using Watershield.Core.Contracts;
namespace Watershield.Devices.Naitik;
public sealed class DispatchController
{
    public DispatchController(string vehicleId, string csvPath, IEnumerable<string> knownSegments, TimeProvider? clock = null) { }
    public void Initialize() { }
    public TelemetryPacket ReadTelemetry() => throw new NotImplementedException();
    public CommandResult Execute(ControlCommand command) => throw new NotImplementedException();
    public void ResetScenario() => throw new NotImplementedException();
}
