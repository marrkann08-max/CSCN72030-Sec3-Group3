using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Core.Contracts;
using Watershield.Devices.Prisha;

namespace Watershield.DeviceTests.Prisha;

[TestClass]
public sealed class VehicleTelemetryModuleTests
{
    [TestMethod]
    public void ReadsValidVehicleTelemetry()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath,
            [
                "vehicle_id,latitude,longitude,speed_kmh,vehicle_status",
                "PLOW-04,43.45,-80.49,24,DISPATCHED"
            ]);

            var sensor = new VehicleTelemetryModule("GPS-01", "R1", csvPath);
            sensor.Initialize();

            TelemetryPacket reading = sensor.ReadTelemetry();

            Assert.AreEqual(TelemetryQuality.Ok, reading.Quality);
            Assert.AreEqual("PLOW-04", (string)reading.Values["vehicle_id"].Value);
            Assert.AreEqual(43.45d, (double)reading.Values["latitude"].Value);
            Assert.AreEqual(-80.49d, (double)reading.Values["longitude"].Value);
            Assert.AreEqual(24d, (double)reading.Values["speed"].Value);
            Assert.AreEqual("DISPATCHED", (string)reading.Values["vehicle_status"].Value);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    public void SkipsVehicleTelemetryWithLatitudeAboveNinety()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath,
            [
                "vehicle_id,latitude,longitude,speed_kmh,vehicle_status",
            "PLOW-04,91,-80.49,24,DISPATCHED",
            "PLOW-04,43.45,-80.49,24,DISPATCHED"
            ]);

            var sensor = new VehicleTelemetryModule("GPS-01", "R1", csvPath);
            sensor.Initialize();

            TelemetryPacket reading = sensor.ReadTelemetry();

            Assert.AreEqual(43.45d, (double)reading.Values["latitude"].Value);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    public void SkipsVehicleTelemetryWithLongitudeAboveOneHundredEighty()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath,
            [
                "vehicle_id,latitude,longitude,speed_kmh,vehicle_status",
            "PLOW-04,43.45,181,24,DISPATCHED",
            "PLOW-04,43.45,-80.49,24,DISPATCHED"
            ]);

            var sensor = new VehicleTelemetryModule("GPS-01", "R1", csvPath);
            sensor.Initialize();

            TelemetryPacket reading = sensor.ReadTelemetry();

            Assert.AreEqual(-80.49d, (double)reading.Values["longitude"].Value);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [TestMethod]
    public void SkipsVehicleTelemetryWithNegativeSpeed()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath,
            [
                "vehicle_id,latitude,longitude,speed_kmh,vehicle_status",
            "PLOW-04,43.45,-80.49,-1,DISPATCHED",
            "PLOW-04,43.45,-80.49,24,DISPATCHED"
            ]);

            var sensor = new VehicleTelemetryModule("GPS-01", "R1", csvPath);
            sensor.Initialize();

            TelemetryPacket reading = sensor.ReadTelemetry();

            Assert.AreEqual(24d, (double)reading.Values["speed"].Value);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }
}