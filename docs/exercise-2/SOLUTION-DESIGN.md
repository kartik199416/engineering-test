# Exercise 2 — Gilded Rose: Solution Design

**Author:** Kartik Parihar
**Status:** written before any code was changed, then amended once during implementation (see [Changes made during implementation](#changes-made-during-implementation))

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

## 4. Options considered

### Option A — Add a `Conjured` branch to the existing conditionals

Find the two places that subtract 1 and add `if (name.StartsWith("Conjured")) subtract 2`.

- **For:** smallest possible diff; lowest immediate risk.
- **Against:** it deepens a structure that is already five levels of nesting, and it is the sixth special case bolted onto code that already cannot accommodate the fifth. The next category costs the same again. It also does nothing about the inconsistent 50 cap.
- **Rejected.** This passes the test and fails the exercise. The brief explicitly invites restructuring ("feel free to make any changes to the `UpdateQuality` method and add any new code").

### Option B — Make `Item` abstract with per-type `Update()` overrides

The canonical Gilded Rose answer.

- **For:** cleanest object model; behaviour lives with the data.
- **Against:** **forbidden by the brief.** `Item` may not be altered.
- **Rejected on the constraint**, not on merit.

### Option C — A single `switch` expression on item name

Replace the nesting with one `switch` over the name returning the quality delta.

- **For:** flat, readable, no new files; a genuine improvement over the status quo.
- **Against:** still one method that must change every time a category is added, so it keeps the Open/Closed violation. The per-category rules (pass tiers, Sulfuras's three exemptions) do not compress neatly into a single expression, so the switch arms end up with multi-line bodies and the readability gain erodes.
- **Rejected,** but it was close, and for a codebase that was genuinely only ever going to have these six categories I would have taken it. I am assuming more categories follow — the brief's framing ("we have recently signed a supplier") says this is the second of N, not the last.

### Option D — Strategy per category, selected by a factory ✅ **chosen**

One small class per category implementing a common interface; a factory maps an item to its updater; `UpdateQuality` becomes a loop that delegates.

- **For:** adding a category is a new file plus one factory line, and no existing updater is touched. Each rule is isolated and independently testable. The shared invariants (0 ≤ Quality ≤ 50, the `SellIn` decrement, the double-application after the sell date) live in one base class, so they cannot drift per category.
- **Against:** more files and more indirection than the problem strictly needs *today*. This is a real cost and I would push back on it in a review if the categories were genuinely fixed.
- **Why I accepted that cost:** the asymmetry favours it. If I am wrong about more categories arriving, I have paid ~6 small files. If I am wrong in the other direction with Option A or C, every future category pays again, on a method that is already the worst code in the repository.

## 5. Chosen design

```
IItemUpdater                     contract: void Update(Item item)
   └── ItemUpdater (abstract)    template: the shared day-end algorithm + invariants
         ├── StandardItemUpdater      degradation rate 1
         ├── ConjuredItemUpdater      degradation rate 2   ← the new requirement
         ├── AgedBrieUpdater          improves instead of degrades
         ├── BackstagePassUpdater     tiered improvement, zeroed after the concert
         └── LegendaryItemUpdater     no-op

ItemCategory                     the closed vocabulary of stock kinds
ItemCategoryClassifier           name -> ItemCategory, and the precedence order
ItemUpdaterFactory               ItemCategory -> IItemUpdater (a checked registry)
Program.UpdateQuality            foreach item -> factory.For(item).Update(item)
```

**The template method in `ItemUpdater`** encodes the three phases I found in section 3, in order:

1. apply the daily quality change for this category,
2. decrement `SellIn` (unless the category opts out),
3. if `SellIn` is now negative, apply the daily change again.

Step 3 is what makes "degrades twice as fast after the sell date" fall out automatically for every category, including Conjured — a Conjured item past its date loses 4, which is 2 × 2, without anybody writing a 4 anywhere. That is the property I most wanted from the design.

**Clamping** happens in exactly one place, in the base class, after every adjustment. No subclass can forget it.

**Why `ConjuredItemUpdater` derives the rate rather than hard-coding −2:** it differs from `StandardItemUpdater` only in its degradation rate, so it is expressed as exactly that difference. If the standard rate ever changes, Conjured stays twice it.

**Dispatch is three concerns, not one.** Recognising an item, deciding which recognition wins, and choosing the behaviour are separate questions, and the obvious implementation — a chain of `if`s in the factory — answers all three with the order of its branches. That is worth avoiding for readability, but the reason I am avoiding it is sharper than that: the chain's final `else` returns ordinary stock, so a category added without a branch would not fail. It would age at the wrong rate, silently, and surface in a stock valuation months later. An `else` that is correct for unrecognised *supplier* data is wrong for a category we wrote ourselves and forgot to wire up, and the chain cannot tell the two apart.

So the three concerns get three types:

| Type | Responsibility | Why it is separate |
|---|---|---|
| `ItemCategory` | the vocabulary | a **closed** set, which is what lets the other two be checked rather than trusted |
| `ItemCategoryClassifier` | name → category, and precedence | every string literal and comparison rule in one tested place; precedence published as data |
| `ItemUpdaterFactory` | category → updater | equality-keyed, so completeness can be proven against the enum |

Precedence is an ordered rule table, and that order is exposed as `Precedence`, so a test can assert `Precedence[0] == ItemCategory.Conjured` directly instead of inferring the ordering from the behaviour of one carefully chosen name. The fallback sits deliberately *outside* the table and is named, which makes `Classify` total by construction — no catch-all rule for anyone to reorder, and no unreachable `return` to explain away.

Completeness is checked twice. The factory validates its registry against `Enum.GetValues<ItemCategory>()` when it is constructed, so a missing updater stops the application at startup with a message naming it; a test enumerates the same values, so it stops the build first. `ForCategory` indexes rather than falling back, on purpose — the constructor has already ruled out a miss, and quietly substituting ordinary stock there would put back exactly the failure mode this removes.

**Enum declaration order is not precedence**, and nothing depends on the numeric values. Precedence is a business rule and lives with the rules; the enum is only the vocabulary. It is called out in the XML docs because it is the assumption a reader makes by default.

**Why the registry is keyed on the category and not on the name:** the matching rules are not pure equality. "Conjured Mana Cake" must match a *prefix*, and the brief implies the supplier will send more conjured products than just the cake, so a name-keyed dictionary could only work by looping its keys — a worse version of the ordered table the classifier already holds. A *category*-keyed dictionary is equality, and that is precisely what makes the completeness check above possible.

**`Program.UpdateQuality` keeps its signature** — public, instance, `void`, no parameters — so the brief's rule about `Items` is untouched and any existing caller still compiles.

## 6. Trade-offs I am accepting

| Decision | What I gain | What it costs | Why the trade is right |
|---|---|---|---|
| Strategy + factory over a `switch` | Open/Closed; isolated, testable rules | 8 new files, more indirection | Cost is paid once; the alternative is paid per category |
| Template method in a base class | Invariants cannot drift; the post-date doubling is automatic | Inheritance, which is harder to reason about than composition | The shared algorithm is genuinely fixed across categories; only the per-day delta varies |
| Name matching in a classifier | Prefix rules are testable; precedence is assertable data; `Program` stays trivial | String matching is still inherently fragile, and it is one more type | The constraint forbids typing `Item` properly; this is the least-bad place for the fragility to live, and confining it to one type is what makes replacing it later a one-file change |
| A closed enum plus a checked registry | A forgotten category fails the build instead of ageing stock wrongly in silence | An enum member and a registry entry to keep in step | They cannot drift — keeping them in step is exactly what the construction check and the exhaustiveness test enforce |
| Characterisation tests written first | Proof the refactor preserved behaviour | Time spent testing code I intended to delete | Without them "it still works correctly" is an assertion, not a fact |
| Not making anything `static` | Testability; no shared state | Declines an explicit offer in the brief | The offer exists to unblock people; I am not blocked |

## 7. Test strategy

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

**Assumption being tested, not assumed:** that "twice as fast" compounds with the post-date rule to give 4, rather than being capped at 2. The brief does not say outright. I am taking the compounding reading because the existing rule is "once the sell by date has passed, Quality degrades twice as fast" and it applies to *all* items, so Conjured's doubling multiplies with it rather than replacing it. This is recorded as an assumption in section 8 and is the one decision I would raise with the product owner.

## 8. Assumptions

Recorded because the brief is ambiguous in these places, and in a real team each of these is a question rather than a guess:

1. **Conjured + past sell date = 4 per day.** The two doublings compound. Reasoning above.
2. **"Conjured" is a name prefix, not an exact name.** The sample data has "Conjured Mana Cake"; a supplier sends a product line, not one SKU. Matching is case-insensitive and ordinal.
3. **Sulfuras's quality of 80 is data, not something the code should assert.** The brief says it "is 80 and never alters". I enforce *never alters*; I do not force it to 80, because doing so would silently rewrite an item the goblin owns. A legendary item arriving with quality 80 keeps it; the updater simply never touches quality or `SellIn`.
4. **The 50 cap does not apply to Sulfuras,** which is why it is clamped in the base class only along paths the legendary updater never takes.
5. **Existing item names are stable strings.** With `Item` off-limits there is no type to match on, so names are the only discriminator available.
6. **One `UpdateQuality()` call is one day.** Never stated, but it is the only reading consistent with "at the end of each day".
