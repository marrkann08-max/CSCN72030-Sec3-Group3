using System.Globalization;

namespace Watershield.Devices.Keshav;

public sealed class SnowDepthSensor
{
    private string? loadedPath;
    private string[] readings = [];
    private int nextReadingIndex;

    public double ReadSnowDepthCm(string csvPath)
    {
        string fullPath = Path.GetFullPath(csvPath);

        if (loadedPath != fullPath)
        {
            string[] dataRows = File.ReadLines(fullPath).Skip(1).ToArray();

            if (dataRows.Length == 0)
            {
                throw new InvalidDataException("The snow-depth CSV contains no data rows.");
            }

            readings = dataRows;
            loadedPath = fullPath;
            nextReadingIndex = 0;
        }

        double depth = double.Parse(readings[nextReadingIndex], CultureInfo.InvariantCulture);
        nextReadingIndex = (nextReadingIndex + 1) % readings.Length;
        return depth;
    }
}
