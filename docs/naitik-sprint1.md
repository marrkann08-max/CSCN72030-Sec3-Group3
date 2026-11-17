# Naitik Sprint 1 handoff (T28-T30 and T14)

## Scope and design
Implements User Stories 10 (dispatch state) and 11 (warning sign device), with the T14 starter integration harness. GUI, ControlManager authorization/confirmation, vehicle movement, SQLite logging, login and History are later integration work, not implemented here. The existing WPF window remains a shell.

Main only contained project shells at the start (a850851). Keshav's remote branch had snow-depth CSV looping but no shared interfaces. This branch therefore adds the shared contracts and LoopedCsvDataSource anticipated by the supplied teammate handoffs without changing any teammate's device files. Review those shared contracts together before merging; do not add duplicate definitions from other branches.

CsvController uses a template method for common lifecycle, fault handling, command validation and persistent state. Concrete controllers supply parsing, transitions and telemetry. TimeProvider is injectable. Call from the core polling thread serially; these objects do not promise concurrent command/poll safety.

## Device API
- VariableWarningSignController(deviceId, segmentId, csvPath, presetMessages, clock?): SetMessage with a configured preset activates the sign; Deactivate clears it. Telemetry: sign_active (bool), sign_message (string).
- DispatchController(vehicleId, csvPath, knownSegments, clock?): Dispatch with a known segment requires AVAILABLE; Recall requires DISPATCHED and enters RETURNING. An AVAILABLE feedback row completes return at the depot. The resulting AVAILABLE state stays pinned until the next accepted command or ResetScenario, preventing replay from resurrecting an old dispatch. This implements the return-completion exception from the supplied handoff and US10.
- Both: Initialize, ReadTelemetry, Execute, ResetScenario, Shutdown. Initialize then poll before commands. Reset clears overrides and rewinds the file; poll again before commands.
- Command result always carries the original command ID, outcome, new state and a reason. Wrong target, empty command ID, missing issuer, invalid values, unsupported transitions and offline/unpolled devices are rejected.
- Device events report bad rows and command outcomes. Future core/logger subscribes to EventRaised; this is not yet SQLite persistence.
- Role authorization and confirmation belong in ControlManager before Execute. IssuedBy is an audit identity, not proof of authorization.

## CSV contract and scenarios
UTF-8, comma-delimited, unquoted fields, exact header, at least one record. Quoted fields/embedded commas are not supported by this minimal simulation reader. Invalid device rows are logged and skipped; an all-invalid cycle produces Fault. Missing/header-only/wrong-header files fault at initialization.

Each of light-snow, heavy-snow and freezing-rain has 50 changing data rows per device (300 total).
- warning-sign.csv: active,message; presets ICY ROAD AHEAD and SLOW DOWN.
- dispatch.csv: vehicle_status,destination_segment; segments R-01 and R-02.
- Dispatch is the authority for commanded availability/destination; Prisha's telemetry module supplies position and speed. The core still needs to reconcile the two sources when integrated. Sample segment IDs and presets remain configurable constructor inputs.

## Run in Visual Studio 2022
Open WATERSHIELD.sln with .NET 8 and the .NET desktop development workload. Use Test > Test Explorer > Run All Tests. Watershield.NaitikTests is a separate xUnit project, so it cannot conflict with Keshav's console test entry point.

Command-line equivalent:
```powershell
dotnet build WATERSHIELD.sln --configuration Release --warnaserror
dotnet test tests/Watershield.NaitikTests --configuration Release --no-build --collect:"XPlat Code Coverage"
```
CI builds the solution and runs these tests on every push/pull request.

## TDD evidence (actual executed stages)
| Stage | Red commit | Red result | Green commit | Green result |
| --- | --- | --- | --- | --- |
| Sign | 1066a9b | 6 failed, 0 passed | feddad9 | 6 passed |
| Dispatch | f1b45c0 | 10 failed, 6 passed | 04e6f0d | 16 passed |
| Faults/configuration | d374a52 | 2 failed, 31 passed | 6370f65 | 33 passed |
| Integration/data | dad1678 | 4 failed, 33 passed | d424da7 | 37 passed |

The first controller stages used compiling stubs that threw NotImplementedException. Robustness tests exposed mutable CSV records and blank configuration values before fixes. The supplied draft informed implementation after the red runs. Local raw TRX files are under artifacts/tdd; final release results and coverage are under artifacts/verification (ignored build artifacts). Preserve the red/green commits if the course requires Git history evidence; squashing removes that sequence.

Release verification: 37 passing tests; zero build warnings/errors. Coverage after the final refactor: 162/164 production lines (98.78%) across the new Core contracts and Devices assembly, not the whole planned application. Final refactor separates the common controller and warning sign into named files; it does not change behavior.

## T14 approach and requirement mapping
The repeatable harness registers a deterministic test sensor as IDevice/ISensor and the real sign as IActuator. Expected sequence: sensor -5 deg C and inactive sign; issue SetMessage; Success; next telemetry ICY ROAD AHEAD; shutdown Offline. This is a starter contract integration check, not a claim of full SystemManager/GUI/SQLite integration. Replace the test-only sensor with team modules and connect the harness to Muhammad's polling loop when available.

| Requirement | Evidence |
| --- | --- |
| FUN-004, INT-004, SYS-010 | baseline wrap, command persistence, reset and all six dataset checks |
| FUN-009 | bad rows logged/skipped; bounded all-invalid cycle |
| FUN-023 / US11 | activation, deactivation, preset validation |
| FUN-024 / US10 | dispatch/recall, unavailable states, depot feedback |
| FUN-026, FUN-027 (device portion) | metadata/value/status validation, result and unchanged state after rejection |
| INT-001 to INT-003 | shared contracts, identity/UTC telemetry, harness |
| NFR-010, NFR-012 / US21 | xUnit, measured coverage, CI, red/green history |

Remaining integration acceptance: user-role authorization and confirmation, public warning display, map movement, logger subscription, real sensor/polling-loop connection and full-system tests. These require the later teammate/core/UI modules.
