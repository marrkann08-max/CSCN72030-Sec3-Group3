namespace Watershield.Devices.Keshav;

public enum BladePosition
{
    Up,
    Down
}

public sealed class SnowplowBladeController
{
    public BladePosition Position { get; private set; } = BladePosition.Up;

    public bool TrySetPosition(string? command)
    {
        switch (command)
        {
            case "UP":
                Position = BladePosition.Up;
                return true;
            case "DOWN":
                Position = BladePosition.Down;
                return true;
            default:
                return false;
        }
    }
}
