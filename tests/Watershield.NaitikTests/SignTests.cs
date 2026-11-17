using Watershield.Core.Contracts;
using Watershield.Devices.Naitik;
namespace Watershield.NaitikTests;
public sealed class SignTests : IDisposable
{
    private readonly string path = Path.GetTempFileName();
    public void Dispose() => File.Delete(path);
    private VariableWarningSignController Create()
    {
        File.WriteAllText(path, "active,message\nfalse,\ntrue,SLOW DOWN\n");
        var sign = new VariableWarningSignController("S-03", "R-01", path, ["ICY ROAD AHEAD", "SLOW DOWN"]);
        sign.Initialize();
        return sign;
    }
    private static ControlCommand Command(string action, string? value = null) =>
        new(Guid.NewGuid(), "S-03", action, value, "Naitik", DateTimeOffset.UtcNow);
    [Fact] public void BaselineLoopsAndReportsIdentity()
    {
        var sign = Create();
        var packet = sign.ReadTelemetry();
        Assert.Equal("S-03", packet.DeviceId);
        Assert.Equal("R-01", packet.SegmentId);
        Assert.Equal(false, packet.Values["sign_active"].Value);
        Assert.Equal("SLOW DOWN", sign.ReadTelemetry().Values["sign_message"].Value);
        Assert.Equal(false, sign.ReadTelemetry().Values["sign_active"].Value);
    }
    [Fact] public void AcceptedCommandPersistsAcrossWrapAndResetRestoresBaseline()
    {
        var sign = Create(); sign.ReadTelemetry();
        var command = Command("SetMessage", "ICY ROAD AHEAD");
        var result = sign.Execute(command);
        Assert.Equal(command.CommandId, result.CommandId);
        Assert.Equal(CommandOutcome.Success, result.Outcome);
        for (int i = 0; i < 5; i++)
            Assert.Equal("ICY ROAD AHEAD", sign.ReadTelemetry().Values["sign_message"].Value);
        Assert.Equal(CommandOutcome.Success, sign.Execute(Command("Deactivate")).Outcome);
        Assert.Equal(false, sign.ReadTelemetry().Values["sign_active"].Value);
        sign.ResetScenario();
        Assert.Equal(false, sign.ReadTelemetry().Values["sign_active"].Value);
        Assert.Equal("SLOW DOWN", sign.ReadTelemetry().Values["sign_message"].Value);
    }
    [Theory]
    [InlineData("SetMessage", "UNKNOWN")]
    [InlineData("SetMessage", "")]
    [InlineData("SetMessage", null)]
    [InlineData("InvalidAction", "SLOW DOWN")]
    public void InvalidCommandsPreserveAcceptedState(string action, string? value)
    {
        var sign = Create(); sign.ReadTelemetry();
        sign.Execute(Command("SetMessage", "ICY ROAD AHEAD"));
        var result = sign.Execute(Command(action, value));
        Assert.Equal(CommandOutcome.Rejected, result.Outcome);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.Equal("ICY ROAD AHEAD", sign.ReadTelemetry().Values["sign_message"].Value);
    }
}
