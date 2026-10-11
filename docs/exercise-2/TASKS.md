# Exercise 2 — Plan

First thing written. No code read in depth yet beyond a skim of `Program.cs`, and
no design decided — this is scope, constraints and the order I intend to work in,
so that the design that follows has something to be measured against.

## What the brief asks for

One new behaviour: **"Conjured" items degrade in Quality twice as fast as normal
items.** Everything else in the exercise is a consequence of where that change
has to land.

## Constraints I have to work inside

Straight from the README, and non-negotiable:

- The `Item` class and the `Items` property must not be altered.
- Everything that works today must still work.
- It has to build through `build.bat`.

The first one is the constraint that will shape the design, because it rules out
the obvious answer of putting behaviour on `Item` itself.

## Order of work

Deliberate, and the reason is the same each time — do not change code you cannot
prove you have not broken:

1. Read `UpdateQuality` properly and write down what it actually does, not what
   it looks like it does.
2. Write a design before touching code, and have it reviewed before committing
   to it.
3. Turn the agreed design into a task list.
4. Pin the current behaviour with tests, against the legacy code, before any
   refactoring.
5. Refactor.
6. Add Conjured.
7. Keep the documents honest as things change.

## Risks I can see from here

| Risk | How I plan to handle it |
|---|---|
| Off-by-one on the backstage tiers — the legacy code checks `SellIn` *before* it decrements it | Characterisation tests on the tier boundaries, written before I touch anything |
| Quietly changing the post-sell-date doubling while tidying up | Express it exactly once, and pin it with tests per category |
| Over-engineering, and losing the reviewer in indirection | Write the options down, including the ones I reject, so the cost is argued rather than assumed |
| Touching `Item` by accident while refactoring around it | Explicit `git diff` check before the final commit |

## Open questions

Noted now, answered in the design:

- Does "twice as fast" compound with the existing "twice as fast once the sell
  date has passed", giving 4 per day, or is it capped at 2? The brief does not say.
- Is "Conjured" an exact item name or a prefix on a product line?
