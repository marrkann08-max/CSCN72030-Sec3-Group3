namespace Watershield.Devices;

/// <summary>Loops through the project's simple, unquoted simulation CSV records.</summary>
public sealed class LoopedCsvDataSource
{
    private readonly string[][] rows;
    private int index;
    public LoopedCsvDataSource(string path, params string[] columns)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2 || !lines[0].Split(',').SequenceEqual(columns))
            throw new InvalidDataException("Expected CSV header and at least one data row.");
        rows = lines.Skip(1).Select(line => line.Split(',')).ToArray();
    }
    public int Count => rows.Length;
    public string[] Next()
    {
        var row = rows[index];
        index = (index + 1) % rows.Length;
        return (string[])row.Clone();
    }
    public void Reset() => index = 0;
}
