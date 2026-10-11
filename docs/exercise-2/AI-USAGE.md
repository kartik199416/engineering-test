# Exercise 2 — AI usage disclosure

The README asks for this, so here it is plainly, with the line drawn where it
actually falls rather than where it flatters me.

**Short version:** the analysis, the design, the implementation and the debugging
are mine. AI reviewed my design and argued back, helped me shape the task list and
the test data, widened test coverage over scenarios I handed it, re-reviewed the
finished code, confirmed the build was green, and ran all the git and GitHub
mechanics — commits, branch, push, pull request.

---

## 1. Who did what

| Area | Me | AI |
|---|---|---|
| Reading the legacy `UpdateQuality` and working out what it really does | ✅ all of it | — |
| Spotting the gaps and pitfalls (tier off-by-one, inconsistent 50 cap, Sulfuras's three exemptions) | ✅ all of it | — |
| The solution design — options, choice, trade-offs | ✅ authored | reviewed and pushed back |
| Task breakdown and ordering | ✅ decided | assisted — drafted the table, I set the order |
| Test data and scenario planning | ✅ scenarios and pitfalls named by me | assisted — turned them into concrete cases and data |
| Writing the production code | ✅ all of it | — |
| Debugging when it did not behave | ✅ all of it | — |
| Post-implementation code review | — | ✅ reviewed locally, I accepted or rejected each point |
| Widening test coverage over the scenarios I listed | — | ✅ wrote the extra cases |
| XML documentation comments | reviewed and corrected | ✅ drafted |
| These markdown documents | ✅ content, structure and corrections | ✅ wrote them up from my notes |
| Build and suite verification | ✅ ran it myself too | ✅ final green pass confirmed |
| Commits, branch, push, pull request | — | ✅ all of it, to my instructions |

## 2. Where AI was used, in detail

**Reviewing the design.** I wrote the first draft on my own — the three-phase
reading of the legacy loop, the strategy-plus-factory shape, and why `Item` being
off-limits forces behaviour to sit beside the item rather than on it. I then put
that draft up for review and asked it to attack the weak points. The useful
challenge was *"you have not argued why a single `switch` on the name is
insufficient"* — a fair hit, because I had dismissed it in my head without
writing the reasoning down. Section 4 of the design, where options A–D are weighed
and three are rejected in the open, exists because of that review. The review
sharpened the argument; it did not change the decision.

**Helping plan the tasks.** I decided the order — characterise, then extract, then
dispatch, then feature, because nothing gets refactored before there are tests
that would catch it breaking. AI turned that into the table in `TASKS.md`, with
the "why it comes here" column, and suggested splitting what I had as one
refactoring step into two (extract the abstraction, then move the categories
across). I took that split; it makes the behaviour-preserving step easier to
verify on its own.

**Planning the test data.** I named the scenarios and the pitfalls I wanted
covered — pass tiers either side of 11 and 6, quality at 0, 1, 49 and 50, Sulfuras
at 80 with a negative `SellIn`, odd quality values where naive doubling would
undershoot zero, and names that *look* conjured but are not. AI turned that list
into the concrete `[InlineData]` sets and filled in gaps in the enumeration.

**Reviewing the finished code and widening coverage.** Once the implementation
worked, I had it review the code locally. Two things came out of that worth
keeping: it flagged that `"Conjured Aged Brie"` has no defined answer and that
whichever way I resolved it should be pinned by a test, and it pointed out that
my negative name cases covered a wrong prefix but not a trailing match. I decided
Conjured wins — conjuring is something done *to* a product — and the tests for
both now exist.

**XML documentation comments.** Drafted across the updater classes, then read and
corrected by me. The `<remarks>` explaining *why* clamping sits inside
`AdjustQuality` is my reasoning in its words.

**Writing up these documents.** The content, the structure and the arguments are
mine; the prose is AI's, from my notes, corrected by me afterwards. Calling the
markdown "mine" would be the dishonest part of this disclosure, so I am not.

**Git and GitHub mechanics.** Every commit message, the branch, the pushes and
this pull request were produced by AI following my instructions on what each
commit should contain and where the boundaries fall. The sequence of commits is
the sequence I asked for.

**The final build check.** I ran `build.bat` myself as I went; the last
end-to-end verification — clean build, 115 tests green, zero warnings — was run and
confirmed by AI.

## 3. Where AI was not used

These are mine, and I can defend each one without notes:

1. **The three-phase reading of the legacy loop** — that "degrades twice as fast
   after the sell date" is not a multiplication anywhere in the original, but the
   same adjustment applied a second time. The whole design rests on this, and it
   is why Conjured's 4-per-day falls out with no `4` written anywhere.
2. **Clamping inside `AdjustQuality` rather than at the end of `Update`.** The
   tidier version is wrong: it drags Sulfuras from 80 down to 50, and every
   non-legendary test still passes while it does so.
3. **The backstage tiers keying off the pre-decrement `SellIn`.** The legacy code
   checks `< 11` and `< 6` before decrementing, so those are the brief's "10 days
   or less" and "5 days or less". Reading them after the decrement is the easiest
   bug in this kata.
4. **Dispatch order, Conjured first.** So `"Conjured Aged Brie"` degrades rather
   than improves.
5. **Treating Sulfuras's 80 as data rather than an assertion.** The updater never
   touches it; it does not force it to 80.
6. **The one deliberate behaviour change** — out-of-range quality is now clamped
   back into range, where the legacy guard let brie that arrived at 60 stay at 60
   forever. Written up in the design and asserted in the tests rather than left to
   be discovered.
7. **The diagnosis that shaped the dispatch design** (design section 5) — that an
   `if`-chain factory would hide a *silent* failure, not merely an untidy one,
   because an unwired category is indistinguishable from unrecognised supplier
   data. The answer — a closed enum, precedence as data, and a registry checked
   against that enum — is mine. AI wrote the XML documentation on those types and
   suggested additional classifier test cases once the shape existed.

## 4. Suggestions I rejected

Recorded because what you turn down says more than what you accept:

- **A `Dictionary<string, IItemUpdater>` for dispatch.** Rejected: the matching
  rules are not equality. Three of the five are prefixes, and a name-keyed
  dictionary cannot express that without a lookup loop. The prefix rules belong
  in an ordered table in `ItemCategoryClassifier`. What a dictionary *is* right
  for is the second half of dispatch — `ItemCategory` to updater — because a
  closed enum is equality and makes the registry's completeness provable. One
  suggestion, right about the data structure and wrong about the key.
- **Making `Update` virtual "for flexibility".** Rejected: the three-phase order
  is the invariant the design rests on. Subclasses vary the steps, never the
  sequence.
- **Collapsing the characterisation tests into the boundary tests.** Rejected:
  they answer different questions. The characterisation tests are only meaningful
  while they remain untouched since before the refactor, and merging them would
  destroy exactly that property.

## 5. Verification

I did not take anything on trust:

- The characterisation tests were written against the legacy code and committed
  before the refactor started. They have **one commit in their history** — check
  `git log` on `CharacterisationTests.cs`. If the refactor had changed behaviour,
  editing them would have been the only way to stay green.
- `git diff` against the fork point shows the `Item` class and the `Items`
  property appear only as unchanged context.
- `build.bat` runs clean: 115 tests, zero warnings, zero errors.
- The 38 characterisation tests have **not been edited since the commit that
  introduced them**, through every refactoring commit that followed. A refactor
  that needs its tests adjusted is not a refactor.
- I walked the final diff myself, file by file, before it was pushed.

## 6. Owning it

I can explain every line of this solution, the alternatives I rejected and why,
and the one assumption I would take to a product owner before shipping —
whether Conjured past its sell date should lose 4 a day, as I have it, or stay
capped at 2. That one is in the design as assumption 1 and it is a genuine
question, not a rhetorical one.
