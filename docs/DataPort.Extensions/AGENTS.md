# DataPort Extensions — Agent Context

Entry point for coding agents. Ships at the **NuGet package root** and drives the docs site. Two
audiences: **[using](#using-this-package)** the package, or
**[maintaining](#maintaining-this-package)** it. Humans start at [README.md](README.md).

---

## Using this package

The package explains itself — docs, XML summaries, symbols and source all ship inside it. **Find the
restored folder first; every path below is relative to it.**

### Find the restored package

You're most likely reading this from your NuGet **global-packages cache**, in the restored
`vicione.suite.dataport.extensions/<version>/` folder (the id is **lowercased** there) — every path below
is relative to it. To re-locate it: the cache root comes from `dotnet nuget locals global-packages --list`,
and the version your solution actually resolved from `dotnet list package`. Assemble the path for your own
shell.

That folder holds `AGENTS.md` + `README.md` at its root, the `explanation/` `how-to/` `reference/`
curated docs, and `lib/net10.0/` with the assembly and its `.xml`.

### Reach for the cheapest tier

| Tier                  | Source                                                                                              | Reach for it when                           |
|-----------------------|-----------------------------------------------------------------------------------------------------|---------------------------------------------|
| 1 — curated docs      | `explanation/`, `how-to/`, `reference/` (catalogued in [README — Components](README.md#components)) | concepts, how-tos, role-organized reference |
| 2 — API summaries     | `lib/net10.0/ViciOne.Suite.DataPort.Extensions.xml`                                                 | an exact signature or per-member contract   |
| 3 — decompiled source | the shipped `.dll` (see [Tier 3](#tier-3--decompile-on-demand))                                     | behavior isn't clear from tiers 1–2         |

Also packed: an **embedded README** (gallery landing page; its Components table links into the curated
docs) and **embedded symbols + source** in the `.dll` (`<DebugType>embedded</DebugType>` +
`<EmbedAllSources>`) — a debugger steps straight into the real library source, no symbol/SourceLink
fetch. No stock .NET CLI pops those embedded files out as `.cs`, so for *static* reading, decompile.

### Tier 3 — decompile (on demand)

Install [`ilspycmd`](https://github.com/icsharpcode/ILSpy) (`dotnet tool install -g ilspycmd`), then run
against the copy in `lib/net10.0/` so the sibling `.xml` is found:

```bash
ilspycmd -p --nested-directories -usepdb --disable-updatecheck \
  -o <scratch-dir> lib/net10.0/ViciOne.Suite.DataPort.Extensions.dll
```

Output is **reconstructed C#** (one `.cs` per type), faithful to behavior. ILSpy folds the `.xml`
summaries back in as `///` comments and `-usepdb` recovers local variable names — but inline `//`
comments, `#pragma`s, and original formatting are lost. Write to a scratch dir, never the NuGet cache.

---

## Maintaining this package

Keep the package self-describing — the tiers only help a consumer's agent if they stay accurate.

- **Public API + docs change in one MR.** Signatures and member text live in XML doc comments (the
  generated API renders them), never duplicated in markdown. Add/remove a type → update the matching
  `reference/*.md` index; add an exception → `reference/exceptions.md`. Conventions:
  [documentation-principles.md](documentation-principles.md); code cheat-sheet: `src/DataPort.Extensions/AGENTS.md`.
- **Breaking changes** → the repo-root `CHANGELOG.md` (this package shares the repository's version),
  in short form and scope-tagged `` (`DataPort.Extensions`) ``. **Migration steps** → a per-release page
  under [how-to/migration/](how-to/migration/index.md), linked from the changelog's `Changed` section.
- **Keep these `DataPort.Extensions.csproj` settings on** — each silently degrades the package if removed:
  `<GenerateDocumentationFile>` (Tier 2 `.xml`), `<DebugType>embedded</DebugType>` + `<EmbedAllSources>`
  (embedded symbols/source), `<PackageReadmeFile>` (README).

### Pointer for consumer repos

Drop this into a consumer's `CLAUDE.md` / `AGENTS.md` so its agent opens this manifest after restore:

```markdown
## ViciOne.Suite.DataPort.Extensions

This package ships an agent manifest. After `dotnet restore`, find your restored copy in the NuGet
global-packages cache (`vicione.suite.dataport.extensions/<version>/` — id lowercased; the version is
whatever `dotnet list package` resolves) and read its **`AGENTS.md`** — it routes you to the docs, API,
and source.
```
