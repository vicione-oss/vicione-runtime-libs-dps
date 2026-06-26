# DataPort Extensions

Reusable base classes for building protocol-specific DataPort implementations. The library handles connection lifecycle,
polling, write queuing, retry, and value validation — you implement the protocol-specific pieces.

---

## New to this library?

- **Building a DataPort implementation for the first time** — start with [Overview](explanation/Overview.md) for
  the design concepts, then go to [Building a DataPort](how-to/index.md), which walks the shared
  building blocks before the incoming and outgoing tracks.
- **Looking something up** — use the component table below.

---

## Components

### Explanation

|                                                                                    |                                                                                                 |
|------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------|
| [Overview](explanation/Overview.md)                                                | Core design: typed nodes, mapper layers, YAML consistency tests                                 |
| [Incoming data port](explanation/incoming/incoming-data-port.md)                   | Why the polling model is designed the way it is                                                 |
| [Outgoing data port](explanation/outgoing/outgoing-data-port.md)                   | Why the queue-based write model is designed the way it is                                       |
| [Typed node model](explanation/configuration/typed-node-model.md)                  | Why the configuration-time tree, node roles, and mapper dispatch work the way they do           |
| [Internal data model](explanation/configuration/internal-data-model.md)            | Why the runtime model is flat/grouped rather than a tree                                        |
| [Connection lifecycle](explanation/connection/connection-lifecycle.md)             | Why disconnect is reversible but dispose is terminal, and why the library never auto-reconnects |
| [Client lifecycle manager](explanation/connection/client-lifecycle-manager.md)     | Why IClientLifecycleManager is separate from IReadClient/IWriteClient                           |
| [Configuration verification](explanation/connection/configuration-verification.md) | Why verification is opt-in, and why it requires a factory method                                |

### How-to

|                                                                      |                                                                                         |
|----------------------------------------------------------------------|-----------------------------------------------------------------------------------------|
| [Building a DataPort](how-to/index.md)                               | The build roadmap and the worked test example                                           |
| [Shared building blocks](how-to/index.md#shared-building-blocks)     | Configuration schema, node model, data points, connection — linked from both directions |
| [Incoming data port](how-to/incoming-dataport/incoming-data-port.md) | Base class for incoming (read/poll) data ports                                          |
| [Outgoing data port](how-to/outgoing-dataport/outgoing-data-port.md) | Base class for outgoing (write) data ports                                              |

### Reference

The full per-member API reference is **generated from the source XML documentation** with
[DocFX](docfx.json) and rendered as an HTML site under [`reference/api/`](reference/api/index.md). The
curated pages below are its companion — they organize that API by role and add the cross-cutting facts
the per-type docs can't carry (where each exception arises, the log-event catalog, how the types relate),
linking into the generated API for the members. Design rationale lives under Explanation; wiring under
How-to:

|                                                   |                                                                   |
|---------------------------------------------------|-------------------------------------------------------------------|
| [Interfaces](reference/interfaces.md)             | Extension contracts grouped by role, linking into the API         |
| [Model](reference/model.md)                       | Runtime model and configuration-mapping types, grouped by role    |
| [Exceptions](reference/exceptions.md)             | Where each exception arises in the framework flow                 |
| [Queue](reference/queue.md)                       | The bounded write-queue building blocks                           |
| [Value validation](reference/value-validation.md) | The conversion + range-validation types and the log-event catalog |
| [Resilience](reference/resilience.md)             | The retry, backoff, and resilience-pipeline types                 |

---

## Building the documentation site

The whole `docs/` tree (every area plus this area's generated API reference) builds into **one combined
site** with a single command — this is what GitLab Pages publishes:

```bash
dotnet tool restore            # restores DocFX (pinned in .config/dotnet-tools.json)
dotnet docfx docs/docfx.json --serve   # build the full site + preview at http://localhost:8080
```

This area can also be built/served **standalone** (handy while editing — and how it would ship on its
own, keeping its Home / Explanation / How-to / Reference / API top tabs):

```bash
dotnet docfx docs/DataPort.Extensions/docfx.json --serve   # this area only, http://localhost:8080
```

The generated API pages and the `_site/` output are git-ignored and rebuilt on demand. See the repo
[`AGENTS.md`](../../AGENTS.md) (*Documentation*) for the full build/publish flow and the CI Pages jobs.

The `docs/` tree is shown in the solution (`dps.slnx`) through non-building MSBuild `NoTargets`
projects, so there is no per-file list to maintain — `.slnx` solution folders can't glob. The docs
projects mirror the `src/` layout: this area is surfaced by `DataPort.Extensions.Docs.csproj`
alongside these files (with `docs/DataPort/` and `docs/DataPort.Extensions.Testing/` getting their own,
and `docs/Docs.csproj` left holding the shared `diagram-creation/` tooling). They compile nothing, so
`dotnet build` and CI treat them as no-ops; see the repo [`AGENTS.md`](../../AGENTS.md) for the full
reasoning.

### Known build warnings: cross-area links (standalone build only)

The **combined** build ([`docs/docfx.json`](../docfx.json)) roots every area under `docs/`, so all
cross-area links resolve and it builds clean — that is what GitLab Pages publishes.

The warnings only appear in the **standalone** area build ([`docfx.json`](docfx.json)), which is
**scoped to `docs/DataPort.Extensions/`** (the directory holding it is the build root). A handful of
curated pages link to docs in *sibling* areas — outside this root:

- `docs/DataPort/explanation/` — the upstream DataPort core concepts (`DataPortCommunication`, `LinkedNode`)
- `docs/DataPort.Extensions.Testing/` — the testing companion package

Because those targets sit outside the standalone build root, `dotnet docfx build` reports them as
`InvalidFileLink` warnings. **The links themselves are correct** — they resolve when the markdown is
viewed in GitLab / the repo (relative paths) and in the combined site. The warnings are **accepted** for
the standalone build and deliberately *not* silenced (turning off link validation would also hide
genuinely broken links).

> Note: a separate, transient class of warning appears when a diagram's `.svg` hasn't been exported
> yet (only the `.excalidraw` source exists) — see [diagrams-howto.md](../diagram-creation/diagrams-howto.md). Those
> clear once the SVG is exported.

---

See [CHANGELOG.md](CHANGELOG.md) for breaking changes and migration steps.

---

*Editing these docs? See [Documentation Principles](documentation-principles.md).*
