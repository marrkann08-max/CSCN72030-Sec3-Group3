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
}