using Watershield.Core.Contracts;
using Watershield.Devices;
using Watershield.Devices.Naitik;
namespace Watershield.NaitikTests;
public sealed class RobustnessTests : IDisposable
{
    private readonly string path = Path.GetTempFileName();
    public void Dispose() => File.Delete(path);
    private VariableWarningSignController Sign(string csv)
    {
        File.WriteAllText(path, csv);
        return new("sign", "R-01", path, ["SLOW DOWN"], new FixedClock());
    }
    private static ControlCommand Command() => new(Guid.NewGuid(), "sign", "SetMessage", "SLOW DOWN", "Naitik", DateTimeOffset.UtcNow);
    [Theory]
    [InlineData("wrong,header\nfalse,\n")]
    [InlineData("active,message\n")]
    [InlineData("")]
    public void InvalidFilesFaultAndReport(string csv)
    {
        var sign = Sign(csv); var events = new List<DeviceEvent>(); sign.EventRaised += events.Add;
        sign.Initialize(); Assert.Equal(DeviceStatus.Fault, sign.Status); Assert.Single(events);
        Assert.Throws<InvalidOperationException>(() => sign.ReadTelemetry());
    }
    [Fact] public void MissingFileCanBeRepairedAndReinitialized()
    {
        var sign = Sign("active,message\nfalse,\n"); File.Delete(path);
        sign.Initialize(); Assert.Equal(DeviceStatus.Fault, sign.Status);
        File.WriteAllText(path, "active,message\nfalse,\n"); sign.Initialize();
        Assert.Equal(TelemetryQuality.Ok, sign.ReadTelemetry().Quality);
    }
    [Fact] public void InvalidRowsAreLoggedAndSkippedWithDeterministicUtcTime()
    {
        var sign = Sign("active,message\nbad,\ntrue,UNKNOWN\nfalse,\n");
        var events = new List<DeviceEvent>(); sign.EventRaised += events.Add; sign.Initialize();
        var packet = sign.ReadTelemetry(); Assert.Equal(TelemetryQuality.Ok, packet.Quality);
        Assert.Equal(2, events.Count); Assert.Equal(FixedClock.Now, packet.TimestampUtc);
        Assert.All(events, e => Assert.Equal(FixedClock.Now, e.TimestampUtc));
    }
    [Fact] public void AllInvalidRowsTerminateWithFaultAndCommandsAreRejected()
    {
        var sign = Sign("active,message\nbad,\ntrue,UNKNOWN\n"); sign.Initialize();
        Assert.Equal(TelemetryQuality.Fault, sign.ReadTelemetry().Quality);
        Assert.Equal(DeviceStatus.Fault, sign.Status);
        Assert.Equal(CommandOutcome.Rejected, sign.Execute(Command()).Outcome);
    }
    [Fact] public void LifecycleRejectsCommandsBeforePollAndAfterShutdown()
    {
        var sign = Sign("active,message\nfalse,\n");
        Assert.Equal(DeviceStatus.Offline, sign.Status);
        Assert.Throws<InvalidOperationException>(() => sign.ResetScenario());
        Assert.Throws<InvalidOperationException>(() => sign.ReadTelemetry());
        Assert.Equal(CommandOutcome.Rejected, sign.Execute(Command()).Outcome);
        sign.Initialize(); Assert.Equal(CommandOutcome.Rejected, sign.Execute(Command()).Outcome);
        sign.ReadTelemetry(); Assert.Equal(CommandOutcome.Success, sign.Execute(Command()).Outcome);
        sign.Shutdown(); Assert.Equal(DeviceStatus.Offline, sign.Status);
        Assert.Equal(CommandOutcome.Rejected, sign.Execute(Command()).Outcome);
        sign.Initialize(); Assert.Equal(false, sign.ReadTelemetry().Values["sign_active"].Value);
    }
    [Theory] [InlineData("target")] [InlineData("id")] [InlineData("user")]
    public void InvalidCommandMetadataIsRejected(string field)
    {
        var sign = Sign("active,message\nfalse,\n"); sign.Initialize(); sign.ReadTelemetry();
        var c = Command();
        c = field switch { "target" => c with { TargetDeviceId = "other" }, "id" => c with { CommandId = Guid.Empty }, _ => c with { IssuedBy = " " } };
        Assert.Equal(CommandOutcome.Rejected, sign.Execute(c).Outcome);
        Assert.Throws<ArgumentNullException>(() => sign.Execute(null!));
    }
    [Theory]
    [InlineData("UNKNOWN,")] [InlineData("AVAILABLE,R-01")]
    [InlineData("DISPATCHED,R-99")] [InlineData("RETURNING,")] [InlineData("AVAILABLE,,")]
    public void InvalidDispatchRowsFault(string row)
    {
        File.WriteAllText(path, "vehicle_status,destination_segment\n" + row + "\n");
        var device = new DispatchController("PLOW-04", path, ["R-01"]); device.Initialize();
        Assert.Equal(TelemetryQuality.Fault, device.ReadTelemetry().Quality);
    }
    [Fact] public void CsvRowsCannotBeMutatedByConsumer()
    {
        File.WriteAllText(path, "value\noriginal\n");
        var source = new LoopedCsvDataSource(path, "value"); source.Next()[0] = "changed";
        Assert.Equal("original", source.Next()[0]);
    }
    [Fact] public void PresetsMustContainOnlyUsableMessages()
    {
        Assert.Throws<ArgumentException>(() => new VariableWarningSignController("sign", "R-01", path, [""]));
        Assert.Throws<ArgumentException>(() => new DispatchController("vehicle", path, [" "]));
    }
    private sealed class FixedClock : TimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
