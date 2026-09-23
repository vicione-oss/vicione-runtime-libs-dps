---
name: dataport-conventions
description: Contracts and design rules shared by every ViciOne DataPort provider (MQTT, OPC-UA, SQL, Modbus, …) — what the built tree guarantees, how a value that cannot be read is handled, what may and may not travel on the wire, where validation belongs, and what a provider may depend on. Read this before changing what a DataPort puts on the wire, how it reads a received message, or what it validates.
---

# DataPort conventions

Rules that hold for every provider built on `ViciOne.Suite.DataPort`, whatever the protocol.
Protocol specifics belong in the provider repository's own `AGENTS.md` and its architecture
decision records; general .NET and review conventions are in the `vicione-conventions` skill.

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
consistency tests in `DataPort.Extensions.Testing` verify exactly that.

A change to the tree therefore starts in the YAML, and its `Common.Version` moves with the ruleset
format of `ViciOne.TreeBuilder`. For the YAML itself, use the `writing-ruleset-yaml` skill in the
`tree-builder` repository.

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
