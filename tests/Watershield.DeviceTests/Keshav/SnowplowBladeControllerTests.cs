using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watershield.Devices.Keshav;

namespace Watershield.DeviceTests.Keshav;

[TestClass]
public sealed class SnowplowBladeControllerTests
{
    [TestMethod]
    public void HandlesBladeCommandsAndPreservesPositionAfterRejection()
    {
        var controller = new SnowplowBladeController();

        Assert.AreEqual(BladePosition.Up, controller.Position, "The blade should start up.");

        Assert.IsTrue(controller.TrySetPosition("DOWN"));
        Assert.AreEqual(BladePosition.Down, controller.Position);

        Assert.IsFalse(controller.TrySetPosition("SIDEWAYS"));
        Assert.AreEqual(BladePosition.Down, controller.Position);

        Assert.IsTrue(controller.TrySetPosition("UP"));
        Assert.AreEqual(BladePosition.Up, controller.Position);
    }

    [TestMethod]
    public void LoopsFeedbackPreservesCommandsAndResetsScenario()
    {
        string csvPath = Path.GetTempFileName();

        try
        {
            File.WriteAllLines(csvPath, ["blade_state", "DOWN", "UP"]);
            var controller = new SnowplowBladeController();
            controller.LoadScenario(csvPath);

            AssertFeedback(controller, BladePosition.Down, "first CSV row");
            AssertFeedback(controller, BladePosition.Up, "second CSV row");
            AssertFeedback(controller, BladePosition.Down, "CSV wrap");

            Assert.IsTrue(controller.TrySetPosition("UP"));
            AssertFeedback(controller, BladePosition.Up, "first poll after command");
            AssertFeedback(controller, BladePosition.Up, "command survives a CSV wrap");

            controller.ResetScenario();
            AssertFeedback(controller, BladePosition.Down, "reset restores first CSV row");
            AssertFeedback(controller, BladePosition.Up, "CSV resumes after reset");
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    private static void AssertFeedback(
        SnowplowBladeController controller, BladePosition expected, string step)
    {
        Assert.AreEqual(expected, controller.PollFeedback(), $"Feedback at {step}.");
        Assert.AreEqual(expected, controller.Position, $"Position at {step}.");
    }
}
