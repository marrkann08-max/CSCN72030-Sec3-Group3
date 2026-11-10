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
}