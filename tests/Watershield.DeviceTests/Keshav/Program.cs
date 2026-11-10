using Watershield.Devices.Keshav;

string csvPath = Path.GetTempFileName();

try
{
    File.WriteAllLines(csvPath, ["snow_depth_cm", "12.4"]);

    var sensor = new SnowDepthSensor();
    double actual = sensor.ReadSnowDepthCm(csvPath);
    const double expected = 12.4;

    if (actual != expected)
    {
        Console.Error.WriteLine($"FAIL: Expected {expected} cm, got {actual} cm.");
        return 1;
    }

    Console.WriteLine($"PASS: Read {actual} cm from the first data row.");
    return 0;
}
finally
{
    File.Delete(csvPath);
}
