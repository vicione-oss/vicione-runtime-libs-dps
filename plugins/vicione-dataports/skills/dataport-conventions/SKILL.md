---
name: dataport-conventions
description: How every ViciOne DataPort provider (Modbus, S7, Allen-Bradley, OPC UA, MQTT, SQL, …) is built and what it must guarantee — what the built tree promises, how a value that cannot be read is handled, what may travel on the wire, where validation belongs, what a provider may depend on — and how to build or extend one on ViciOne.Suite.DataPort.Extensions: the ruleset YAML, the typed node model (TypedNodeMapper), IncomingDataPortBase / OutgoingDataPortBase or a hand-written push port, clients and client pools, configuration verifier and connection tester, tests and CI packaging. Read this before creating a dp-* repository or provider, adding a node type, data type or property, changing what a port puts on the wire, how it reads a received message or what it validates, or reviewing such a change.
---

# DataPort conventions

Rules that hold for every provider built on `ViciOne.Suite.DataPort`, whatever the protocol.
Protocol specifics belong in the provider repository's own `AGENTS.md` and its architecture
decision records. How a provider is built follows the rules below, with its detail in
`references/`.

## The built tree is the contract

The tree the Suite builds is the agreement between the Suite and the target system. What the tree
declares is what travels, and nothing about a value at runtime may change that.

So a port sends a declared key **whatever the value's state** — including an engine validity of
`0`. It is tempting to treat an invalid value as a reason to leave its key off the message; that
silently makes the wire format depend on runtime data, and the receiving side, which was built
against the tree, sees a field disappear for reasons it cannot know. Metadata rules that are not
part of the tree are hidden rules, and hidden rules are how two systems that both follow the
specification stop understanding each other.

The one thing that legitimately omits a key is the absence of a value: a channel the engine has
never written has nothing to put on the wire.

## The `.yaml` is the source of truth for the tree

Every provider carries a ruleset YAML (`src/<Provider>/<Provider>.yaml`) that defines the node
tree the editor offers and the port serves. It is the source of truth, not a description of the
code: every property in it needs its counterpart on the communication or node class, and the
consistency tests in `DataPort.Extensions.Testing` verify exactly that. A member that deliberately
has no counterpart goes in the test's `Excluded…` sets, with a comment saying why.

A change to the tree therefore starts in the YAML. Its `Common.Version` is set by the release
commit alone, to the provider version without a pre-release suffix; nothing reads the field, and
`ViciOne.Tree.Builder` only checks its `x.y[.z]` format. For the YAML itself, use the
`writing-ruleset-yaml` skill in the `tree-builder` repository — it is repo-local, so from a provider
repository read it on GitLab. [references/yaml-schema.md](references/yaml-schema.md) covers what a
DataPort adds.

## Validate the tree when the port starts

A tree the port cannot serve is refused **at startup**, not message by message. Everything that is
decidable from the tree alone — a child in a position the protocol has no room for, a duplicate or
reserved key, a limit the protocol imposes, a node that transfers no value of its own — is checked
once, when the port comes up, and reported as a start error.

The alternative fails every message with the same error forever, and only once data is flowing.
The engine shows a start error where an operator will see it; a per-message log is where an
operator will not.

## Never invent a value

When a port cannot read something — a payload the configured data type cannot deserialize, a
property that is not a value of its declared type, a member of a group message — it forwards **no
value** for it. The channel keeps what it had.

The default of a data type is not a safe stand-in: `0`, `false` and the empty string are all
readings a sender could legitimately have produced, so the engine cannot tell a substituted value
from a real one. A value that cannot be read is nearly always an infrastructure problem —
mismatched versions, a wrong data flow — and it has to stay visible as one.

This is per value, not per message: the other values of the same message are forwarded as usual.
Each one is its own data point's business.

**The documented exception is time.** A received timestamp that cannot be read is replaced by the
time the message was received, because every value needs *some* point in time to reach the engine
with. That substitution is reported. A message that carries no timestamp at all is not an error and
is not reported.

## A value read from a message is never more valid than the message

When one received message fans out into several engine values — the data point's own value plus
anything read from the message's metadata — all of them carry the validity of that message. A
sender that marks a message invalid marks everything read from it invalid.

Without this, an engine that reads only the fanned-out value acts on data whose sender declared it
unusable, and nothing in the value says so. The value is still forwarded, flagged rather than
dropped, so the engine sees the reading together with the state its sender gave it.

The base classes' `ToExternalValue` default of `Validity = 1` fits a polled read only; an override
that fans out a received message passes the message's validity on.

## DataPorts carry data, not diagnostics

Only what is part of the agreed data exchange goes on the wire. Engine cycle counters, internal
identifiers, timing information and anything else that exists to help us debug do not — they are
not part of what the two systems agreed on, and a field added for one investigation is a field the
other side has to carry forever.

Diagnose through logs, traces and the engine's own tooling instead.

## Do not re-check what the engine already checks

The managed engine validates what it hands a port and what a port hands back. A second check in
the provider is not free safety: it is a second place to keep in sync, it reports the same problem
twice in different words, and it goes stale when the engine's rule changes.

Check what only the provider can know — the protocol's limits, the tree's shape, what the remote
system sent. Trust the engine for the rest.

## Bound what a foreign sender can drive

A remote party decides how often a provider's error paths run. Two consequences, both of which
have shipped as defects:

- **Deduplicate the diagnostics.** A warning emitted once per bad message becomes one warning per
  message per second for as long as a misconfigured publisher runs, and buries the log it was added
  for. Report a distinct problem once per source — per topic, per node, per remote endpoint.
- **Memoize the work behind them.** A failing type resolution, parse or reflection lookup on a
  sender-supplied string costs the same on every message. Resolve a given input once and keep the
  answer, so a bad sender costs a bounded amount rather than a per-message one.

The same applies to anything a sender can name that the provider then loads or resolves: bound it,
and keep the resolution inside what is already reachable.

## A provider merge request does not wait for a new `dps` release

A provider change that would need a new API in `ViciOne.Suite.DataPort` blocks on a release of
another repository, and blocked merge requests age badly. Look for the solution inside the API the
provider already consumes — the need for a new seam is usually a sign that the change is reaching
for something it does not need.

Where a new API genuinely is the right answer, it is proposed as its own issue against `dps` and
the provider change ships without it.

---

These rules were extracted from provider reviews, most of them from more than one. When a rule
turns out to be wrong for a protocol, change it here rather than working around it there.

## Building a provider

### Step 0 — pull or push, decided by the protocol

`ViciOne.Suite.DataPort.Extensions` (repo `dps`) ships the shared building blocks and two base
classes. The incoming base **polls**: `IncomingDataPortBase` requires `TDataPoint : IPollingDataPoint`
and reads on a schedule. dps says itself that a protocol which pushes "does not sit naturally on the
current design" (`docs/DataPort.Extensions/explanation/Overview.md`).

| | Pull — the provider asks | Push — the remote side sends |
|---|---|---|
| Protocols | Modbus, S7, Allen-Bradley, … | MQTT, AMQP, OPC UA subscriptions |
| Configuration | typed node tree via `TypedNodeMapper` | same |
| Incoming | `IncomingDataPortBase` | hand-written `IExternalIncomingCommunication<T>` on the shared blocks |
| Outgoing | `OutgoingDataPortBase` | `OutgoingDataPortBase` — it queues and writes, nothing in it polls |
| Built that way | dp-modbus, s7, dp-s7-absolute, cip | — |

- **New providers use the typed node tree in both directions** and inherit a base class wherever the
  protocol fits it. A push protocol writes its incoming port by hand on top of the same node model,
  mappers, data points and pool; it does not wait for a push base class in dps.
- dp-mqtt, dp-opcua, dp-sql, dp-azure and dp-aws predate the typed tree and implement
  `IExternal*Communication` directly. Maintain them in place, don't copy their raw `INode` handling
  into a new provider, and don't half-migrate one as a side effect of another task.

**Canonical references, in order** (GitLab projects under `vicione-oss/vicione/runtime-libs/`,
paths relative to the repository root):

1. dps `docs/DataPort.Extensions/how-to/` — the authoritative recipes (`index.md` is the map). Read
   the relevant guide before writing code. They also ship inside the
   `ViciOne.Suite.DataPort.Extensions` package (`how-to/`, `explanation/`, `reference/` and an
   `AGENTS.md` at the package root), so a provider repo reads them from the NuGet cache without a
   dps clone.
2. dps `tests/DataPort.Extensions.Tests/Stubs/` — a complete, compiling minimal provider.
3. cip `src/AllenBradley.Logix/` + `src/AllenBradley.Client.Shared/` — newest provider: a generic
   shared client pool, raw-node property validators, a configuration verifier in both directions.
   Not a template for packaging or repo layout (no connection tester, README or CHANGELOG).
4. dp-modbus `src/Modbus.Client/`, `src/Modbus.Server/`, `src/Modbus.Core/` — client **and**
   server, both directions, a shared core project; the template for packaging and repo layout.
5. dp-s7-absolute `src/Siemens.S7.Absolute/` — the richest data-type set (scalars + arrays) and
   `docs/Siemens.S7.Absolute.Documentation/how-to/add-a-data-type.md`.

dps `src/DataPort.Extensions/AGENTS.md` is the agent-facing cheat sheet for the base classes.

### Build order

Shared blocks first, direction-specific last. Each step compiles and is testable on its own.

| # | Build | Contract | Lives in |
|---|-------|----------|----------|
| 1 | YAML ruleset | — | `src/<Provider>/<Provider>.yaml` |
| 2 | Communication record | `[Communication("<Root.Id>")] … : DataPortCommunication` | `Model/Nodes/Device/` |
| 3 | Root node + mapper + validator | `IRootConfigurationNode<TCommunication>` | `Model/Nodes/Device/{,Mapping/}` |
| 4 | Branch nodes + mappers (optional) | `IBranchConfigurationNode` | `Model/Nodes/Containers/<X>/` |
| 5 | Leaf nodes + mappers + validators | `IDataPointNode` | `Model/Nodes/DataPoints/<X>/` |
| 6 | Mapper composition root | `TypedNodeMapper<TComm, TRoot>` | `Model/Nodes/Mapper/` |
| 7 | Runtime data points + values | `IPollingDataPoint`, `ITypedDataPoint<T>` | `Model/DataPort/DataPoints/` |
| 8 | Client information, client, pool | `IClientInformation`, `IReadClient`/`IWriteClient`, `IClientLifecycleManager` | `Model/DataPort/Device/`, `Client/`, `Client/Pool/` |
| 9 | Data-point mappers | `IDataPointsMapper` / `IDataPointGroupsMapper` | `Model/Mapping/` |
| 10 | The ports | `IncomingDataPortBase` (pull) / hand-written (push), `OutgoingDataPortBase` | `Incoming/`, `Outgoing/` |
| 11 | Connection tester | `IDataPortConnectionTester<TCommunication>` | `src/<Provider>/` |
| 12 | Packaging: `metadata.json`, CI component jobs, `VERSION*`, `CHANGELOG.md` | — | `src/<Provider>/`, `.gitlab-ci.yml` |

Full detail with the code shape of every step: [references/build-order.md](references/build-order.md).

### The rules that break builds

1. **`TreatWarningsAsErrors` + `EnforceCodeStyleInBuild`** — a single warning fails the build. Run
   the formatters as part of "done", not at review time.
2. **No exceptions on engine/hot paths.** Log through a `[LoggerMessage]` source-generated
   companion class (`XLogs`/`XLogMessages`) instead of throwing; add new messages there, never an
   inline `_logger.LogX(...)` with interpolation. Connection failures are the exception: those
   surface as `ConnectionFailureException`, never a protocol-specific type — the lifecycle manager
   and both base classes depend on that uniform surface.
3. **No LINQ** in code that runs per poll, per message or per write — `SendAsync`, queue
   processing, `ToExternalValues` overrides, read/write loops. Indexed `for`, pre-sized `List`; s7
   measured ~3.5× on `ToExternalValues` alone.
4. **One type per file**, UTF-8 without BOM (`.editorconfig` is authoritative), package versions
   only in `Directory.Packages.props`.

### Verification

The "done" gate — build and both format checks must pass:

```bash
dotnet build <repo>.slnx && dotnet format analyzers <repo>.slnx --severity info --verify-no-changes && dotnet format whitespace <repo>.slnx --verify-no-changes
```

Take the test command from the repo's `AGENTS.md`; see [references/testing.md](references/testing.md).

## Reference files

- [references/build-order.md](references/build-order.md) — each step's code shape, the seams, and
  the base-class hooks worth overriding
- [references/yaml-schema.md](references/yaml-schema.md) — the DataPort side of the ruleset: what
  the C# model pairs with, and the gotchas (UInt8 boxing, Id-keyed `DependentProperties`, grouped
  `DataTypes` leaves, declarations nothing enforces)
- [references/conventions.md](references/conventions.md) — the remaining build settings, the
  exception vocabulary, connection lifecycle decisions, outgoing semantics, packaging and release
- [references/testing.md](references/testing.md) — the expected test set per provider and the
  fixtures that make it deterministic
- [references/repo-layout.md](references/repo-layout.md) — folder map, new-repo scaffolding and the
  CI publish jobs

## Before reporting done

- [ ] The contract above holds — declared keys always sent, no invented values, validity carried
      over, tree checked at startup
- [ ] Typed node tree; base class wherever the protocol fits it, with the shared blocks actually
      shared between the two directions
- [ ] YAML ↔ C# consistency test green, with justified exclusions only
- [ ] Both directions covered, or an explicit statement of why only one exists
- [ ] Build + both `dotnet format` checks clean; unit tests green (integration tests may be skipped —
      say so)
- [ ] A `CHANGELOG.md` entry under the unreleased version for anything a suite user notices
- [ ] `metadata.json`, `VERSION`/`VERSION_CORE`, and the CI component jobs exist for a new provider
