using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Core.Contracts;
using Watershield.Devices.Adilbhai;

namespace Watershield.DeviceTests.Adilbhai;

[TestClass]
public sealed class SaltSpreaderControllerTests
{
    [TestMethod]
    public void CommandPersistsThroughCsvLoopAndClearsOnReset()
    {
        string path = WriteTestCsv("salt_rate_percent\n0\n20\n");

        try
        {
            var controller = new SaltSpreaderController("salt", "R1", path);
            controller.Initialize();

            Assert.AreEqual(0, (int)controller.ReadTelemetry().Values["salt_rate"].Value);

            CommandResult result = controller.Execute(Command("60"));
            Assert.AreEqual(CommandOutcome.Success, result.Outcome);
            Assert.AreEqual("60", result.NewState);

            Assert.AreEqual(60, (int)controller.ReadTelemetry().Values["salt_rate"].Value);
            Assert.AreEqual(60, (int)controller.ReadTelemetry().Values["salt_rate"].Value);

            controller.ResetScenario();
            Assert.AreEqual(0, (int)controller.ReadTelemetry().Values["salt_rate"].Value);
            Assert.AreEqual(20, (int)controller.ReadTelemetry().Values["salt_rate"].Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    [DataRow("-5")]
    [DataRow("101")]
    [DataRow("52")]
    [DataRow("bad")]
    public void RejectsInvalidRatesWithoutChangingTheCurrentRate(string value)
    {
        string path = WriteTestCsv("salt_rate_percent\n20\n");

        try
        {
            var controller = new SaltSpreaderController("salt", "R1", path);
            controller.Initialize();
            controller.ReadTelemetry();

            Assert.AreEqual(CommandOutcome.Rejected, controller.Execute(Command(value)).Outcome);
            Assert.AreEqual(20, controller.RatePercent);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void RejectsCommandsWhenOffline()
    {
        string path = WriteTestCsv("salt_rate_percent\n20\n");

        try
        {
            var controller = new SaltSpreaderController("salt", "R1", path);
            controller.Initialize();
            controller.Shutdown();

            Assert.AreEqual(CommandOutcome.Rejected, controller.Execute(Command("40")).Outcome);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void FaultsWhenEveryCsvRowIsInvalid()
    {
        string path = WriteTestCsv("salt_rate_percent\n52\nbad\n");

        try
        {
            var controller = new SaltSpreaderController("salt", "R1", path);
            controller.Initialize();

            TelemetryPacket reading = controller.ReadTelemetry();
            Assert.AreEqual(TelemetryQuality.Fault, reading.Quality);
            Assert.AreEqual(DeviceStatus.Fault, controller.Status);
            Assert.AreEqual(0, reading.Values.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void RejectsCommandsWithoutRequiredIdentityOrSupportedAction()
    {
        string path = WriteTestCsv("salt_rate_percent\n20\n");

        try
        {
            var controller = new SaltSpreaderController("salt", "R1", path);
            controller.Initialize();

            Assert.AreEqual(CommandOutcome.Rejected,
                controller.Execute(Command("40") with { CommandId = Guid.Empty }).Outcome);
            Assert.AreEqual(CommandOutcome.Rejected,
                controller.Execute(Command("40") with { IssuedBy = " " }).Outcome);
            Assert.AreEqual(CommandOutcome.Rejected,
                controller.Execute(Command("40") with { TargetDeviceId = "other" }).Outcome);
            Assert.AreEqual(CommandOutcome.Rejected,
                controller.Execute(Command("40") with { Action = "Other" }).Outcome);
            Assert.AreEqual(0, controller.RatePercent);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ControlCommand Command(string value) =>
        new(Guid.NewGuid(), "salt", "SetSaltRate", value, "Adilbhai", DateTimeOffset.UtcNow);

    private static string WriteTestCsv(string content)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }
}
