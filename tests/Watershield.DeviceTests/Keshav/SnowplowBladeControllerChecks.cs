using Watershield.Devices.Keshav;

internal static class SnowplowBladeControllerChecks
{
    internal static void Run()
    {
        var controller = new SnowplowBladeController();

        if (controller.Position != BladePosition.Up)
        {
            throw new InvalidOperationException("Expected the blade to start UP.");
        }

        if (!controller.TrySetPosition("DOWN") || controller.Position != BladePosition.Down)
        {
            throw new InvalidOperationException("Expected DOWN to lower the blade.");
        }

        if (controller.TrySetPosition("SIDEWAYS") || controller.Position != BladePosition.Down)
        {
            throw new InvalidOperationException("Expected an invalid command to leave the blade DOWN.");
        }

        if (!controller.TrySetPosition("UP") || controller.Position != BladePosition.Up)
        {
            throw new InvalidOperationException("Expected UP to raise the blade.");
        }

        Console.WriteLine("PASS: Lower and raise the blade, preserving its position after rejection");
    }
}
