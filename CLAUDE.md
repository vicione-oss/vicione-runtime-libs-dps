# DataPorts

ViciOne Suite runtime library providing DataPort providers for various protocols (AWS IoT, Azure IoT Hub, MQTT, OPC-UA, SQL databases, InfluxDB, etc.).

**Solution file**: `dps.slnx`

## Build & test

```bash
dotnet build dps.slnx                                         # full solution
dotnet test dps.slnx                                          # all tests
dotnet test dps.slnx --settings tests.runsettings            # with coverage + JUnit XML output (artifacts/tests/)
dotnet build src/DataPort.Extensions/DataPort.Extensions.csproj  # single project
dotnet test tests/DataPort.Extensions.Tests/                  # single test project
```

## NuGet

Packages are served from a private JFrog Artifactory instance. Credentials must be configured
locally (e.g., `dotnet nuget update source`) — `NuGet.Config` placeholders (`##NuGet_User_JFrog##`
etc.) are replaced by CI; configure manually for local development.

**Central package versions**: `Directory.Packages.props` manages all NuGet versions centrally
(`ManagePackageVersionsCentrally=true`). When adding a package, add its version entry there —
not in the `.csproj`.

**Assertion library**: Tests use `AwesomeAssertions` (FluentAssertions fork). Do not add
`FluentAssertions` — it conflicts and was replaced due to a licensing change.

## Structure

- `src/DataPort/` — core DataPort framework (no extensions)
- `src/DataPort.Extensions/` — reusable base classes for building providers (see its CLAUDE.md)
- `src/DataPort.Extensions.Testing/` — test helpers for extension-based providers
- `src/{Protocol}/` — protocol-specific providers (Mqtt, OpcUa.Client, InfluxDb, etc.)
- `tests/` — test projects mirroring src layout
- `samples/` — benchmark and example projects
