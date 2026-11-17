using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Core.Contracts;
using Watershield.Devices.Prisha;

namespace Watershield.DeviceTests.Prisha;

[TestClass]
public sealed class ScenarioReplayTests
{
    [TestMethod]
    [DataRow("light-snow")]
    [DataRow("heavy-snow")]
    [DataRow("freezing-rain")]
    public void ReadsEveryVehicleRow(string scenario)
    {
        string path = ScenarioFile(scenario, "vehicle-telemetry.csv");
        Assert.AreEqual(50, File.ReadAllLines(path).Length - 1);

        var sensor = new VehicleTelemetryModule("GPS-01", "R1", path);
        var events = new List<DeviceEvent>();
        sensor.EventRaised += events.Add;
        sensor.Initialize();

        for (int row = 0; row < 50; row++)
            Assert.AreEqual(TelemetryQuality.Ok, sensor.ReadTelemetry().Quality, $"Row {row + 1}");

        Assert.HasCount(0, events);
    }

    [TestMethod]
    [DataRow("light-snow")]
    [DataRow("heavy-snow")]
    [DataRow("freezing-rain")]
    public void ReplaysEveryRouteInOrder(string scenario)
    {
        string path = ScenarioFile(scenario, "snowplow-route.csv");
        string[] routes = File.ReadAllLines(path).Skip(1).ToArray();
        Assert.HasCount(50, routes);

        var controller = new SnowplowRouteController("PLOW-04", "R1", path, ["R-01", "R-02"]);
        controller.Initialize();

        for (int row = 0; row <= routes.Length; row++)
        {
            var reading = controller.ReadTelemetry();
            Assert.AreEqual(TelemetryQuality.Ok, reading.Quality, $"Poll {row + 1}");
            Assert.AreEqual(routes[row % routes.Length], (string)reading.Values["route_id"].Value);
        }
    }

    private static string ScenarioFile(string scenario, string filename)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "scenarios", scenario, filename);
        Assert.IsTrue(File.Exists(path), $"Missing scenario file: {path}");
        return path;
    }
}
