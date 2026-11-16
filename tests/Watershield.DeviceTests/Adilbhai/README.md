# Adilbhai

Automated tests use MSTest, the framework configured for `Watershield.DeviceTests`.

- `RoadSensorTests.cs` covers parsing, range boundaries, invalid rows, normalization, and replay.
- `SaltSpreaderControllerTests.cs` covers valid and rejected commands, persistence, reset, and fault handling.
- `ScenarioReplayTests.cs` verifies all three scenario files contain 50 valid rows per device.
