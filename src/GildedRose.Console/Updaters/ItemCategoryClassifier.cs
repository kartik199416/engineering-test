namespace GildedRose.Console.Updaters;

/// <summary>
/// Decides which <see cref="ItemCategory"/> an item belongs to, from the only thing
/// an item tells us about itself: its name.
/// </summary>
/// <remarks>
/// <para>
/// Matching on names is fragile. That fragility is not avoidable while
/// <see cref="Item"/> cannot be altered - the name is the entire contract - so the
/// goal is to confine it. Every string literal and every comparison rule in the
/// solution lives in this one type, and nothing downstream of it ever sees a name
/// again. Replacing name matching later with a supplier-supplied category code is
/// then a change to this file and no other.
/// </para>
/// <para>
/// Prefixes rather than exact names for two of the three rules: "Sulfuras, Hand of
/// Ragnaros" and "Backstage passes to a TAFKAL80ETC concert" are instances of
/// product lines, not one-off SKUs, and a second concert should not need a code
/// change. "Aged Brie" is matched exactly, because it names a product rather than a
/// line. Comparisons are ordinal and case-insensitive.
/// </para>
/// <para>
/// Precedence is the order of <see cref="Rules"/> - data, not control flow - and is
/// published through <see cref="Precedence"/> so that it can be asserted directly
/// rather than inferred from the behaviour of a carefully chosen example.
/// </para>
/// </remarks>
public static class ItemCategoryClassifier
{
    private const string AgedBrieName = "Aged Brie";
    private const string BackstagePassPrefix = "Backstage passes";
    private const string LegendaryPrefix = "Sulfuras";

    /// <summary>
    /// The category used when no rule matches. Named rather than implied, and
    /// deliberately outside <see cref="Rules"/> so that <see cref="Classify"/> is
    /// total by construction instead of by a catch-all rule somebody could reorder.
    /// </summary>
    private const ItemCategory Fallback = ItemCategory.Standard;

    /// <summary>
    /// The matching rules, in precedence order. The first match wins.
    /// </summary>
    /// <remarks>
    /// No name in today's catalogue matches two rules, so the order is not yet
    /// load-bearing. This is an ordered list rather than a set because that will not
    /// stay true: the moment the catalogue gains a modifier - something applied
    /// <i>to</i> a product rather than naming one - it has to be decided whether the
    /// modifier or the product wins. That decision belongs in a position in this
    /// table, which a reviewer can disagree with by moving one line, rather than in
    /// the order somebody happened to type a chain of conditionals.
    /// </remarks>
    private static readonly ItemCategoryRule[] Rules =
    [
        new(ItemCategory.Legendary, name => StartsWith(name, LegendaryPrefix)),
        new(ItemCategory.AgedBrie, name => MatchesExactly(name, AgedBrieName)),
        new(ItemCategory.BackstagePass, name => StartsWith(name, BackstagePassPrefix))
    ];

    /// <summary>
    /// The categories in the order they are tested, ending with the fallback.
    /// </summary>
    public static IReadOnlyList<ItemCategory> Precedence { get; } =
        [.. Rules.Select(rule => rule.Category), Fallback];

    /// <summary>
    /// Returns the category for <paramref name="name"/>, or
    /// <see cref="ItemCategory.Standard"/> if nothing matches.
    /// </summary>
    /// <remarks>
    /// A null or blank name is ordinary stock rather than an error. Classification
    /// is a reading of data we did not author; refusing to age an item because its
    /// name is missing would turn a data-quality problem into an outage.
    /// </remarks>
    public static ItemCategory Classify(string? name)
    {
        var candidate = name ?? string.Empty;

        foreach (var rule in Rules)
        {
            if (rule.IsMatch(candidate))
            {
                return rule.Category;
            }
        }

        return Fallback;
    }

    private static bool MatchesExactly(string name, string expected) =>
        name.Equals(expected, StringComparison.OrdinalIgnoreCase);

    private static bool StartsWith(string name, string prefix) =>
        name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// One rule: the category, and the test that recognises it.
    /// </summary>
    private sealed record ItemCategoryRule(ItemCategory Category, Func<string, bool> IsMatch);
}
