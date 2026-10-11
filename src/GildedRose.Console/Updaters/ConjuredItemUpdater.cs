namespace GildedRose.Console.Updaters;

/// <summary>
/// Conjured stock: degrades twice as fast as ordinary stock.
/// </summary>
/// <remarks>
/// <para>
/// The rate is derived from <see cref="StandardItemUpdater"/> rather than written
/// as 2, because the requirement is relative - "twice as fast as normal items". If
/// normal ever stops meaning 1, this follows it without being edited.
/// </para>
/// <para>
/// Nothing here mentions the sell date. A conjured item past its date loses 4 a
/// day because the inherited algorithm applies the rate twice, which is the same
/// reason an ordinary item loses 2. The number 4 appears nowhere in the code -
/// only in the tests, where it belongs.
/// </para>
/// </remarks>
public sealed class ConjuredItemUpdater : StandardItemUpdater
{
    private const int TwiceAsFast = 2;

    /// <inheritdoc />
    protected override int DegradationRate => base.DegradationRate * TwiceAsFast;
}
