using Watershield.Devices.Keshav;

string csvPath = Path.GetTempFileName();

try
{
    File.WriteAllLines(csvPath, ["snow_depth_cm", "12.4", "8.1"]);

    var sensor = new SnowDepthSensor();
    double[] expectedReadings = [12.4, 8.1, 12.4];

    for (int index = 0; index < expectedReadings.Length; index++)
    {
        double actual = sensor.ReadSnowDepthCm(csvPath);
        double expected = expectedReadings[index];

        if (actual != expected)
        {
            Console.Error.WriteLine(FormattableString.Invariant(
                $"FAIL on poll {index + 1}: Expected {expected} cm, got {actual} cm."));
            return 1;
        }
    }

    Console.WriteLine("PASS: Read both data rows and looped back to the first.");
    return 0;
}
finally
{
    File.Delete(csvPath);
}
