using System.Globalization;
using Watershield.Core.Contracts;

namespace Watershield.Devices.Adilbhai;

public sealed class SaltSpreaderController : IActuator
{
    private readonly string csvPath;
    private readonly TimeProvider clock;
    private LoopedCsvDataSource? source;
    private int? commandedRate;

    public SaltSpreaderController(
        string deviceId,
        string segmentId,
        string csvPath,
        TimeProvider? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(segmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);

        DeviceId = deviceId;
        SegmentId = segmentId;
        this.csvPath = csvPath;
        this.clock = clock ?? TimeProvider.System;
    }

    public string DeviceId { get; }
    public string SegmentId { get; }
    public string Name => "Salt spreader controller";
    public DeviceStatus Status { get; private set; } = DeviceStatus.Offline;
    public int RatePercent { get; private set; }

    public event Action<DeviceEvent>? EventRaised;

    public void Initialize()
    {
        source = null;
        commandedRate = null;
        RatePercent = 0;

        try
        {
            source = new LoopedCsvDataSource(csvPath, "salt_rate_percent");
            Status = DeviceStatus.Online;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Status = DeviceStatus.Fault;
            ReportEvent($"Cannot load salt spreader data: {ex.Message}");
        }
    }

    public TelemetryPacket ReadTelemetry()
    {
        if (source is null)
        {
            throw new InvalidOperationException(
                "Initialize a valid simulation file before polling.");
        }

        for (int row = 0; row < source.Count; row++)
        {
            string[] fields = source.Next();

            if (fields.Length == 1 && TryRate(fields[0], out int baselineRate))
            {
                RatePercent = commandedRate ?? baselineRate;
                Status = DeviceStatus.Online;

                return CreatePacket(
                    new Dictionary<string, TelemetryValue>
                    {
                        ["salt_rate"] = new(RatePercent, "%")
                    },
                    TelemetryQuality.Ok);
            }

            ReportEvent("Skipped an invalid salt-rate record.");
        }

        Status = DeviceStatus.Fault;
        return CreatePacket(
            new Dictionary<string, TelemetryValue>(),
            TelemetryQuality.Fault);
    }

    public CommandResult Execute(ControlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        string? rejection = ValidateCommand(command);

        if (rejection is not null)
        {
            return Reject(command, rejection);
        }

        if (!TryRate(command.Parameter, out int rate))
        {
            return Reject(command, "Use a rate from 0 to 100 in steps of 5.");
        }

        RatePercent = rate;
        commandedRate = rate;
        ReportEvent($"Salt rate set to {rate}%.");

        return new CommandResult(
            command.CommandId,
            CommandOutcome.Success,
            RatePercent.ToString(CultureInfo.InvariantCulture),
            "Simulated salt rate updated.");
    }

    public void ResetScenario()
    {
        if (source is null)
        {
            throw new InvalidOperationException("Initialize before resetting the scenario.");
        }

        source.Reset();
        commandedRate = null;
        RatePercent = 0;
        Status = DeviceStatus.Online;
    }

    public void Shutdown()
    {
        source = null;
        commandedRate = null;
        Status = DeviceStatus.Offline;
    }

    private string? ValidateCommand(ControlCommand command)
    {
        if (command.TargetDeviceId != DeviceId)
            return "Command targets another device.";

        if (Status != DeviceStatus.Online)
            return "Device is not online.";

        if (command.CommandId == Guid.Empty)
            return "A command ID is required.";

        if (string.IsNullOrWhiteSpace(command.IssuedBy))
            return "An issuing user is required.";

        if (command.Action != "SetSaltRate")
            return "Supported action is SetSaltRate.";

        return null;
    }

    private CommandResult Reject(ControlCommand command, string reason)
    {
        ReportEvent($"Rejected salt command: {reason}");

        return new CommandResult(
            command.CommandId,
            CommandOutcome.Rejected,
            RatePercent.ToString(CultureInfo.InvariantCulture),
            reason);
    }

    private static bool TryRate(string? value, out int rate)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out rate)
            && rate is >= 0 and <= 100
            && rate % 5 == 0;
    }

    private TelemetryPacket CreatePacket(
        IReadOnlyDictionary<string, TelemetryValue> values,
        TelemetryQuality quality)
    {
        return new TelemetryPacket(
            DeviceId,
            nameof(SaltSpreaderController),
            SegmentId,
            clock.GetUtcNow(),
            values,
            quality);
    }

    private void ReportEvent(string message)
    {
        EventRaised?.Invoke(new DeviceEvent(DeviceId, clock.GetUtcNow(), message));
    }
}
