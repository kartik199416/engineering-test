namespace GildedRose.Console.Updaters;

/// <summary>
/// The day-end algorithm every category shares, with the category-specific parts
/// left open.
/// </summary>
/// <remarks>
/// <para>
/// The three phases below are the three the legacy <c>UpdateQuality</c> already
/// had, just stated once instead of being interleaved with the branching:
/// </para>
/// <list type="number">
///   <item><description>adjust quality for the day,</description></item>
///   <item><description>advance the sell-by date,</description></item>
///   <item><description>if the date has passed, adjust quality a second time.</description></item>
/// </list>
/// <para>
/// Phase 3 is why "degrades twice as fast once the sell date has passed" is not
/// written as a doubling anywhere: it is the same adjustment, applied again. Any
/// category that defines a rate gets the post-date behaviour for free.
/// </para>
/// <para>
/// Clamping happens inside the adjustment, not at the end of <see cref="Update"/>.
/// That is deliberate - a blanket clamp on the way out would drag Sulfuras down
/// from 80 to 50, since the 0..50 range is a rule about items that change, not
/// about every item.
/// </para>
/// </remarks>
public abstract class ItemUpdater : IItemUpdater
{
    /// <summary>Lowest quality an item that degrades can reach.</summary>
    public const int MinQuality = 0;

    /// <summary>Highest quality an item that improves can reach.</summary>
    public const int MaxQuality = 50;

    /// <summary>
    /// Whether this category counts down to a sell-by date at all. Legendary items
    /// do not.
    /// </summary>
    protected virtual bool AdjustsSellIn => true;

    /// <inheritdoc />
    public void Update(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        ApplyDailyQualityChange(item);

        if (AdjustsSellIn)
        {
            item.SellIn--;
        }

        if (item.SellIn < 0)
        {
            ApplyExpiredQualityChange(item);
        }
    }

    /// <summary>
    /// The one phase every category defines for itself: what a normal day does to
    /// quality.
    /// </summary>
    protected abstract void ApplyDailyQualityChange(Item item);

    /// <summary>
    /// What an additional day past the sell date does. By default it repeats the
    /// daily change, which is what "twice as fast" means. Backstage passes are the
    /// exception and override it.
    /// </summary>
    protected virtual void ApplyExpiredQualityChange(Item item) => ApplyDailyQualityChange(item);

    /// <summary>
    /// Moves quality by <paramref name="delta"/>, keeping it inside 0..50.
    /// </summary>
    protected static void AdjustQuality(Item item, int delta)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.Quality = Math.Clamp(item.Quality + delta, MinQuality, MaxQuality);
    }
}
