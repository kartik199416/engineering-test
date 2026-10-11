namespace GildedRose.Console.Updaters;

/// <summary>
/// Backstage passes: appreciate as the concert approaches, faster inside ten days
/// and faster still inside five, then become worthless the moment it is over.
/// </summary>
public sealed class BackstagePassUpdater : ItemUpdater
{
    private const int FinalWeekThreshold = 5;
    private const int LastTenDaysThreshold = 10;

    private const int FinalWeekAppreciation = 3;
    private const int LastTenDaysAppreciation = 2;
    private const int BaseAppreciation = 1;

    /// <inheritdoc />
    /// <remarks>
    /// The thresholds are read before <c>SellIn</c> is decremented, matching the
    /// brief's "10 days or less" and "5 days or less" as counted on the day the
    /// update runs. Reading them afterwards would shift every tier by one day.
    /// </remarks>
    protected override void ApplyDailyQualityChange(Item item) =>
        AdjustQuality(item, AppreciationFor(item.SellIn));

    /// <inheritdoc />
    /// <remarks>
    /// The one category that does not simply repeat its daily change past the sell
    /// date. An expired pass is not worth less - it is worth nothing.
    /// </remarks>
    protected override void ApplyExpiredQualityChange(Item item) =>
        item.Quality = MinQuality;

    private static int AppreciationFor(int sellIn) => sellIn switch
    {
        <= FinalWeekThreshold => FinalWeekAppreciation,
        <= LastTenDaysThreshold => LastTenDaysAppreciation,
        _ => BaseAppreciation
    };
}
