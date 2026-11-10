using System.Globalization;

namespace Watershield.Devices.Keshav;

public sealed class SnowDepthSensor
{
    public double ReadSnowDepthCm(string csvPath)
    {
        string firstReading = File.ReadLines(csvPath).Skip(1).First();
        return double.Parse(firstReading, CultureInfo.InvariantCulture);
    }
}
