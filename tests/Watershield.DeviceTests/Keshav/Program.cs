using Watershield.Devices.Keshav;

try
{
    CheckReadings("Loop through valid rows", ["12.4", "8.1"], [12.4, 8.1, 12.4]);
    CheckReadings(
        "Skip invalid rows and keep looping",
        ["12.4", "bad", "-2", "NaN", "Infinity", "8.1"],
        [12.4, 8.1, 12.4]);

    string invalidPath = WriteCsv(["bad", "-2"]);
    try
    {
        try
        {
            new SnowDepthSensor().ReadSnowDepthCm(invalidPath);
            throw new Exception("Expected a CSV with no usable readings to fail.");
        }
        catch (InvalidDataException)
        {
            Console.WriteLine("PASS: Reject a CSV with no usable readings");
        }
    }
    finally
    {
        File.Delete(invalidPath);
    }

    SnowplowBladeControllerChecks.Run();
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"FAIL: {exception.Message}");
    return 1;
}

static void CheckReadings(string name, string[] rows, double[] expectedReadings)
{
    string csvPath = WriteCsv(rows);

    try
    {
        var sensor = new SnowDepthSensor();

        for (int index = 0; index < expectedReadings.Length; index++)
        {
            double actual = sensor.ReadSnowDepthCm(csvPath);
            double expected = expectedReadings[index];

            if (actual != expected)
            {
                throw new Exception(FormattableString.Invariant(
                    $"{name}, poll {index + 1}: Expected {expected} cm, got {actual} cm."));
            }
        }

        Console.WriteLine($"PASS: {name}");
    }
    finally
    {
        File.Delete(csvPath);
    }
}

static string WriteCsv(string[] rows)
{
    string csvPath = Path.GetTempFileName();
    File.WriteAllLines(csvPath, ["snow_depth_cm", .. rows]);
    return csvPath;
}
