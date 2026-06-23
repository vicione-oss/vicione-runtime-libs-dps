# Excalidraw Diagrams in Markdown Docs

How diagrams in this repo's documentation work: the file convention, the
edit/export workflow, and the font fix that keeps SVGs rendering correctly.

## File convention

Each diagram is a pair of files with the same basename, in a `diagrams/` folder
next to the docs that use it:

```
docs/<area>/diagrams/<doc-prefix>-<slug>.excalidraw   # editable source (JSON)
docs/<area>/diagrams/<doc-prefix>-<slug>.svg          # export, embedded by the docs
```

- Markdown embeds **only the SVG**: `![Human-readable title](diagrams/<name>.svg)`
- Prefix basenames with a doc slug (`architecture-`, `pooling-`, …) so several
  docs can share one folder without collisions.
- Register both files in `dps.slnx` so they show up in the solution view.

## Adding or editing a diagram

1. Create/edit the `.excalidraw` file with the IDE Excalidraw plugin
   (IntelliJ/Rider or VS Code) or on excalidraw.com.
2. Export as SVG over the same basename, next to the source.
3. Run `dotnet run docs/diagram-creation/fix-excalidraw-fonts.cs` (idempotent; see below —
   **never skip this**, a fresh export always reintroduces the font problem).
4. Embed in markdown: `![Title](diagrams/<name>.svg)`.
5. Commit both files (and the slnx entries for new diagrams).

## Why exports are manual (no CLI)

There is no official Excalidraw CLI. Excalidraw's SVG export is not a standalone converter — the
`exportToSvg` logic lives in its browser JavaScript packages and depends on a
DOM, canvas-based text measurement, and the loaded fonts. Automating it means
either:

- **node + jsdom** faking a browser — known to be finicky around fonts and text
  measurement (labels clip or misalign), or
- **a real headless browser** (Playwright/Puppeteer driving the Excalidraw app,
  which is what community tools like `excalidraw-brute-export-cli` do) — works,
  but pulls a browser download and a node toolchain into a .NET repo.

Writing our own renderer for the JSON would mean reimplementing the hand-drawn
(roughjs) style and maintaining it forever. Against that, exports change rarely
and the IDE plugin exports in two clicks — so the pragmatic trade-off is:
manual export, followed by the font-fix script below.

## The font problem (why the script exists)

Excalidraw SVG exports (at least via the jetbrains plugin) declare the handwriting font as a **remote** download:

```css
@font-face { font-family: "Virgil"; src: url("https://excalidraw.com/Virgil.woff2"); }
```

Markdown renders SVGs as *images*, and browsers block all external requests
inside image-context SVGs. The font never loads and text silently falls back to
the next stack entry (`Segoe UI Emoji`!) or the browser default. Traps:

- Opening the SVG **directly** in a browser tab is *document* context — the
  download is allowed, the diagram looks fine, and the bug stays hidden.
  Test the embed context instead: DevTools → Network → block the font URL →
  reload → Computed → "Rendered Fonts".
- A `@font-face` rule **claims** the family name: a locally installed Virgil is
  ignored unless the `src` list contains `local(...)` entries.
- The font's installed name is **not** "Virgil" — it is `Virgil 3 YOFF`
  (PostScript: `Virgil3YOFF`), so `local("Virgil")` never matches.
- Every re-export from the plugin resets the SVG, so the fix must be a
  **re-runnable post-export step**, not a one-off edit.

**The fix** — `docs/diagram-creation/fix-excalidraw-fonts.cs` (a .NET file-based app, run via
`dotnet run docs/diagram-creation/fix-excalidraw-fonts.cs [folder ...]`; without arguments it
processes all `docs/**/diagrams` folders) rewrites every Excalidraw export
(identified by the `svg-source:excalidraw` marker) to be local-only:

```css
@font-face { font-family: "Virgil"; src: local("Virgil 3 YOFF"), local("Virgil3YOFF"); }
```

and sets all text to `font-family="Virgil, Helvetica"`. Machines with the font
installed get real handwriting; everyone else gets a clean sans-serif. No
network, no embedded font bytes, works offline and in every embed context.

Rejected alternatives: base64-embedding the woff2 (~100 KB per SVG), or
switching diagrams to the "Normal" font entirely (zero maintenance, loses the
hand-drawn look).

## Stencil library & style standard

A shared Excalidraw library standardises the shapes, colours, arrows and type
across the diagrams so they look like one set. It lives alongside this guide in
`docs/diagram-creation/`:

```
docs/diagram-creation/diagram-stencils.excalidrawlib       # the library (drag-and-drop stencils)
docs/diagram-creation/diagram-stencils-showcase.excalidraw # visual sheet of every stencil, captioned
```

The `.excalidrawlib` is the **single source of truth** for diagram styling.
The showcase is a rendered overview — open it to see everything at a glance.

### Loading the library

In the IDE Excalidraw plugin (Rider/IntelliJ or VS Code) or on excalidraw.com:
open the **library panel** → *Open library*, then pick the `.excalidrawlib`
(or just drag the file onto the canvas). The stencils then appear for
drag-and-drop. Note: Rider's text editor shows the file as raw JSON — you need
the Excalidraw plugin view to see it render.

### The palette

Component boxes — `rectangle`, stroke `#1e1e1e`, width 2, solid fill, rounded
corners — vary only by fill colour, which carries the meaning:

| Role                  | Fill swatch         |
|-----------------------|---------------------|
| Primary component     | `#a5d8ff` (blue)    |
| Active / data         | `#b2f2bb` (green)   |
| Config / intermediate | `#ffec99` (yellow)  |
| Error / failure       | `#ffc9c9` (red)     |
| External              | `#d0bfff` (violet)  |
| Neutral container     | `#f1f3f5` (gray)    |

Plus a **muted/secondary box** (stroke `#ced4da`, fill `#f1f3f5`, width 1) and
a **decision diamond** (stroke `#1e1e1e`, fill `#f1f3f5`, rounded).

**Lifecycle nodes:** a *start* node (filled black circle) and an *end* node
(final state — a ring around a filled black dot).

### Connectors

`arrow`, width 2, curved, single `arrow` head (never `triangle`):

| Role               | Stroke              | Style  |
|--------------------|---------------------|--------|
| Flow               | `#1e1e1e` (black)   | solid  |
| Return / async     | `#1e1e1e` (black)   | dashed |
| Error              | `#e03131` (red)     | solid  |
| Muted / optional   | `#868e96` (gray)    | dashed |

Plus a **separator** (`line`, black, width 2, dashed, sharp).

### Shared defaults

Every stencil uses `fillStyle: "solid"`, `roughness: 1`, and the **Virgil**
hand-drawn font (`fontFamily: 1`); text is `#1e1e1e`, or `#495057` for
secondary text/captions. Shapes use `strokeWidth: 2` and rounded corners
(`roundness: { "type": 3 }`); connectors use `roundness: { "type": 2 }`.
The **type scale** is just three sizes: **16** (title) · **14** (body) ·
**13** (small / caption) — avoid other sizes.

### Adding or changing a stencil

Edit `diagram-stencils.excalidrawlib` (it is the source of truth): in the
plugin, load the library, draw/adjust a shape on canvas, then *add to library*
and *export library* over the same file — or edit the JSON directly. Keep new
stencils within the palette and shared defaults above. If you add one, also
drop it into `diagram-stencils-showcase.excalidraw` so the sheet stays
complete, then re-export its SVG (`docs/diagram-creation/fix-excalidraw-fonts.cs`, as above).
