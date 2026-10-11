using GildedRose.Console;
using Xunit;

namespace GildedRose.Tests;

/// <summary>
/// The quality bounds, including data that arrives already outside them.
/// </summary>
/// <remarks>
/// The legacy code enforced 0..50 by guarding each individual increment, which
/// meant an item that was already out of range stayed out of range - brie at 60
/// simply sat at 60 forever, because the guard said "only add if below 50" rather
/// than "never be above 50". The refactored code clamps instead, so bad data is
/// pulled back into range on the next update.
///
/// That is a deliberate change and it is recorded in section 9 of the design doc.
/// Nothing in the sample data or the brief produces such an item, so no
/// characterisation test covers it; these tests exist so that the new behaviour is
/// stated somewhere rather than discovered.
/// </remarks>
public class QualityBoundaryTests
{
    [Theory]
    [InlineData("+5 Dexterity Vest", 60)]
    [InlineData("Aged Brie", 60)]
    [InlineData("Backstage passes to a TAFKAL80ETC concert", 60)]
    [InlineData("Conjured Mana Cake", 60)]
    public void Quality_above_the_ceiling_is_brought_back_into_range(string name, int quality)
    {
        var item = UpdateOnce(name, sellIn: 5, quality: quality);

        Assert.Equal(50, item.Quality);
    }

    [Theory]
    [InlineData("+5 Dexterity Vest", -5)]
    [InlineData("Conjured Mana Cake", -5)]
    public void Quality_below_the_floor_is_brought_back_into_range(string name, int quality)
    {
        var item = UpdateOnce(name, sellIn: 5, quality: quality);

        Assert.Equal(0, item.Quality);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(90)]
    [InlineData(0)]
    public void Legendary_quality_is_left_exactly_as_the_data_supplies_it(int quality)
    {
        // The rule is "never alters", not "is always 80". Clamping legendary items
        // into 0..50 would be the single most damaging thing this refactor could do
        // and it is the reason the clamp sits inside the adjustment, not after it.
        var item = UpdateFor(100, "Sulfuras, Hand of Ragnaros", sellIn: 0, quality: quality);

        Assert.Equal(quality, item.Quality);
    }

    [Theory]
    [InlineData("+5 Dexterity Vest", 0)]
    [InlineData("Conjured Mana Cake", 0)]
    [InlineData("Aged Brie", 50)]
    [InlineData("Backstage passes to a TAFKAL80ETC concert", 0)]   // climbs to 50, then the concert passes
    public void Quality_settles_at_its_bound_and_stays_there(string name, int expected)
    {
        var item = UpdateFor(60, name, sellIn: 30, quality: 25);

        Assert.Equal(expected, item.Quality);
    }

    private static Item UpdateOnce(string name, int sellIn, int quality) =>
        UpdateFor(1, name, sellIn, quality);

    private static Item UpdateFor(int days, string name, int sellIn, int quality)
    {
        var item = new Item { Name = name, SellIn = sellIn, Quality = quality };
        var app = new Program { Items = new List<Item> { item } };

        for (var day = 0; day < days; day++)
        {
            app.UpdateQuality();
        }

        return item;
    }
}
