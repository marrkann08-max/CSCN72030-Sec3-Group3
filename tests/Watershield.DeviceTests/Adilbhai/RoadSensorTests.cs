using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Core.Contracts;
using Watershield.Devices.Adilbhai;

namespace Watershield.DeviceTests.Adilbhai;

[TestClass]
public sealed class RoadFrictionSensorTests
{
    [TestMethod]
    public void AcceptsBoundaryValuesSkipsInvalidValuesAndLoops()
    {
        string path = WriteTestCsv("friction_coefficient\n0\n-0.1\nNaN\n1.2\n1\n");

        try
        {
            var sensor = new RoadFrictionSensor("friction", "R1", path);
            int invalidRows = 0;
            sensor.EventRaised += _ => invalidRows++;
            sensor.Initialize();

            Assert.AreEqual(0d, (double)sensor.ReadTelemetry().Values["road_friction"].Value);
            Assert.AreEqual(1d, (double)sensor.ReadTelemetry().Values["road_friction"].Value);
            Assert.AreEqual(0d, (double)sensor.ReadTelemetry().Values["road_friction"].Value);
            Assert.AreEqual(3, invalidRows);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    [DataRow(0d)]
    [DataRow(1d)]
    public void AcceptsFrictionAtRangeBoundaries(double coefficient)
    {
        string path = WriteTestCsv($"friction_coefficient\n{coefficient.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");

        try
        {
            var sensor = new RoadFrictionSensor("friction", "R1", path);
            sensor.Initialize();

            TelemetryPacket reading = sensor.ReadTelemetry();
            Assert.AreEqual(TelemetryQuality.Ok, reading.Quality);
            Assert.AreEqual(coefficient, (double)reading.Values["road_friction"].Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTestCsv(string content)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }
}

[TestClass]
public sealed class RoadConditionSensorTests
{
    [TestMethod]
    public void NormalizesKnownConditionsAndSkipsUnknownConditions()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "road_condition\nwet\nunknown\nIce\n");
            var sensor = new RoadConditionSensor("condition", "R1", path);
            int invalidRows = 0;
            sensor.EventRaised += _ => invalidRows++;
            sensor.Initialize();

            Assert.AreEqual("WET", (string)sensor.ReadTelemetry().Values["road_condition"].Value);
            Assert.AreEqual("ICE", (string)sensor.ReadTelemetry().Values["road_condition"].Value);
            Assert.AreEqual("WET", (string)sensor.ReadTelemetry().Values["road_condition"].Value);
            Assert.AreEqual(1, invalidRows);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    [DataRow("DRY")]
    [DataRow("WET")]
    [DataRow("SNOW")]
    [DataRow("ICE")]
    public void AcceptsSupportedRoadConditions(string condition)
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, $"road_condition\n{condition}\n");
            var sensor = new RoadConditionSensor("condition", "R1", path);
            sensor.Initialize();

            Assert.AreEqual(condition, (string)sensor.ReadTelemetry().Values["road_condition"].Value);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
