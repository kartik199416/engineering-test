namespace GildedRose.Console.Updaters;

/// <summary>
/// How a single item changes at the end of a day.
/// </summary>
/// <remarks>
/// The behaviour has to live beside the item rather than on it, because the brief
/// forbids altering <see cref="Item"/>. This interface is that seam: one item, one
/// day, nothing returned.
/// </remarks>
public interface IItemUpdater
{
    /// <summary>
    /// Advances <paramref name="item"/> by one day, in place.
    /// </summary>
    void Update(Item item);
}
