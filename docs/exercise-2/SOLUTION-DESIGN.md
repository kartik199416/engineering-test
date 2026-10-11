# Exercise 2 — Gilded Rose: Solution Design

**Author:** Kartik Parihar
**Status:** first draft — written before any code was changed, and before review

---

## 1. The problem

The brief asks for one new behaviour:

> **"Conjured" items degrade in Quality twice as fast as normal items.**

That is a two-line change in principle. It is not a two-line change in `Program.UpdateQuality` as it stands, and that gap is the actual exercise. The kata is not testing whether I can halve a number; it is testing what I do when a trivial requirement lands on code that cannot absorb it.

So I have treated this as two pieces of work, in a deliberate order:

1. Make the existing behaviour safe to change.
2. Then add Conjured, which by that point should be small and obvious.

If step 2 is not small and obvious when I get there, step 1 was wrong.

## 2. Constraints I must respect

From the README, and non-negotiable:

| Constraint | Consequence for the design |
|---|---|
| **Do not alter the `Item` class or the `Items` property** | No `ItemType` enum on `Item`, no subclasses of `Item`, no behaviour on `Item`. The polymorphism has to live beside the item, not inside it. This rules out the most obvious OO answer. |
| `UpdateQuality` and `Items` may be made `static` | Permitted, but I have no reason to. Static makes the type harder to test and shares state across any future caller. I am declining the offer. |
| Everything must still work correctly | The existing six-item scenario in `Main` must behave exactly as it does today for the five pre-existing categories. |
| .NET 8, builds via `build.bat` | No new target framework, no new tooling beyond what the test project already references (xunit is already wired up). |

The first constraint is the interesting one. The textbook refactoring of this kata is to make `Item` abstract and push `Update()` onto subclasses. That is explicitly forbidden here, and I think deliberately so — it forces a strategy-style solution where the behaviour is selected *for* the item rather than owned *by* it.

## 3. What the legacy code actually does

Before changing anything I read `UpdateQuality` and wrote down its real behaviour, because the nested conditionals do not read the way they execute. The loop body is three sequential phases, not one decision:

1. **Pre-sell-date quality change** — everything except Aged Brie and Backstage passes loses 1 (Sulfuras excluded); Brie and passes gain 1, and passes gain a further 1 under 11 days and another under 6.
2. **`SellIn` decrement** — everything except Sulfuras.
3. **Post-sell-date correction** — if `SellIn` has gone negative, apply the change a second time: another −1 for normal items, another +1 for Brie, and passes are zeroed.

Observations that shaped the design:

- **The "twice as fast after the sell date" rule is not written anywhere as a doubling.** It emerges from the same adjustment being applied twice, in two separate phases. That structure is worth preserving, because it generalises: "Conjured degrades twice as fast" then becomes a *rate*, applied by the same two phases, rather than a new branch.
- **The 50 cap is enforced inconsistently.** It is checked before each individual increment in the first phase, but the post-sell-date Brie increment has its own separate check. Any rewrite must centralise this, or it will drift again.
- **Backstage passes are checked against `SellIn < 11` and `SellIn < 6`**, which is *before* the decrement — so these correspond to the brief's "10 days or less" and "5 days or less". Reading it as 11 and 6 after the decrement would be an off-by-one. This is the single easiest bug to introduce while refactoring, so it gets an explicit test.
- **Sulfuras is special in three separate places** (no degradation, no `SellIn` decrement, no post-date correction), expressed as three scattered negative conditions. One type should express this once.
- **`Quality` is never clamped at 0 for the pass-zeroing path** — it uses `Quality = Quality - Quality`, which is a strange way to write `0` but is correct.

## 4. Proposed design

```
IItemUpdater                     contract: void Update(Item item)
   └── ItemUpdater (abstract)    template: the shared day-end algorithm + invariants
         ├── StandardItemUpdater      degradation rate 1
         ├── ConjuredItemUpdater      degradation rate 2   ← the new requirement
         ├── AgedBrieUpdater          improves instead of degrades
         ├── BackstagePassUpdater     tiered improvement, zeroed after the concert
         └── LegendaryItemUpdater     no-op

ItemUpdaterFactory               maps an Item to its IItemUpdater
Program.UpdateQuality            foreach item -> factory.For(item).Update(item)
```

**The template method in `ItemUpdater`** encodes the three phases I found in section 3, in order:

1. apply the daily quality change for this category,
2. decrement `SellIn` (unless the category opts out),
3. if `SellIn` is now negative, apply the daily change again.

Step 3 is what makes "degrades twice as fast after the sell date" fall out automatically for every category, including Conjured — a Conjured item past its date loses 4, which is 2 × 2, without anybody writing a 4 anywhere. That is the property I most wanted from the design.

**Clamping** happens in exactly one place, in the base class, after every adjustment. No subclass can forget it.

**Why `ConjuredItemUpdater` derives the rate rather than hard-coding −2:** it differs from `StandardItemUpdater` only in its degradation rate, so it is expressed as exactly that difference. If the standard rate ever changes, Conjured stays twice it.

**Why a factory and not a dictionary in `Program`:** the matching rules are not pure equality. "Conjured Mana Cake" must match a *prefix*, and the brief implies the supplier will send more conjured products than just the cake. Name-matching policy is its own concern and deserves its own tested type.

**`Program.UpdateQuality` keeps its signature** — public, instance, `void`, no parameters — so the brief's rule about `Items` is untouched and any existing caller still compiles.

## 5. Test strategy

The HM feedback that reached me was explicit that missing unit tests alone is grounds for rejection, so this section is deliberate rather than an afterthought.

**Three layers, each answering a different question:**

1. **Characterisation tests — "did I change anything by accident?"**
   Written *against the legacy code, before refactoring*, and committed in that state so the git history proves they predate the rewrite. They pin the observable behaviour of all five original categories: ordinary degradation, the post-date doubling, the 0 floor, the 50 ceiling, Brie's improvement, Sulfuras's complete immobility, and every backstage pass tier boundary.
   These are the safety net. They are written in terms of `Item` in / `Item` out, never in terms of updaters, so they survive the refactor unmodified — which is the whole point. **If a single one of them needed editing during the refactor, the refactor changed behaviour.**

2. **Boundary tests — "are the edges right?"**
   The kata's bugs live on boundaries, so these are enumerated rather than sampled: pass tiers at `SellIn` 11, 10, 6, 5, 1, 0; quality at 0, 1, 49, 50; Sulfuras at quality 80 and negative `SellIn`.

3. **New-feature tests — "does Conjured do what was asked?"**
   Degrades at 2/day before the sell date and 4/day after; never below 0, including from an odd quality such as 1 or 3 where naive doubling would undershoot; matched by prefix so any "Conjured …" product is covered; and unaffected by the 50 cap since it only ever decreases.

**Deliberately not tested:** `Main`'s console output. It is a demo harness, not behaviour under contract.

## 6. Still open

To settle before I start cutting code:

- Whether "twice as fast" compounds with the post-sell-date doubling to give 4 per day, or is capped at 2.
- Whether the strategy-and-factory shape is worth its file count, or whether a single `switch` on the item name would carry the same weight for less. I want this argued rather than assumed, so the alternatives go in the next revision.
