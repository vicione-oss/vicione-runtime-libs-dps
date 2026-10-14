# Binding conventions

Sourced from the `AGENTS.md` of dp-modbus, cip and s7, and from dps
`src/DataPort.Extensions/AGENTS.md`. Where a repo's own `AGENTS.md`/`CLAUDE.md` disagrees with this
file, that repo wins. What may travel on the wire, how an unreadable value is handled and where
validation belongs are the contract in SKILL.md, not here.

## Language and build

The build gate, one type per file, UTF-8 without BOM, central package versions, logging instead of
throwing, and no LINQ per poll are "The rules that break builds" in SKILL.md. Beyond those:

- `net10.0`, `LangVersion 14`, `Nullable` + `ImplicitUsings` enabled.
- dp-modbus sets `GenerateDocumentationFile=true`, with `src` projects suppressing `CS1591` (public
  API not yet fully documented); new public types should carry XML docs either way.
- The repos store **LF**. Never hand-convert line endings.
- `ManagePackageVersionsCentrally=true`, and lock files on (`packages.lock.json`,
  `RestoreLockedMode` in CI).
- Test projects get `InternalsVisibleTo $(MSBuildProjectName).Tests` automatically from
  `Directory.Build.props` — internal types are testable, so prefer `internal` over `public` for
  anything not part of the engine-facing surface.

## Exceptions

The library's exception vocabulary: `ConnectionFailureException`, `DataConversionException`,
`DataRetrievalException`, `InvalidConfigurationException`, `MaxRetriesExceededException`,
`QueueBackpressureException`. Where one may be thrown is rule 2 in SKILL.md; conversion failures are
returned as data instead ([build-order.md](build-order.md), step 7).

## Connection lifecycle — decide these explicitly

Whether a failed start is fatal is a per-provider product decision. dp-modbus makes both choices,
for documented reasons:

- **Client, non-fatal**: `ConnectAsync` on the client is strict (throws — the connection tester
  needs that), but the *pool* catches it and returns a deferred client. A device that is not up at
  engine start reconnects on the next poll.
- **Server, fatal**: the pool rethrows, so a bind failure surfaces as the engine's red start error.

Whichever you pick, say so in the provider's AGENTS.md — it is the first thing anyone debugging a
silent no-data condition needs to know.

## Outgoing path semantics — do not "improve" these

- **Two all-or-nothing gates**: conversion, then range validation. If any value in a batch fails
  either, the whole batch is discarded. Unmatched channels are the one exception — logged and
  skipped, not batch-dropping. Changing this to partial validation needs a discussion, not a commit.
  (Incoming is the other way round: each received value is its own data point's business.)
- **Queue peek/mark**: the queue processor peeks the head, writes, and marks read only on success,
  so a retry needs no re-enqueue. Do not refactor to dequeue-first.
- `SendAsync` never blocks or throws on a full queue; it falls back to the `QueueStrategy`
  (`DropOldest` for freshness, `DropNewest` to preserve order) so a stuck write can't stall the
  engine cycle.

## Performance

Channel resolution is O(1) via `ChannelResolver`, built once at connect — never scan per send.
Batching at the protocol level is in [build-order.md](build-order.md), step 9.

## Thread safety

In [build-order.md](build-order.md): the client's `SemaphoreSlim` gate and the pool's connect gate
in step 8, the ports' lifecycle methods in step 10.

## Packaging and versioning

- `VERSION` — the provider version; `VERSION_CORE` — minimum engine core version. CI writes both
  into `metadata.json`'s `__VERSION__` / `__MIN_CORE_VERSION__`.
- Untagged pipelines produce `<VERSION>-ci<PIPELINE_ID>`; a tag `v<version>` produces the version
  without the `v`.
- CI publishes each provider for `linux-x64`, `linux-arm64` and `win-x64`, zips each, and uploads
  the zips plus the metadata JSON to JFrog under `fbs/<AssemblyName>`. Untagged pipelines upload to
  `vicione-suite-dev` (manual job); a tag uploads to `vicione-suite-staging`, and a manual promote
  moves it to `vicione-suite`.
- `NuGet.Config` carries no feed credentials (s7 and cip still hold `##…##` placeholders there);
  CI injects them with `dotnet nuget update source … -u $NuGet_User_JFrog -p $NuGet_PW_JFrog`.
  Configure the sources yourself for local development.

## Changelog and release

- Every change a suite user notices gets an entry in `CHANGELOG.md`, in the unreleased section
  (`## <next version> - Unreleased`), committed with the change.
- A release is its own merge request (`prepare-release-<version>`, one commit "Prepare release
  <version>"): `VERSION` gets the full version (`1.0.0-beta1`), and every `src/**/*.yaml` gets
  `Common.Version` (without a pre-release suffix — the ruleset only accepts `x.y[.z]`) and today's
  `Common.Date`. After the merge: an annotated tag `v<version>` on the merge commit, a GitLab release
  whose description is that version's changelog section, then the staging upload and the promote.
  dp-s7-absolute `docs/Siemens.S7.Absolute.Documentation/development/GitLab-Release-Process.md`
  has the steps.

## Repository interaction

These are GitLab projects: use `glab` (`glab mr view/create/update`, `glab issue view`). Never `gh`.
Where a repo has `.gitlab/merge_request_templates/` or `.gitlab/issue_templates/` (cip does), the
description follows the template.
