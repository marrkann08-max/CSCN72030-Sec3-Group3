using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Core.Contracts;
using Watershield.Devices.Prisha;

namespace Watershield.DeviceTests.Prisha;

[TestClass]
public sealed class HumiditySensorTests
{
    [TestMethod]
    public void ReadsValidHumidityWithPercentUnit()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath,
                ["relative_humidity_percent", "82"]);

            var sensor = new HumiditySensor("HUM-01", "R1", csvPath);
            sensor.Initialize();

            TelemetryPacket reading = sensor.ReadTelemetry();
            TelemetryValue humidity = reading.Values["relative_humidity"];

            Assert.AreEqual(82d, (double)humidity.Value);
            Assert.AreEqual("%", humidity.Unit);
            Assert.AreEqual(TelemetryQuality.Ok, reading.Quality);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    public void SkipsHumidityAboveOneHundredPercent()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath,
                ["relative_humidity_percent", "101", "82"]);

            var sensor = new HumiditySensor("HUM-01", "R1", csvPath);
            sensor.Initialize();

            TelemetryPacket reading = sensor.ReadTelemetry();

            Assert.AreEqual(82d, (double)reading.Values["relative_humidity"].Value);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(100)]
    public void AcceptsHumidityAtRangeBoundary(int percent)
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath,
                ["relative_humidity_percent", percent.ToString()]);

            var sensor = new HumiditySensor("HUM-01", "R1", csvPath);
            sensor.Initialize();

            TelemetryPacket reading = sensor.ReadTelemetry();

            Assert.AreEqual(TelemetryQuality.Ok, reading.Quality);
            Assert.AreEqual(
                (double)percent,
                (double)reading.Values["relative_humidity"].Value);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    public void SkipsInvalidHumidityReportsEventAndLoops()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath,
                ["relative_humidity_percent", "82", "NaN", "93"]);

            var sensor = new HumiditySensor("HUM-01", "R1", csvPath);
            int eventsRaised = 0;
            sensor.EventRaised += _ => eventsRaised++;
            sensor.Initialize();

            Assert.AreEqual(82d, (double)sensor.ReadTelemetry().Values["relative_humidity"].Value);
            Assert.AreEqual(93d, (double)sensor.ReadTelemetry().Values["relative_humidity"].Value);
            Assert.AreEqual(1, eventsRaised);
            Assert.AreEqual(82d, (double)sensor.ReadTelemetry().Values["relative_humidity"].Value);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    [DataRow("light-snow")]
    [DataRow("heavy-snow")]
    [DataRow("freezing-rain")]
    public void ReadsEveryHumidityRowInScenario(string scenario)
    {
        string path = Path.Combine(
            AppContext.BaseDirectory, "scenarios", scenario, "humidity.csv");

        Assert.IsTrue(File.Exists(path), $"Missing scenario file: {path}");

        int rowCount = File.ReadAllLines(path).Length - 1;
        Assert.AreEqual(50, rowCount, $"{scenario} must have 50 humidity readings.");

        var sensor = new HumiditySensor("HUM-01", "R1", path);
        int eventsRaised = 0;
        sensor.EventRaised += _ => eventsRaised++;
        sensor.Initialize();

        for (int row = 0; row < rowCount; row++)
        {
            Assert.AreEqual(
                TelemetryQuality.Ok,
                sensor.ReadTelemetry().Quality,
                $"{scenario}, data row {row + 1}");
        }

        Assert.AreEqual(0, eventsRaised, $"{scenario} should load without warnings.");
    }
}
