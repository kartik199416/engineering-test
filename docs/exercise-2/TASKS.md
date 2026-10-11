# Exercise 2 — Plan and task breakdown

Started as a plan before anything was read in depth; the breakdown below was
added once [SOLUTION-DESIGN.md](SOLUTION-DESIGN.md) had been reviewed and agreed.
The ordering is deliberate: nothing is refactored until there are tests that
would catch the refactor breaking it.

Each task maps to one commit, which is why the history reads as a sequence of
safe steps rather than one drop.

## What the brief asks for

One new behaviour: **"Conjured" items degrade in Quality twice as fast as normal
items.** Everything else in the exercise is a consequence of where that change
has to land.

## Constraints I have to work inside

Straight from the README, and non-negotiable:

- The `Item` class and the `Items` property must not be altered.
- Everything that works today must still work.
- It has to build through `build.bat`.

The first one is the constraint that shaped the design, because it rules out the
obvious answer of putting behaviour on `Item` itself.

## Task breakdown

| # | Task | Why it comes here | Commit | Status |
|---|---|---|---|---|
| 0 | Read the brief; write down the plan, the constraints and the risks | Cheap, and it stops the design starting from the code instead of the requirement | `docs: capture the brief, the constraints and the plan` | ☑ |
| 1 | Draft the solution design against the legacy behaviour | Decisions are worth more when they demonstrably predate the code | `docs: first draft of the solution design` | ☑ |
| 2 | Get the draft reviewed; argue the alternatives and pin the assumptions | A design nobody challenged is a guess with formatting | `docs: revise the design after review` | ☑ |
| 3 | Break the agreed design into this list | Turns a design into a sequence of commits with a defined stopping point | `docs: break the agreed design into tasks` | ☑ |
| 4 | Characterise the legacy behaviour with tests | A refactor without a safety net is a rewrite. These must pass against untouched legacy code | `test: pin legacy UpdateQuality behaviour` | ☑ |
| 5 | Extract `IItemUpdater` + `ItemUpdater` template base | Establishes the shared day-end algorithm and the single clamping point | `refactor: extract the item updater abstraction` | ☑ |
| 6 | Add `ItemCategory`, the classifier and the checked registry; add the four existing-category updaters; delegate from `UpdateQuality` | Behaviour-preserving by construction; task 4's tests are the proof. Dispatch goes in as three types from the start — an `if`-chain would make precedence a side effect of statement order | `refactor: replace nested conditionals with per-category updaters` | ☑ |
| 7 | Add `ConjuredItemUpdater`: one enum member, one rule, one registry entry, one class | The actual feature. Should be small — if it is not, tasks 5–6 were wrong | `feat: add Conjured items, degrading at twice the normal rate` | ☑ |
| 8 | Add classification, dispatch and boundary tests — precedence, exhaustiveness, pass tiers, quality limits | The kata's bugs live on boundaries; sampling them is not enough. Precedence is published as data, so assert it directly rather than inferring it from one name | `test: cover dispatch rules and the quality bounds` | ☑ |
| 9 | Write up how the code actually executes, once it has settled | A flow document written before the code describes an intention, not a system | `docs: write up the execution flow` | ☑ |
| 10 | Verify `build.bat` and the full suite; close out the design with what changed | "It builds" is a claim until observed | `docs: close out the design doc` | ☑ |
| 11 | Record AI usage | Required by the brief; written last so it is accurate rather than aspirational | `docs: record where AI was used` | ☑ |

## Definition of done

Taken from the brief and the feedback I was given, not invented:

- [x] Conjured items degrade twice as fast, and four times as fast once past the sell date
- [x] `Item` and `Items` untouched — verifiable with `git diff` on the `Item` class
- [x] All five original categories behave exactly as before, proven by tests written *before* the refactor
- [x] `build.bat` runs clean
- [x] Full test suite green, no skipped tests
- [x] Design document covering options considered, the choice, trade-offs and assumptions
- [x] AI usage disclosed explicitly, with the split between my work and the tool's made clear
- [x] Commit history that shows the order the work happened in, not a single drop

## Risks identified up front

| Risk | Mitigation |
|---|---|
| Off-by-one on the backstage tiers — the legacy checks `< 11` / `< 6` *before* the decrement | Characterisation tests at `SellIn` 11, 10, 6 and 5 written before touching the code |
| Silently changing the post-sell-date doubling while "tidying" | Expressed once in the base class template, and pinned by tests for every category |
| Over-engineering the solution and losing the reviewer | Options A–D written down with the rejected ones kept visible, so the cost is argued rather than assumed |
| Accidentally touching `Item` while refactoring around it | Explicit check before the final commit |
