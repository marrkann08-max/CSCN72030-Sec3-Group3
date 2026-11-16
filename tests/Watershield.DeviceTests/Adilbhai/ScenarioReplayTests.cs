using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Core.Contracts;
using Watershield.Devices.Adilbhai;

namespace Watershield.DeviceTests.Adilbhai;

[TestClass]
public sealed class ScenarioReplayTests
{
    [TestMethod]
    [DataRow("light-snow")]
    [DataRow("heavy-snow")]
    [DataRow("freezing-rain")]
    public void ReplaysFiftyValidReadingsFromEachRoadSensor(string scenario)
    {
        string frictionPath = ScenarioFile(scenario, "road-friction.csv");
        string conditionPath = ScenarioFile(scenario, "road-condition.csv");
        Assert.AreEqual(50, File.ReadAllLines(frictionPath).Length - 1);
        Assert.AreEqual(50, File.ReadAllLines(conditionPath).Length - 1);

        var friction = new RoadFrictionSensor("FR-01", "R1", frictionPath);
        var condition = new RoadConditionSensor("RC-01", "R1", conditionPath);
        friction.Initialize();
        condition.Initialize();

        for (int row = 0; row < 50; row++)
        {
            Assert.AreEqual(TelemetryQuality.Ok, friction.ReadTelemetry().Quality, $"Friction row {row + 1}");
            Assert.AreEqual(TelemetryQuality.Ok, condition.ReadTelemetry().Quality, $"Condition row {row + 1}");
        }
    }

    [TestMethod]
    [DataRow("light-snow")]
    [DataRow("heavy-snow")]
    [DataRow("freezing-rain")]
    public void ReplaysFiftyValidSaltRates(string scenario)
    {
        string path = ScenarioFile(scenario, "salt-spreader.csv");
        Assert.AreEqual(50, File.ReadAllLines(path).Length - 1);

        var controller = new SaltSpreaderController("SALT-01", "R1", path);
        controller.Initialize();

        for (int row = 0; row < 50; row++)
        {
            TelemetryPacket reading = controller.ReadTelemetry();
            Assert.AreEqual(TelemetryQuality.Ok, reading.Quality, $"Salt row {row + 1}");
            int rate = (int)reading.Values["salt_rate"].Value;
            Assert.IsTrue(rate is >= 0 and <= 100 && rate % 5 == 0, $"Invalid rate {rate} at row {row + 1}");
        }
    }

    private static string ScenarioFile(string scenario, string filename)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "scenarios", scenario, filename);
        Assert.IsTrue(File.Exists(path), $"Missing scenario file: {path}");
        return path;
    }
}
