namespace GildedRose.Console.Updaters;

/// <summary>
/// Legendary stock such as Sulfuras: never has to be sold and never degrades.
/// </summary>
/// <remarks>
/// In the original code this was three separate negative conditions scattered
/// through the method - don't decrement quality, don't decrement the sell date,
/// don't decrement quality again past it. All three say the same thing, so here
/// they are one thing: nothing happens.
///
/// The quality of 80 is left exactly as the data supplies it. The rule is "never
/// alters", not "is always 80", so forcing the value would be inventing a rule
/// the brief does not state.
/// </remarks>
public sealed class LegendaryItemUpdater : ItemUpdater
{
    /// <inheritdoc />
    protected override bool AdjustsSellIn => false;

    /// <inheritdoc />
    protected override void ApplyDailyQualityChange(Item item)
    {
        // Intentionally empty. Legendary items do not change.
    }
}
