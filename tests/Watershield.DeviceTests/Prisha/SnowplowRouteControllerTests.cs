using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Core.Contracts;
using Watershield.Devices.Prisha;

namespace Watershield.DeviceTests.Prisha;

[TestClass]
public sealed class SnowplowRouteControllerTests
{
    [TestMethod]
    public void AssignsAnAllowedRouteToTheTargetPlow()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath, ["route_id", "R-01"]);

            var controller = new SnowplowRouteController(
                "PLOW-04", "R1", csvPath, ["R-01", "R-02"]);
            controller.Initialize();

            var command = new ControlCommand(
                Guid.NewGuid(),
                "PLOW-04",
                "AssignRoute",
                "R-02",
                "Prisha",
                DateTimeOffset.UtcNow);

            CommandResult result = controller.Execute(command);

            Assert.AreEqual(CommandOutcome.Success, result.Outcome);
            Assert.AreEqual("R-02", controller.AssignedRoute);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    public void RejectsRouteAssignmentWithoutAnIssuingUser()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath, ["route_id", "R-01"]);

            var controller = new SnowplowRouteController(
                "PLOW-04", "R1", csvPath, ["R-01", "R-02"]);
            controller.Initialize();

            var command = new ControlCommand(
                Guid.NewGuid(),
                "PLOW-04",
                "AssignRoute",
                "R-02",
                "",
                DateTimeOffset.UtcNow);

            CommandResult result = controller.Execute(command);

            Assert.AreEqual(CommandOutcome.Rejected, result.Outcome);
            Assert.IsNull(controller.AssignedRoute);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    public void RejectsRouteAssignmentWithoutACommandId()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath, ["route_id", "R-01"]);

            var controller = new SnowplowRouteController(
                "PLOW-04", "R1", csvPath, ["R-01", "R-02"]);
            controller.Initialize();

            var command = new ControlCommand(
                Guid.Empty,
                "PLOW-04",
                "AssignRoute",
                "R-02",
                "Prisha",
                DateTimeOffset.UtcNow);

            CommandResult result = controller.Execute(command);

            Assert.AreEqual(CommandOutcome.Rejected, result.Outcome);
            Assert.IsNull(controller.AssignedRoute);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    public void KeepsAssignedRouteWhenCsvLoops()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath, ["route_id", "R-01", "R-01"]);

            var controller = new SnowplowRouteController(
                "PLOW-04", "R1", csvPath, ["R-01", "R-02"]);
            controller.Initialize();

            var command = new ControlCommand(
                Guid.NewGuid(), "PLOW-04", "AssignRoute", "R-02",
                "Prisha", DateTimeOffset.UtcNow);

            Assert.AreEqual(CommandOutcome.Success, controller.Execute(command).Outcome);

            for (int poll = 0; poll < 3; poll++)
            {
                TelemetryPacket reading = controller.ReadTelemetry();
                Assert.AreEqual("R-02", (string)reading.Values["route_id"].Value);
            }
        }
        finally
        {
            File.Delete(csvPath);
        }
    }
}