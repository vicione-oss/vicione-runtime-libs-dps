---
title: DataPort Extensions
---

# DataPort Extensions

Reusable base classes for building protocol-specific DataPort implementations. The library handles connection
lifecycle, polling, write queuing, retry, and value validation — you implement the protocol-specific pieces.

## New to this library?

- **Building a DataPort implementation for the first time** — start with the [Overview](explanation/Overview.md)
  for the design concepts, then go to [Building a DataPort](how-to/index.md), which walks the shared
  building blocks and then the incoming or outgoing track.
- **Looking a type up** — the [API reference](reference/api/index.md) is generated from the source XML documentation;
  the [Reference](reference/interfaces.md) guides organize it by role and add the cross-cutting facts the
  per-type docs can't carry (where each exception arises, the log-event catalog, how the types relate).

## Sections

| Section                                | What you'll find                                                                                          |
|----------------------------------------|-----------------------------------------------------------------------------------------------------------|
| [Explanation](explanation/Overview.md) | Why the library is designed the way it is — typed nodes, mapper layers, lifecycle phases                  |
| [How-to guides](how-to/index.md)       | Step-by-step recipes: the shared building blocks, then the incoming and outgoing tracks                   |
| [Reference](reference/interfaces.md)   | A role-organized index into the generated API, with the cross-cutting facts the per-type docs can't carry |
| [API](reference/api/index.md)          | Full generated API reference (every public type and member, from the XML summaries)                       |

See [CHANGELOG.md](CHANGELOG.md) for breaking changes and migration steps.

---

[!INCLUDE [Documentation principles](documentation-principles.md)]
