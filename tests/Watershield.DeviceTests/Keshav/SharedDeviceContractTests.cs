using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Core.Contracts;
using Watershield.Devices.Keshav;

namespace Watershield.DeviceTests.Keshav;

[TestClass]
public sealed class SharedDeviceContractTests
{
    [TestMethod]
    public void SensorPublishesCentimetresAndStopsAfterShutdown()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, ["snow_depth_cm", "12.4", "8.1"]);
            ISensor sensor = new SnowDepthSensor("snow", "R1", path);
            sensor.Initialize();

            var packet = sensor.ReadTelemetry();
            Assert.AreEqual(DeviceStatus.Online, sensor.Status);
            Assert.AreEqual("snow", packet.DeviceId);
            Assert.AreEqual("R1", packet.SegmentId);
            Assert.AreEqual(TelemetryQuality.Ok, packet.Quality);
            Assert.AreEqual(new TelemetryValue(12.4, "cm"), packet.Values["snow_depth"]);
            Assert.AreEqual(new TelemetryValue(8.1, "cm"), sensor.ReadTelemetry().Values["snow_depth"]);

            sensor.Shutdown();
            Assert.AreEqual(TelemetryQuality.Fault, sensor.ReadTelemetry().Quality);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void BladeReportsAcceptedCommandAndRejectsWrongTarget()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, ["blade_state", "UP"]);
            IActuator blade = new SnowplowBladeController("blade", "R1", path);
            blade.Initialize();
            var command = new ControlCommand(Guid.NewGuid(), "blade", "SetBlade", "DOWN",
                "operator", DateTimeOffset.UtcNow);

            Assert.AreEqual(CommandOutcome.Success, blade.Execute(command).Outcome);
            Assert.AreEqual(new TelemetryValue("DOWN", ""), blade.ReadTelemetry().Values["blade_state"]);

            var wrongTarget = command with { TargetDeviceId = "other", Parameter = "UP" };
            Assert.AreEqual(CommandOutcome.Rejected, blade.Execute(wrongTarget).Outcome);
            Assert.AreEqual(new TelemetryValue("DOWN", ""), blade.ReadTelemetry().Values["blade_state"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
