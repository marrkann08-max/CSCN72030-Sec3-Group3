using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Devices.Keshav;

namespace Watershield.DeviceTests.Keshav;

[TestClass]
public sealed class SnowDepthSensorTests
{
    [TestMethod]
    public void ReadsValidRowsInOrderAndLoops()
    {
        AssertReadings(["12.4", "8.1"], [12.4, 8.1, 12.4]);
    }

    [TestMethod]
    public void SkipsInvalidRowsAndKeepsLooping()
    {
        AssertReadings(
            ["12.4", "bad", "-2", "NaN", "Infinity", "8.1"],
            [12.4, 8.1, 12.4]);
    }

    [TestMethod]
    public void RejectsCsvWithNoUsableReadings()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath, ["snow_depth_cm", "bad", "-2"]);
            var sensor = new SnowDepthSensor();

            Assert.ThrowsExactly<InvalidDataException>(
                () => sensor.ReadSnowDepthCm(csvPath));
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    private static void AssertReadings(string[] rows, double[] expectedReadings)
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath, ["snow_depth_cm", .. rows]);
            var sensor = new SnowDepthSensor();

            for (int index = 0; index < expectedReadings.Length; index++)
            {
                double actual = sensor.ReadSnowDepthCm(csvPath);
                Assert.AreEqual(expectedReadings[index], actual, $"Poll {index + 1}.");
            }
        }
        finally
        {
            File.Delete(csvPath);
        }
    }
}
