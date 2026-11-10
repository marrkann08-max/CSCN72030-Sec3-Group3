using Watershield.Core.Contracts;
using Watershield.Devices;
using Watershield.Devices.Naitik;
namespace Watershield.NaitikTests;
public sealed class IntegrationTests
{
    [Theory] [InlineData("light-snow")] [InlineData("heavy-snow")] [InlineData("freezing-rain")]
    public void ScenarioFilesContainFiftyValidChangingRecordsAndLoop(string scenario)
    {
        string root = Path.Combine(AppContext.BaseDirectory, "scenarios", scenario);
        string signPath = Path.Combine(root, "warning-sign.csv"), dispatchPath = Path.Combine(root, "dispatch.csv");
        Assert.Equal(51, File.ReadAllLines(signPath).Length);
        Assert.Equal(51, File.ReadAllLines(dispatchPath).Length);
        IActuator[] devices = [new VariableWarningSignController("S-03", "R-01", signPath, ["ICY ROAD AHEAD", "SLOW DOWN"]),
            new DispatchController("PLOW-04", dispatchPath, ["R-01", "R-02"])];
        foreach (var device in devices)
        {
            device.Initialize(); var first = device.ReadTelemetry();
            var distinctStates = new HashSet<string>();
            for (int i = 1; i < 50; i++)
            {
                var packet = device.ReadTelemetry(); Assert.Equal(TelemetryQuality.Ok, packet.Quality);
                distinctStates.Add(string.Join("|", packet.Values.Select(v => v.Value.Value)));
            }
            Assert.True(distinctStates.Count > 1);
            Assert.Equal(first.Values, device.ReadTelemetry().Values);
            device.Shutdown();
        }
    }
    [Fact] public void HarnessPollsSensorAndVerifiesControllerCommandThroughContracts()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "active,message\nfalse,\n");
            IDevice sensor = new SimulatedSensor();
            IActuator sign = new VariableWarningSignController("S-03", "R-01", path, ["ICY ROAD AHEAD"]);
            var harness = new IntegrationHarness([sensor, sign]);
            harness.Initialize();
            var before = harness.Poll();
            Assert.Equal(-5d, before[0].Values["road_temperature"].Value);
            Assert.Equal(false, before[1].Values["sign_active"].Value);
            var command = new ControlCommand(Guid.NewGuid(), "S-03", "SetMessage", "ICY ROAD AHEAD", "Naitik", DateTimeOffset.UtcNow);
            Assert.Equal(CommandOutcome.Success, harness.Execute(command).Outcome);
            Assert.Equal("ICY ROAD AHEAD", harness.Poll()[1].Values["sign_message"].Value);
            harness.Shutdown(); Assert.Equal(DeviceStatus.Offline, sign.Status);
        }
        finally { File.Delete(path); }
    }
    // Test-only sensor: does not replace Muhammad's or Keshav's production modules.
    private sealed class SimulatedSensor : ISensor
    {
        public string DeviceId => "test-temperature";
        public string Name => "Test temperature sensor";
        public DeviceStatus Status { get; private set; }
        public event Action<DeviceEvent>? EventRaised { add { } remove { } }
        public void Initialize() => Status = DeviceStatus.Online;
        public void Shutdown() => Status = DeviceStatus.Offline;
        public TelemetryPacket ReadTelemetry() => new(DeviceId, "TestSensor", "R-01", DateTimeOffset.UtcNow,
            new Dictionary<string, TelemetryValue> { ["road_temperature"] = new(-5d, "deg C") }, TelemetryQuality.Ok);
    }
}
internal sealed class IntegrationHarness(IDevice[] devices)
{
    public void Initialize() { }
    public TelemetryPacket[] Poll() => throw new NotImplementedException();
    public CommandResult Execute(ControlCommand command) => throw new NotImplementedException();
    public void Shutdown() { }
}
