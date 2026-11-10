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
        CheckScenarioFeedback();
    }

    private static void CheckScenarioFeedback()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath, ["blade_state", "DOWN", "UP"]);
            var controller = new SnowplowBladeController();
            controller.LoadScenario(csvPath);

            ExpectFeedback(controller, BladePosition.Down, "first CSV row");
            ExpectFeedback(controller, BladePosition.Up, "second CSV row");
            ExpectFeedback(controller, BladePosition.Down, "CSV wrap");

            controller.TrySetPosition("UP");
            ExpectFeedback(controller, BladePosition.Up, "first poll after command");
            ExpectFeedback(controller, BladePosition.Up, "command survives a CSV wrap");

            controller.ResetScenario();
            ExpectFeedback(controller, BladePosition.Down, "reset restores first CSV row");
            ExpectFeedback(controller, BladePosition.Up, "CSV resumes after reset");
            Console.WriteLine("PASS: Blade feedback loops, preserves commands, and resets");
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    private static void ExpectFeedback(
        SnowplowBladeController controller, BladePosition expected, string step)
    {
        BladePosition feedback = controller.PollFeedback();

        if (feedback != expected || controller.Position != expected)
        {
            throw new InvalidOperationException(
                $"Blade {step}: expected {expected}, got feedback {feedback} and position {controller.Position}.");
        }
    }
}
