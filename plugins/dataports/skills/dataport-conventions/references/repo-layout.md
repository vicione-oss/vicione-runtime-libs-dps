# Repository layout and scaffolding

## Folder map of a provider

Canonical shape, close to dp-modbus `src/Modbus.Client/` and dp-s7-absolute
`src/Siemens.S7.Absolute/` (which names its branch folder `Model/Nodes/Areas/`):

```
src/<Provider>/
├── <Provider>.yaml                 # ruleset (CopyToOutputDirectory: Always)
├── metadata.json                   # Name + __VERSION__ + __MIN_CORE_VERSION__ placeholders
├── <X>ConnectionTester.cs          # IDataPortConnectionTester<TCommunication>
├── Client/
│   ├── I<X>Client.cs               # IReadClient / IWriteClient (+ an …Disposable variant for owned clients)
│   ├── <X>Client.cs
│   ├── <X>ClientLogs.cs            # [LoggerMessage] companion
│   └── Pool/
│       ├── I<X>ClientFactory.cs
│       ├── <X>ClientFactory.cs
│       ├── <X>ClientPool.cs        # IClientLifecycleManager
│       └── <X>ClientPoolLogs.cs
├── Incoming/<X>DataPortIncoming.cs
├── Outgoing/<X>DataPortOutgoing.cs
└── Model/
    ├── DataPort/                   # runtime model
    │   ├── DataPoints/             # I<X>DataPoint, values, groups, codecs
    │   └── Device/                 # <X>ClientInformation and its value objects
    ├── Mapping/                    # <X>DataPointsMapper, <X>DataPointGroupsMapper
    └── Nodes/                      # configuration-time typed tree
        ├── Containers/<Area|Folder>/{,Mapping/}
        ├── DataPoints/<Type>/{,Mapping/}     # node + mapper + validator per type
        ├── Device/{,Mapping/}                # communication, root node, root mapper, validator
        └── Mapper/<X>NodeMapper.cs           # composition root
```

Tests mirror it under `tests/<Provider>.Tests/` (see [testing.md](testing.md)).

Code two providers in one repo share goes in a third project. dp-modbus moves the runtime data
points, codecs, block planning and the data-point mappers into `src/Modbus.Core/`
(`ViciOne.Suite.DataPort.Modbus.Core`), so `src/Modbus.Client/Model/DataPort/` keeps only
`Device/`. cip shares its pool and tag access through `src/AllenBradley.Client.Shared/`.

## Assembly and namespace naming

```xml
<RootNamespace>ViciOne.Suite.DataPort.Modbus</RootNamespace>
<AssemblyName>ViciOne.Suite.DataPort.Modbus.Client</AssemblyName>
```

Root namespace is the protocol; the assembly name adds the provider role. dp-s7-absolute uses
`ViciOne.Suite.DataPort.Siemens.S7.Absolute`.

## `src` project file

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>ViciOne.Suite.DataPort.Modbus</RootNamespace>
    <AssemblyName>ViciOne.Suite.DataPort.Modbus.Client</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <None Update="ModbusClient.yaml"><CopyToOutputDirectory>Always</CopyToOutputDirectory></None>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="ViciOne.Suite.DataPort" />
    <PackageReference Include="ViciOne.Suite.DataPort.Extensions" />
    <PackageReference Include="ViciOne.Modbus.Wrapper" />   <!-- the protocol library -->
    <PackageReference Include="FluentValidation" />
  </ItemGroup>
</Project>
```

No `<Version>` anywhere — `Directory.Build.props` derives it from the `VERSION` file, and
`Directory.Packages.props` owns package versions.

## `Directory.Packages.props`

Pin the three DataPort packages to one shared property so they never drift apart:

```xml
<DataPortVersion>1.0.0</DataPortVersion>
…
<PackageVersion Include="ViciOne.Suite.DataPort" Version="$(DataPortVersion)" />
<PackageVersion Include="ViciOne.Suite.DataPort.Extensions" Version="$(DataPortVersion)" />
<PackageVersion Include="ViciOne.Suite.DataPort.Extensions.Testing" Version="$(DataPortVersion)" />
```

Test-side set: `xunit.v3.mtp-v2`, `AwesomeAssertions` (+ `.Analyzers`), `NSubstitute`,
`Microsoft.Extensions.TimeProvider.Testing`, `Microsoft.Extensions.Diagnostics.Testing`,
`Microsoft.Testing.Extensions.CodeCoverage`. `ViciOne.Tree.Builder` (3.0.0, the ruleset parser)
arrives through `Extensions.Testing`; reference it directly only from a test that uses its types,
as dp-modbus's node-direction and dependent-properties tests do.

## New-repo checklist

Copy dp-modbus's root as the template:

- [ ] `.editorconfig`, `.gitignore`, `NuGet.Config` (feed sources, no credentials)
- [ ] `global.json` selecting the MTP test runner ([testing.md](testing.md))
- [ ] `Directory.Build.props` — version derivation, `net10.0`/`LangVersion 14`, warnings-as-errors,
      lock files, `InternalsVisibleTo`, MTP settings for `tests/**`
- [ ] `Directory.Packages.props`
- [ ] `VERSION` (provider) and `VERSION_CORE` (min engine core)
- [ ] `<repo>.slnx` with `/Solution Items/`, `/src/`, `/tests/` folders
- [ ] `AGENTS.md` + a `CLAUDE.md` that just points at it — record the provider's *decisions*
      (fatal vs non-fatal connect, direction semantics, pool identity), not restatements of this skill
- [ ] `.claude/settings.json` subscribing to the `vicione-dps` marketplace (snippet in the
      `dataports` plugin README)
- [ ] `README.md`, `LICENSE.txt`, `CHANGELOG.md`
- [ ] `.gitlab-ci.yml` — the shared component jobs plus one publish block per provider

## CI

`.gitlab-ci.yml` is built from GitLab CI components under `vicione-oss/tooling/`. Shared by every
provider in the repo: `components/version/version-calc`, `components/module-packaging/runtime-identifier`,
`dotnet-component/dotnet-restore`, and `dotnet-component/dotnet-artifacts` (build + test + coverage,
with the repo's `test_filter`). Then one block per provider — abridged from dp-modbus, where
`exec_matrix` is linux x64/arm64 + win x64 and the `rules` are YAML anchors defined at the top of
the file:

```yaml
- component: $CI_SERVER_FQDN/vicione-oss/tooling/components/module-packaging/metadata@v0.2.0
  inputs:
    name: "Modbus.Client Metadata"
    project: "src/Modbus.Client/Modbus.Client.csproj"
    module_version: "${Version}"
    metadata_json: "src/Modbus.Client/metadata.json"
    metadata_dir: "metadata/Modbus.Client"
    min_core_version_file: "VERSION_CORE"
    min_core_version_property: "MinCoreVersion"
- component: $CI_SERVER_FQDN/vicione-oss/tooling/dotnet-component/dotnet-publish@v2.7.1
  inputs:
    name: "dotnet Publish Modbus.Client"
    project: "src/Modbus.Client/Modbus.Client.csproj"
    output_directory: "artifacts/Modbus.Client/${OS}-${ARCH}"
    execution_matrix: *exec_matrix
- component: $CI_SERVER_FQDN/vicione-oss/tooling/components/module-packaging/package-module@v0.2.0
  inputs:
    name: "Pack Modbus.Client binaries"
    publish_job: "dotnet Publish Modbus.Client"
    metadata_job: "Modbus.Client Metadata"
- component: $CI_SERVER_FQDN/vicione-oss/tooling/components/jfrog/release@v2.0.1
  inputs:
    name: "Push Modbus.Client binaries (dev)"
    repository: "vicione-suite-dev"
    upload_path: "fbs/ViciOne.Suite.DataPort.Modbus.Client"
- component: $CI_SERVER_FQDN/vicione-oss/tooling/components/jfrog/release@v2.0.1
  inputs:
    name: "Push Modbus.Client binaries (staging)"
    repository: "vicione-suite-staging"
    upload_path: "fbs/ViciOne.Suite.DataPort.Modbus.Client"
- component: $CI_SERVER_FQDN/vicione-oss/tooling/components/jfrog/promote@v2.0.1
  inputs:
    name: "Release Modbus.Client binaries"
    source_repository: "vicione-suite-staging"
    destination_repository: "vicione-suite"
    remote_path: "fbs/ViciOne.Suite.DataPort.Modbus.Client"
```

Copy the block from the repo itself rather than from here — it carries the `stage`, `rules`,
`needs`, `artifacts` and `before_script` inputs left out above, and the component versions move.
Which pipeline uploads where is in [conventions.md](conventions.md), "Packaging and versioning".

**The publish jobs restore a second time, on purpose.** `dotnet-restore` runs without an
`execution_matrix`, so it caches the packages under a key without OS and ARCH, which the matrix jobs
of `dotnet Publish` never find; each of them runs `dotnet restore` in its `before_script` instead.
Giving `dotnet-restore` the matrix, as the dotnet-component README recommends, would also need an
entry without OS and ARCH for `dotnet-artifacts` — four restore jobs in every pipeline, to save time
in a job that runs on tags and by hand only. Keep the restore in the publish block. (Decided for the
dotnet-component 2.7.2 rollout, 2026-10-14.)

## Neighbouring repos worth knowing

| Repo | What it is |
|---|---|
| `dps` | the DataPort framework itself — `src/DataPort` (core), `src/DataPort.Extensions` (base classes), `src/DataPort.Extensions.Testing`, `src/FunctionBlock` (the engine function block that hosts a DataPort), the how-to docs, and this plugin |
| `tree-builder` | `ViciOne.Tree.Builder` — parses and validates the YAML ruleset; its repo-local `writing-ruleset-yaml` skill documents the format |
| `cip`, `dp-modbus`, `dp-s7-absolute`, `s7` | the providers built on the base classes |
| `managed-engine`, `engine-host` | the runtime that loads the published `fbs/` modules |
