using Watershield.Core.Contracts;
namespace Watershield.Devices.Naitik;
public sealed class VariableWarningSignController
{
    public VariableWarningSignController(string deviceId, string segmentId, string csvPath,
        IEnumerable<string> presetMessages, TimeProvider? clock = null) { }
    public void Initialize() { }
    public TelemetryPacket ReadTelemetry() => throw new NotImplementedException();
    public CommandResult Execute(ControlCommand command) => throw new NotImplementedException();
    public void ResetScenario() => throw new NotImplementedException();
}
