namespace Watershield.Devices.Keshav;

public enum BladePosition
{
    Up,
    Down
}

public sealed class SnowplowBladeController
{
    private BladePosition[] scenarioReadings = [];
    private int nextReadingIndex;
    private BladePosition? commandedPosition;

    public BladePosition Position { get; private set; } = BladePosition.Up;

    public void LoadScenario(string csvPath)
    {
        using var rows = File.ReadLines(csvPath).GetEnumerator();

        if (!rows.MoveNext() || rows.Current.Trim() != "blade_state")
        {
            throw new InvalidDataException("Expected a blade_state CSV header.");
        }

        var validReadings = new List<BladePosition>();

        while (rows.MoveNext())
        {
            if (ParsePosition(rows.Current.Trim()) is { } position)
            {
                validReadings.Add(position);
            }
        }

        if (validReadings.Count == 0)
        {
            throw new InvalidDataException("The blade CSV contains no usable readings.");
        }

        scenarioReadings = validReadings.ToArray();
        ResetScenario();
    }

    public BladePosition PollFeedback()
    {
        EnsureScenarioLoaded();

        Position = commandedPosition ?? scenarioReadings[nextReadingIndex];
        nextReadingIndex = (nextReadingIndex + 1) % scenarioReadings.Length;
        return Position;
    }

    public void ResetScenario()
    {
        EnsureScenarioLoaded();

        nextReadingIndex = 0;
        commandedPosition = null;
        Position = scenarioReadings[0];
    }

    public bool TrySetPosition(string? command)
    {
        if (ParsePosition(command) is not { } position)
        {
            return false;
        }

        Position = position;
        commandedPosition = position;
        return true;
    }

    private void EnsureScenarioLoaded()
    {
        if (scenarioReadings.Length == 0)
        {
            throw new InvalidOperationException("Load a blade scenario before polling or resetting it.");
        }
    }

    private static BladePosition? ParsePosition(string? value) => value switch
    {
        "UP" => BladePosition.Up,
        "DOWN" => BladePosition.Down,
        _ => null
    };
}
