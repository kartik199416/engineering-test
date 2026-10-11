namespace GildedRose.Console.Updaters;

/// <summary>
/// Aged Brie: improves with age, and improves twice as fast once the sell date has
/// passed - the inverse of ordinary stock, which falls out of phase 3 running the
/// same adjustment a second time.
/// </summary>
public sealed class AgedBrieUpdater : ItemUpdater
{
    private const int ImprovementRate = 1;

    /// <inheritdoc />
    protected override void ApplyDailyQualityChange(Item item) => AdjustQuality(item, ImprovementRate);
}
