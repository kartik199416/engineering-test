namespace GildedRose.Console.Updaters;

/// <summary>
/// Ordinary stock: loses quality as it ages, and twice as fast once the sell date
/// has passed.
/// </summary>
public class StandardItemUpdater : ItemUpdater
{
    /// <summary>
    /// How much quality is lost per adjustment. Virtual so that Conjured items can
    /// express themselves as a multiple of it rather than restating a number.
    /// </summary>
    protected virtual int DegradationRate => 1;

    /// <inheritdoc />
    protected override void ApplyDailyQualityChange(Item item) => AdjustQuality(item, -DegradationRate);
}
