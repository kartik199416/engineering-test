using System.Collections.Frozen;

namespace GildedRose.Console.Updaters;

/// <summary>
/// Resolves the updater responsible for an item.
/// </summary>
/// <remarks>
/// <para>
/// Dispatch is three questions, not one: what is this item, which answer wins when
/// more than one could apply, and what behaviour does that answer imply. The first
/// two belong to <see cref="ItemCategoryClassifier"/>. What is left here is the
/// third - the registry mapping a category to the behaviour that implements it.
/// </para>
/// <para>
/// Keeping them apart is worth more than a tidier method. The obvious alternative,
/// a chain of <c>if</c> statements ending in a default, cannot distinguish a name
/// we do not recognise from a category we wrote and forgot to wire up: both fall
/// through to ordinary stock and age at the wrong rate in silence - the sort of
/// defect found in a stock valuation months later rather than in a build. Here the
/// registry is checked against <see cref="ItemCategory"/> when the factory is
/// constructed, so that mistake stops the application at startup with a message
/// naming what is missing, and stops the build before that, because a test
/// enumerates the same enum.
/// </para>
/// <para>
/// Updaters are created once and shared. They hold no per-item state, so there is
/// nothing to isolate between items, and the inventory loop asks for one on every
/// item on every day.
/// </para>
/// </remarks>
public sealed class ItemUpdaterFactory
{
    private readonly FrozenDictionary<ItemCategory, IItemUpdater> _updaters;

    /// <summary>
    /// Builds the registry and verifies that it covers every known category.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A member of <see cref="ItemCategory"/> has no updater registered.
    /// </exception>
    public ItemUpdaterFactory()
    {
        var updaters = new Dictionary<ItemCategory, IItemUpdater>
        {
            [ItemCategory.Standard] = new StandardItemUpdater(),
            [ItemCategory.AgedBrie] = new AgedBrieUpdater(),
            [ItemCategory.BackstagePass] = new BackstagePassUpdater(),
            [ItemCategory.Legendary] = new LegendaryItemUpdater(),
        };

        var unregistered = Enum.GetValues<ItemCategory>()
            .Where(category => !updaters.ContainsKey(category))
            .ToArray();

        if (unregistered.Length > 0)
        {
            throw new InvalidOperationException(
                $"No {nameof(IItemUpdater)} is registered for: {string.Join(", ", unregistered)}. " +
                "Every member of ItemCategory needs one, otherwise the items it covers " +
                "would be aged as ordinary stock without anything reporting it.");
        }

        // Frozen rather than Dictionary to say that the registry is fixed once built;
        // the lookup speed is incidental at this size.
        _updaters = updaters.ToFrozenDictionary();
    }

    /// <summary>
    /// Returns the updater responsible for <paramref name="item"/>.
    /// </summary>
    public IItemUpdater For(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return ForCategory(ItemCategoryClassifier.Classify(item.Name));
    }

    /// <summary>
    /// Returns the updater registered for <paramref name="category"/>.
    /// </summary>
    /// <remarks>
    /// Indexed rather than looked up with a fallback, on purpose. A category with no
    /// updater is a programming error, and the constructor has already ruled it out;
    /// quietly substituting ordinary stock here would reintroduce exactly the silent
    /// failure this design is built to avoid.
    /// </remarks>
    public IItemUpdater ForCategory(ItemCategory category) => _updaters[category];
}
