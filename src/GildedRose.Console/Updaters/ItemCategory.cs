namespace GildedRose.Console.Updaters;

/// <summary>
/// The kinds of stock the inventory knows how to age.
/// </summary>
/// <remarks>
/// <para>
/// This exists so that "what kind of thing is this" stops being a string comparison
/// performed at the point of use. The set is closed, which is what lets the rest of
/// the design be checked rather than trusted: <see cref="ItemUpdaterFactory"/> can
/// prove at construction that every member has an updater, and a test can enumerate
/// the members rather than re-list them by hand and drift.
/// </para>
/// <para>
/// <b>Declaration order is not precedence.</b> It is tempting to read this list as
/// the matching order - it is not, and nothing here depends on the underlying
/// numeric values. Precedence is data, held in
/// <see cref="ItemCategoryClassifier.Precedence"/>, because it is a business rule
/// that can change without the vocabulary changing.
/// </para>
/// </remarks>
public enum ItemCategory
{
    /// <summary>
    /// Ordinary stock. Degrades by one a day, twice that once the sell date passes.
    /// Also the fallback for anything unrecognised, which is the safe default - an
    /// unknown item ageing normally is a smaller error than one that never ages.
    /// </summary>
    Standard,

    /// <summary>Appreciates with age instead of degrading.</summary>
    AgedBrie,

    /// <summary>
    /// Appreciates as the concert approaches, faster near the date, worthless after it.
    /// </summary>
    BackstagePass,

    /// <summary>Never has to be sold and never degrades.</summary>
    Legendary,

    /// <summary>Degrades at twice the rate of ordinary stock.</summary>
    Conjured
}
