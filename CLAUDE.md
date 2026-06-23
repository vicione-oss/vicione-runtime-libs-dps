# DataPorts

ViciOne Suite runtime library providing the core DataPort framework and the reusable base classes for
building DataPort implementations. The concrete protocol-specific implementations (MQTT, OPC-UA, SQL, …)
live in their own repositories — this repo is the shared foundation they build on.

**Solution file**: `dps.slnx`

## Build & test

```bash
dotnet build dps.slnx                                         # full solution
dotnet test dps.slnx                                          # all tests
dotnet test dps.slnx --settings tests.runsettings            # with coverage + JUnit XML output (artifacts/tests/)
dotnet build src/DataPort.Extensions/DataPort.Extensions.csproj  # single project
dotnet test tests/DataPort.Extensions.Tests/                  # single test project
```

## Documentation

The whole `docs/` tree builds into **one combined DocFX site** with a single command — this is what
GitLab Pages publishes. CI builds it **once** in the `DocFX` job (runs automatically, doubling as a
docs-build check) and then deploys that artifact via GitLab Pages parallel deployments (the
`.dotnet-docfx-*` job templates, currently defined locally at the bottom of `.gitlab-ci.yml` pending
upstreaming to the shared `CI-Templates/v2/dotnet.yml`): the **default branch (main)** auto-publishes
the current docs to the Pages **root**, so the default/current docs always track main; **tagged
releases** publish **permanently** to a per-version subpath (`/<version>/`, never expires) so every
released version stays browsable at its own URL (releases are *not* mirrored to root); and MR /
prerelease pipelines offer a short-lived, manual preview under a `<VERSION>-ci<CI_PIPELINE_ID>`
subfolder. The areas mirror `src/` and become the
top-level tabs (`docs/toc.yml`); `docs/index.md` is the landing page. The DataPort.Extensions API
reference is **generated from the source XML doc comments** as part of the same build.

```bash
dotnet tool restore            # installs DocFX 2.78.5 (pinned in .config/dotnet-tools.json)
dotnet docfx docs/docfx.json   # build the FULL site (all areas + API) into docs/_site
dotnet docfx docs/docfx.json --serve   # same, then preview at http://localhost:8080
```

Each area **also** has its own `docfx.json` (`docs/DataPort/`, `docs/DataPort.Extensions/`,
`docs/DataPort.Extensions.Testing/`) for building/serving that area **standalone** — useful for quick
iteration, and for publishing DataPort.Extensions on its own (where it keeps its Home / Explanation /
How-to / Reference / API **top tabs**, since it then owns the navbar). The combined build instead shows
the areas as the top tabs and each area's sections in the left sidebar.

| Build                  | Config                                  | Output                                  |
|------------------------|-----------------------------------------|-----------------------------------------|
| Full site (CI / Pages) | `docs/docfx.json`                       | `docs/_site/` — everything, one site    |
| DataPort standalone    | `docs/DataPort/docfx.json`              | `docs/DataPort/_site/`                   |
| Extensions standalone  | `docs/DataPort.Extensions/docfx.json`   | `docs/DataPort.Extensions/_site/`        |
| Testing standalone     | `docs/DataPort.Extensions.Testing/docfx.json` | `docs/DataPort.Extensions.Testing/_site/` |

Generated output is **git-ignored and rebuilt on demand** — never commit it: every `_site/`, and for
DataPort.Extensions everything in `reference/api/` *except* the hand-written `reference/api/index.md`.
The curated `reference/*.md` pages are design-notes that link into the generated API via `xref:` UIDs;
keep signatures/members in the XML doc comments (the generator owns them), not duplicated in markdown.

In the **combined** build every area shares one root, so cross-area links resolve and it builds clean.
The **standalone** area builds are scoped to one folder, so their cross-area links (e.g.
DataPort.Extensions → `docs/DataPort/`) produce expected `InvalidFileLink` warnings — **accepted, not
bugs**; don't "fix" them by disabling link validation. See the README's
[*Known build warnings: cross-area links*](docs/DataPort.Extensions/README.md#known-build-warnings-cross-area-links).

### Docs in the solution: per-area `*.Docs.csproj` projects

The `docs/` tree is surfaced in `dps.slnx` by **`Microsoft.Build.NoTargets`** projects instead of
hand-listing every file as solution-folder items — `.slnx` solution folders have no glob support, so
that manual list drifted constantly. NoTargets compiles nothing and emits no assembly (Rider/VS still
render the project's on-disk tree), so `dotnet build` / `dotnet test` / CI treat it as a fast no-op.

The docs projects **mirror the `src/` layout** — each code area with docs has its own per-area docs
project living next to those docs:

| Docs project                                                       | Surfaces                          | Mirrors                          |
|--------------------------------------------------------------------|-----------------------------------|----------------------------------|
| `docs/DataPort/DataPort.Docs.csproj`                               | `docs/DataPort/`                  | `src/DataPort/`                  |
| `docs/DataPort.Extensions/DataPort.Extensions.Docs.csproj`         | `docs/DataPort.Extensions/`       | `src/DataPort.Extensions/`       |
| `docs/DataPort.Extensions.Testing/DataPort.Extensions.Testing.Docs.csproj` | `docs/DataPort.Extensions.Testing/` | `src/DataPort.Extensions.Testing/` |
| `docs/Docs.csproj`                                                 | `docs/diagram-creation/` (leftover) | — (shared diagram tooling)     |

- Each per-area project sits *inside* its docs folder and globs it with `<None Include="**" />`,
  excluding `bin/**;obj/**;*.csproj`. `DataPort.Extensions` additionally excludes the DocFX-generated,
  git-ignored output (`_site/`, and `reference/api/` except the hand-written `index.md`).
- `docs/Docs.csproj` is the **leftover container** for areas without their own project — currently
  just `diagram-creation/`. NoTargets (rather than `Microsoft.NET.Sdk`) is what keeps the stray
  `docs/diagram-creation/fix-excalidraw-fonts.cs` from being compiled.
- The `DataPort.Extensions` markdown is *also* packed into the NuGet package by
  `src/DataPort.Extensions/DataPort.Extensions.csproj` (with `Visible="false"`). That packaging
  concern is separate from the docs project's IDE-visibility concern — the same files legitimately
  belong to both projects.
- `docs/Directory.Build.props` is **intentionally empty** — it stops inheritance of the repo-root
  `Directory.Build.props` so docs projects get none of the code conventions (TFM,
  `TreatWarningsAsErrors`, lock files, packaging) that don't apply to docs-only projects.
- **Adding a new top-level docs folder**: give it its own per-area `*.Docs.csproj` (preferred, if it
  mirrors a `src/` area) or add a `<None Include="<folder>/**" />` glob to `docs/Docs.csproj`, then
  register the project in `dps.slnx` under the `/docs/` folder.

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
- `src/DataPort.Extensions/` — reusable base classes for building DataPort implementations (see its CLAUDE.md)
- `src/DataPort.Extensions.Testing/` — test helpers for extension-based DataPort implementations
- `src/FunctionBlock/` — the managed-engine DataPort function block (`ViciOne.Suite.System.DataPort`)
- `tests/` — test projects mirroring src layout
- `samples/` — benchmark and example projects

The concrete protocol-specific implementations (MQTT, OPC-UA, SQL, …) live in their own repositories.
