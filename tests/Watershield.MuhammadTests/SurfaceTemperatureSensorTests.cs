using Watershield.Core.Contracts;
using Watershield.Devices.Muhammad;

namespace Watershield.MuhammadTests;

public class SurfaceTemperatureSensorTests
{
    [Fact]
    public void SkipsNaN_AndLoopsBackToFirstRow()
    {
        using var csv = new TestCsv("surface_temperature_c\n-9.1\nNaN\n-2.5\n");
        var sensor = new SurfaceTemperatureSensor("ST-01", "SEG-A", csv.Path);
        int events = 0;
        sensor.EventRaised += _ => events++;
        sensor.Initialize();

        var first = sensor.ReadTelemetry();
        var second = sensor.ReadTelemetry();
        var third = sensor.ReadTelemetry();

        Assert.Equal(-9.1, (double)first.Values["surface_temperature"].Value);
        Assert.Equal("°C", first.Values["surface_temperature"].Unit);
        Assert.Equal(-2.5, (double)second.Values["surface_temperature"].Value); // NaN row skipped
        Assert.Equal(-9.1, (double)third.Values["surface_temperature"].Value);
        Assert.Equal(TelemetryQuality.Ok, third.Quality);
        Assert.Equal(1, events);
    }
}
