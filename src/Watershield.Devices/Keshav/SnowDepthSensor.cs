using System.Globalization;

namespace Watershield.Devices.Keshav;

public sealed class SnowDepthSensor
{
    private string? loadedPath;
    private List<double> readings = [];
    private int nextReadingIndex;

    public double ReadSnowDepthCm(string csvPath)
    {
        string fullPath = Path.GetFullPath(csvPath);

        if (loadedPath != fullPath)
        {
            var validReadings = new List<double>();

            foreach (string row in File.ReadLines(fullPath).Skip(1))
            {
                if (double.TryParse(row, NumberStyles.Float, CultureInfo.InvariantCulture, out double depth)
                    && double.IsFinite(depth)
                    && depth >= 0)
                {
                    validReadings.Add(depth);
                }
            }

            if (validReadings.Count == 0)
            {
                throw new InvalidDataException("The snow-depth CSV contains no usable readings.");
            }

            readings = validReadings;
            loadedPath = fullPath;
            nextReadingIndex = 0;
        }

        double nextReading = readings[nextReadingIndex];
        nextReadingIndex = (nextReadingIndex + 1) % readings.Count;
        return nextReading;
    }
}
