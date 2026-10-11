# Exercise 2 — Execution flow

How the system actually runs, before and after the refactor. Written alongside [SOLUTION-DESIGN.md](SOLUTION-DESIGN.md) as the intended flow, and verified against the finished code at the end of the exercise.

This document exists because the design doc argues *why*; this one shows *what happens, in order*, which is the faster way to review whether the behaviour was preserved.

---

## 1. Entry point

```
build.bat
   └── dotnet build  →  dotnet test
                            └── GildedRose.Tests  (characterisation + boundary + Conjured)

GildedRose.Console.exe
   └── Program.Main
         ├── prints "OMGHAI!"
         ├── builds the six-item inventory  (Items — NOT altered, per the brief)
         ├── app.UpdateQuality()            ← one call == one day
         └── Console.ReadKey()
```

`Main`, `Items` and `Item` are untouched. The only thing that changed inside `Program` is the body of `UpdateQuality`.

---

## 2. Before — legacy `UpdateQuality`

One method, one loop, five levels of nesting, with the category decisions interleaved through three phases rather than stated once.

```
for each item:

    ── phase 1: quality change ──────────────────────────────
    if name is NOT Brie and NOT Backstage:
        if quality > 0:
            if name is NOT Sulfuras:            ← Sulfuras exemption #1
                quality -= 1
    else:
        if quality < 50:                        ← cap check, inline
            quality += 1
            if name is Backstage:
                if sellIn < 11 and quality < 50: quality += 1   ← cap check again
                if sellIn <  6 and quality < 50: quality += 1   ← and again

    ── phase 2: sellIn ──────────────────────────────────────
    if name is NOT Sulfuras:                    ← Sulfuras exemption #2
        sellIn -= 1

    ── phase 3: past sell date correction ───────────────────
    if sellIn < 0:
        if name is NOT Brie:
            if name is NOT Backstage:
                if quality > 0:
                    if name is NOT Sulfuras:    ← Sulfuras exemption #3
                        quality -= 1            ← the "twice as fast" rule, implicitly
            else:
                quality -= quality              ← pass zeroed after the concert
        else:
            if quality < 50: quality += 1       ← Brie's cap check, a fourth copy
```

Three things this flow makes obvious, and they drove the design:

1. **"Twice as fast after the sell date" is never written as a doubling.** It is the same −1 happening again in phase 3. Preserve that structure and the rule generalises for free.
2. **The 50 cap is checked in four separate places.** Four chances to drift.
3. **Sulfuras is excluded three times, by three separate negative conditions**, none of which are near each other.

---

## 3. After — delegation

`UpdateQuality` becomes the loop and nothing else:

```
Program.UpdateQuality()
    foreach item in Items:
        factory.For(item).Update(item)
```

### Dispatch

Dispatch is two steps, deliberately: recognise the item, then resolve the behaviour.

```
ItemUpdaterFactory.For(item)
    │
    ├─ 1. ItemCategoryClassifier.Classify(item.Name)
    │       first matching rule wins, rules in precedence order:
    │
    │       starts with "Conjured"         → ItemCategory.Conjured
    │       starts with "Sulfuras"         → ItemCategory.Legendary
    │       equals     "Aged Brie"         → ItemCategory.AgedBrie
    │       starts with "Backstage passes" → ItemCategory.BackstagePass
    │       no rule matched                → ItemCategory.Standard   (the fallback)
    │
    └─ 2. ItemUpdaterFactory.ForCategory(category)
            Conjured      → ConjuredItemUpdater   (rate 2)
            Legendary     → LegendaryItemUpdater  (no-op)
            AgedBrie      → AgedBrieUpdater
            BackstagePass → BackstagePassUpdater
            Standard      → StandardItemUpdater   (rate 1)
```

Order matters: Conjured is tested first so that a hypothetical "Conjured Aged Brie" resolves as conjured rather than as brie. That ordering is a decision, not an accident — it is a position in an ordered rule table, published as `ItemCategoryClassifier.Precedence`, and a test asserts `Precedence[0]` directly rather than inferring it from one example's behaviour.

The fallback deliberately sits outside the rule table. That makes `Classify` total by construction, and it keeps "unrecognised supplier data ages normally" separate from "a category we forgot to wire up" — the second is a bug, and step 2 refuses to disguise it. `ForCategory` indexes the registry rather than falling back, and the registry is checked against every `ItemCategory` member when the factory is constructed.

### The shared day-end algorithm

Every updater runs the same three phases — the same three the legacy code had, now stated once in `ItemUpdater`:

```
ItemUpdater.Update(item)                     ← template method, identical for all categories
    │
    ├─ 1. ApplyDailyQualityChange(item)      ← the only part subclasses vary
    │        └── then Clamp(0..50)           ← single enforcement point
    │
    ├─ 2. if AdjustsSellIn:  item.SellIn -= 1
    │
    └─ 3. if item.SellIn < 0:
              ApplyDailyQualityChange(item)  ← the doubling, applied generically
                 └── then Clamp(0..50)
```

Subclasses override only the small pieces:

| Updater | `ApplyDailyQualityChange` | `AdjustsSellIn` | Notes |
|---|---|---|---|
| `StandardItemUpdater` | `quality -= 1` | yes | |
| `ConjuredItemUpdater` | `quality -= 2` | yes | expressed as 2 × the standard rate |
| `AgedBrieUpdater` | `quality += 1` | yes | improves; phase 3 doubles the improvement |
| `BackstagePassUpdater` | `+1`, `+2` ≤10 days, `+3` ≤5 days; `0` once past | yes | overrides phase 3 — zeroed, not doubled |
| `LegendaryItemUpdater` | nothing | **no** | all three Sulfuras exemptions, in one place |

---

## 4. Worked traces

The six items from `Main`, one day each. These doubled as the expected values in the characterisation tests.

| Item | SellIn → | Quality → | Which phases fired |
|---|---|---|---|
| +5 Dexterity Vest (10, 20) | 9 | 19 | 1 (−1), 2 |
| Aged Brie (2, 0) | 1 | 1 | 1 (+1), 2 |
| Elixir of the Mongoose (5, 7) | 4 | 6 | 1 (−1), 2 |
| Sulfuras (0, 80) | **0** | **80** | none — no-op |
| Backstage passes (15, 20) | 14 | 21 | 1 (+1, no tier), 2 |
| Conjured Mana Cake (3, 6) | 2 | **4** | 1 (−2), 2 |

And the cases where phase 3 fires:

| Scenario | Before | After one day | Why |
|---|---|---|---|
| Standard, expired | (0, 10) | (−1, 8) | −1 in phase 1, −1 again in phase 3 |
| **Conjured, expired** | (0, 10) | (−1, **6**) | −2 in phase 1, −2 again in phase 3 → 4/day |
| Conjured, quality 1 | (5, 1) | (4, **0**) | clamped at the floor, not −1 |
| Conjured, expired, quality 3 | (0, 3) | (−1, **0**) | would be −4; clamped |
| Aged Brie, expired | (−1, 10) | (−2, 12) | improvement doubles too |
| Backstage, concert passed | (0, 20) | (−1, **0**) | phase 3 overridden: zeroed |
| Backstage at 50, ≤5 days | (3, 50) | (2, **50**) | +3 then clamped |

The Conjured-expired row is the one I care about most: nobody wrote a `4` anywhere. It falls out of rate 2 × phase 3. That is the evidence the abstraction is carrying its weight.

---

## 5. Adding the next category

The practical test of whether this design was worth its extra files — what a future developer has to do when the innkeeper signs another supplier:

1. Add one member to `ItemCategory`.
2. Add one file implementing `ApplyDailyQualityChange`.
3. Add one rule to `ItemCategoryClassifier.Rules`, at the position its precedence demands.
4. Add one entry to the `ItemUpdaterFactory` registry.
5. Add that category's tests.

Steps 1 and 4 cannot be forgotten independently: the factory checks its registry against the enum when it is constructed, and a test enumerates the enum, so adding the member without the updater fails the build rather than quietly ageing the new stock as ordinary.

No existing updater is opened, and `UpdateQuality` is never touched again. Compare with the legacy flow in section 2, where a sixth category meant edits in three separate phases of the same method and a fifth copy of the cap check.
