using Watershield.Core.Contracts;
using Watershield.Devices.Muhammad;

namespace Watershield.MuhammadTests;

public class WeatherStationTests
{
    [Fact]
    public void ReportsAirWindAndPrecip_SkipsNegativeWindAndUnknownPrecip()
    {
        using var csv = new TestCsv(
            "air_temperature_c,wind_speed_kmh,precipitation\n" +
            "-7,22,snow\n" +
            "-6,-1,rain\n" +      // negative wind, should be skipped
            "-6,10,unknown\n" +   // not a precipitation type we know
            "-5,0,none\n");
        var station = new WeatherStation("WS-01", "SEG-A", csv.Path);
        int events = 0;
        station.EventRaised += _ => events++;
        station.Initialize();

        var first = station.ReadTelemetry();
        Assert.Equal(-7.0, (double)first.Values["air_temperature"].Value);
        Assert.Equal("°C", first.Values["air_temperature"].Unit);
        Assert.Equal(22.0, (double)first.Values["wind_speed"].Value);
        Assert.Equal("km/h", first.Values["wind_speed"].Unit);
        Assert.Equal("snow", (string)first.Values["precipitation"].Value);

        var second = station.ReadTelemetry();
        Assert.Equal(0.0, (double)second.Values["wind_speed"].Value);
        Assert.Equal("none", (string)second.Values["precipitation"].Value);
        Assert.Equal(2, events);

        var third = station.ReadTelemetry();
        Assert.Equal("snow", (string)third.Values["precipitation"].Value);
    }
}
