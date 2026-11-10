using Watershield.Core.Contracts;
using Watershield.Devices.Naitik;
namespace Watershield.NaitikTests;
public sealed class DispatchTests : IDisposable
{
    private readonly string path = Path.GetTempFileName();
    public void Dispose() => File.Delete(path);
    private DispatchController Create(string rows = "AVAILABLE,\n")
    {
        File.WriteAllText(path, "vehicle_status,destination_segment\n" + rows);
        var device = new DispatchController("PLOW-04", path, ["R-01", "R-02"]);
        device.Initialize(); device.ReadTelemetry(); return device;
    }
    private static ControlCommand Command(string action, string? value = null) =>
        new(Guid.NewGuid(), "PLOW-04", action, value, "Naitik", DateTimeOffset.UtcNow);
    [Fact] public void DispatchPersistsAndRecallCompletesOnlyAtAvailableFeedback()
    {
        var device = Create("AVAILABLE,\nRETURNING,R-01\nAVAILABLE,\n");
        Assert.Equal(CommandOutcome.Success, device.Execute(Command("Dispatch", "R-02")).Outcome);
        for (int i = 0; i < 4; i++)
        {
            var packet = device.ReadTelemetry();
            Assert.Equal("DISPATCHED", packet.Values["vehicle_status"].Value);
            Assert.Equal("R-02", packet.SegmentId);
        }
        Assert.Equal("RETURNING", device.Execute(Command("Recall")).NewState);
        Assert.Equal("AVAILABLE", device.ReadTelemetry().Values["vehicle_status"].Value);
        // Completion is a state-machine transition, not permission for old baseline states to return.
        Assert.Equal("AVAILABLE", device.ReadTelemetry().Values["vehicle_status"].Value);
        Assert.Equal("AVAILABLE", device.ReadTelemetry().Values["vehicle_status"].Value);
    }
    [Theory]
    [InlineData("AVAILABLE,", "Recall", null)]
    [InlineData("AVAILABLE,", "Dispatch", "R-99")]
    [InlineData("AVAILABLE,", "Dispatch", null)]
    [InlineData("DISPATCHED,R-01", "Dispatch", "R-02")]
    [InlineData("RETURNING,R-01", "Dispatch", "R-02")]
    [InlineData("OUT OF SERVICE,", "Dispatch", "R-01")]
    [InlineData("OUT OF SERVICE,", "Recall", null)]
    [InlineData("AVAILABLE,", "Unknown", null)]
    public void RejectsInvalidTransitions(string baseline, string action, string? value)
    {
        var device = Create(baseline + "\n");
        var result = device.Execute(Command(action, value));
        Assert.Equal(CommandOutcome.Rejected, result.Outcome);
        Assert.NotEmpty(result.Message);
        Assert.Equal(baseline.Split(',')[0], device.ReadTelemetry().Values["vehicle_status"].Value);
    }
    [Fact] public void ReturningWaitsForDepotFeedbackAndResetClearsCommand()
    {
        var device = Create("DISPATCHED,R-01\nRETURNING,R-01\n");
        Assert.Equal("RETURNING", device.Execute(Command("Recall")).NewState);
        for (int i = 0; i < 4; i++)
            Assert.Equal("RETURNING", device.ReadTelemetry().Values["vehicle_status"].Value);
        device.ResetScenario();
        Assert.Equal("DISPATCHED", device.ReadTelemetry().Values["vehicle_status"].Value);
    }
}
