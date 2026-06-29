# Documentation Principles

These docs follow the [Diátaxis](https://diataxis.fr/) framework — a systematic approach to technical documentation
organised around what users need, not what the product does.

![Diátaxis quadrant diagram](https://diataxis.fr/_images/diataxis.png)

---

## The four types

Diátaxis identifies four distinct documentation needs, each requiring a different kind of writing:

### Tutorials

*Learning-oriented.* A tutorial guides a learner through a hands-on activity to acquire new skills. The goal is
learning, not task completion. This project has no tutorials yet.

### How-to guides

*Task-oriented.* A how-to guide is a recipe: step-by-step directions for an already-competent user to accomplish a
specific goal. It omits teaching and background. It assumes the reader knows *what* they want to do and focuses entirely
on *how* to do it.

### Reference

*Information-oriented.* Reference material describes things precisely and completely. It is consulted, not read
sequentially. Its structure mirrors the product — one section per interface, type, or exception. It contains no
instruction and no opinion.

### Explanation

*Understanding-oriented.* Explanation discusses *why* — design rationale, trade-offs, conceptual context — at a higher
level than the other types. It is the only type suitable for reading away from a keyboard. It does not instruct; it
illuminates.

---

## The two axes

The four types sit on two axes:

|                         | Action (practical) | Cognition (theoretical) |
|-------------------------|--------------------|-------------------------|
| **Acquisition (study)** | Tutorials          | Explanation             |
| **Application (work)**  | How-to guides      | Reference               |

- **Action ↔ Cognition**: whether the writing serves hands-on work or conceptual understanding
- **Acquisition ↔ Application**: whether the reader is learning something new or applying existing knowledge

---

## How we apply it

| Folder         | Type         | Answers                                              |
|----------------|--------------|------------------------------------------------------|
| `explanation/` | Explanation  | *Why* is this designed this way?                     |
| `how-to/`      | How-to guide | *How* do I implement a DataPort?                     |
| `reference/`   | Reference    | *What exactly* is this interface / type / exception? — a curated role-organized index plus the generated API under `reference/api/` |

When adding content, identify which question it answers and place it in the matching folder. If no existing file fits,
create a new one.

---

## What belongs where

The markdown docs and the source code serve different readers. Keep them from overlapping:

| Belongs in markdown docs                             | Belongs in XML doc comments / IntelliSense |
|------------------------------------------------------|--------------------------------------------|
| Why an abstraction exists                            | What a method or property does             |
| How the pieces fit together                          | Parameter meaning and allowed values       |
| Which parts you implement vs. which the base handles | Return value semantics                     |
| Behavioral contracts and ordering guarantees         | Exception conditions                       |
| Design rationale for non-obvious decisions           | Type constraints                           |
| When to choose one strategy over another             | Overload differences                       |
| Common pitfalls and invariants                       | Deprecation notices                        |

If a detail is already expressed by the source code and surfaced by IntelliSense, it does not belong in the markdown
docs. Duplicated implementation details go stale silently.

---

## Keeping docs in sync

Doc changes that match a source change belong in the same merge request. Signature and member changes belong in the
XML doc comments (the generated API renders them); add or remove a type and you update the matching reference index
(`reference/interfaces.md`, `reference/model.md`). If you add a new exception, add it to `reference/exceptions.md`.
