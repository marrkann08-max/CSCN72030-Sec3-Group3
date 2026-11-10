using Watershield.Core.Contracts;
using Watershield.Devices.Muhammad;

namespace Watershield.MuhammadTests;

public class RoadTemperatureSensorTests
{
    [Fact]
    public void ReadsNegativeValues_SkipsBadRow_AndLoops()
    {
        using var csv = new TestCsv("road_temperature_c\n-8.4\nbad\n0.2\n");
        var sensor = new RoadTemperatureSensor("RT-01", "SEG-A", csv.Path);
        int events = 0;
        sensor.EventRaised += _ => events++;
        sensor.Initialize();

        var first = sensor.ReadTelemetry();
        var second = sensor.ReadTelemetry();
        var third = sensor.ReadTelemetry();

        Assert.Equal(-8.4, (double)first.Values["road_temperature"].Value);
        Assert.Equal("°C", first.Values["road_temperature"].Unit);
        Assert.Equal(TelemetryQuality.Ok, first.Quality);
        Assert.Equal("RT-01", first.DeviceId);
        Assert.Equal("SEG-A", first.SegmentId);
        Assert.Equal(0.2, (double)second.Values["road_temperature"].Value);
        Assert.Equal(-8.4, (double)third.Values["road_temperature"].Value);
        Assert.Equal(1, events);
        Assert.Equal(DeviceStatus.Online, sensor.Status);
    }
}
