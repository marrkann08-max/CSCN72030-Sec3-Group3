using Watershield.Core.Contracts;
using Watershield.Devices;
using Watershield.Devices.Muhammad;

namespace Watershield.MuhammadTests;

// runs the real scenario csvs through the real sensors, so a typo in a data file gets caught here
public class ScenarioDataTests
{
    public static TheoryData<string, string> Files()
    {
        var data = new TheoryData<string, string>();
        foreach (var scenario in new[] { "light-snow", "heavy-snow", "freezing-rain" })
            foreach (var file in new[] { "road-temperature.csv", "surface-temperature.csv", "weather-station.csv" })
                data.Add(scenario, file);
        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void ScenarioFile_HasSixtyRows_AndReadsCleanly(string scenario, string file)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "scenarios", scenario, file);
        CsvWeatherSensor sensor = file switch
        {
            "road-temperature.csv" => new RoadTemperatureSensor("RT-01", scenario, path),
            "surface-temperature.csv" => new SurfaceTemperatureSensor("ST-01", scenario, path),
            _ => new WeatherStation("WS-01", scenario, path),
        };
        int events = 0;
        sensor.EventRaised += _ => events++;
        sensor.Initialize();

        // two full loops so the wrap-around gets exercised too
        for (int i = 0; i < 120; i++)
            Assert.Equal(TelemetryQuality.Ok, sensor.ReadTelemetry().Quality);

        Assert.Equal(DeviceStatus.Online, sensor.Status);
        Assert.Equal(0, events);

        var header = File.ReadLines(path).First().Split(',');
        Assert.Equal(60, new LoopedCsvDataSource(path, header).Count);
    }
}
