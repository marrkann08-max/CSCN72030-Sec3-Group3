# Muhammad - temperature sensors and weather station

All three devices extend `CsvWeatherSensor` (in `MuhammadSensors.cs`) and read a looped CSV from `data/scenarios/<scenario>/`.
Scenarios: `light-snow`, `heavy-snow`, `freezing-rain`. Each file has exactly 60 data rows.

| Device | CSV file | Header | Telemetry keys (unit) |
|---|---|---|---|
| `RoadTemperatureSensor` | `road-temperature.csv` | `road_temperature_c` | `road_temperature` (°C) |
| `SurfaceTemperatureSensor` | `surface-temperature.csv` | `surface_temperature_c` | `surface_temperature` (°C) |
| `WeatherStation` | `weather-station.csv` | `air_temperature_c,wind_speed_kmh,precipitation` | `air_temperature` (°C), `wind_speed` (km/h), `precipitation` (text, no unit) |

**Allowed precipitation values:** `none`, `rain`, `snow`, `freezing rain` (case doesn't matter, reported in lower case).

**Numbers** use a dot for decimals (invariant culture). Negative temperatures are fine; NaN/Infinity are not, and wind speed can't be negative.

**Bad data:**
- An invalid row is skipped and the device raises an `EventRaised` event, then moves on to the next row.
- If a whole pass over the file finds no valid row, the device goes to `DeviceStatus.Fault` and returns a packet with `TelemetryQuality.Fault` (the last good reading, or empty values if there never was one).
- A missing file or wrong header puts the device in `Fault` on `Initialize()` instead of throwing.

Tests are in `tests/Watershield.MuhammadTests` (run with `dotnet test tests/Watershield.MuhammadTests`).
