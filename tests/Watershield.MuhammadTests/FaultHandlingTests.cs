using Watershield.Core.Contracts;
using Watershield.Devices.Muhammad;

namespace Watershield.MuhammadTests;

// the HMI keeps polling no matter what, so bad data should show up as Fault instead of crashing it
public class FaultHandlingTests
{
    [Fact]
    public void WeatherStation_EveryRowInvalid_ReportsFault()
    {
        using var csv = new TestCsv(
            "air_temperature_c,wind_speed_kmh,precipitation\n" +
            "bad,22,snow\n" +
            "-7,-1,snow\n");
        var station = new WeatherStation("WS-01", "SEG-A", csv.Path);
        station.Initialize();

        var reading = station.ReadTelemetry();

        Assert.Equal(TelemetryQuality.Fault, reading.Quality);
        Assert.Equal(DeviceStatus.Fault, station.Status);
        Assert.Empty(reading.Values);
    }

    [Fact]
    public void RoadSensor_WrongHeader_GoesToFaultOnInitialize()
    {
        using var csv = new TestCsv("wrong_header\n-8.4\n");
        var sensor = new RoadTemperatureSensor("RT-01", "SEG-A", csv.Path);

        sensor.Initialize();

        Assert.Equal(DeviceStatus.Fault, sensor.Status);
    }
}
