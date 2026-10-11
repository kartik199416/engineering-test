using GildedRose.Console;
using Xunit;

namespace GildedRose.Tests;

/// <summary>
/// The new requirement: conjured items degrade twice as fast as normal ones.
/// </summary>
/// <remarks>
/// The brief states the rate relative to normal items but says nothing about what
/// happens once a conjured item is past its sell date. These tests take the
/// position that the two rules compound, giving 4 a day, because "once the sell by
/// date has passed, Quality degrades twice as fast" is written as a rule about all
/// items rather than about ordinary ones. That reading is recorded as assumption 1
/// in the design doc and is the one point I'd confirm with the product owner.
/// </remarks>
public class ConjuredItemTests
{
    private const string ManaCake = "Conjured Mana Cake";

    [Theory]
    [InlineData(3, 6, 2, 4)]
    [InlineData(10, 20, 9, 18)]
    [InlineData(1, 20, 0, 18)]   // last day on sale: still 2, not 4
    public void Conjured_item_degrades_twice_as_fast_as_normal(
        int sellIn, int quality, int expectedSellIn, int expectedQuality)
    {
        var item = UpdateOnce(ManaCake, sellIn, quality);

        Assert.Equal(expectedSellIn, item.SellIn);
        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData(0, 10, -1, 6)]    // sell date passes during this update
    [InlineData(-1, 10, -2, 6)]   // already expired
    [InlineData(-5, 20, -6, 16)]
    public void Conjured_item_degrades_four_times_as_fast_once_expired(
        int sellIn, int quality, int expectedSellIn, int expectedQuality)
    {
        var item = UpdateOnce(ManaCake, sellIn, quality);

        Assert.Equal(expectedSellIn, item.SellIn);
        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData(5, 0)]    // nothing to lose
    [InlineData(5, 1)]    // odd quality: 1 - 2 would be -1
    [InlineData(0, 1)]    // expired, odd quality
    [InlineData(0, 3)]    // expired: 3 - 4 would be -1
    [InlineData(-2, 2)]
    public void Conjured_quality_never_falls_below_zero(int sellIn, int quality)
    {
        var item = UpdateOnce(ManaCake, sellIn, quality);

        Assert.Equal(0, item.Quality);
    }

    [Fact]
    public void Conjured_item_degrades_steadily_over_several_days()
    {
        var item = UpdateFor(5, ManaCake, sellIn: 10, quality: 20);

        Assert.Equal(5, item.SellIn);
        Assert.Equal(10, item.Quality);
    }

    [Fact]
    public void Conjured_item_reaches_zero_and_stays_there()
    {
        var item = UpdateFor(10, ManaCake, sellIn: 3, quality: 6);

        Assert.Equal(-7, item.SellIn);
        Assert.Equal(0, item.Quality);
    }

    // ---------- which items count as conjured ----------

    [Theory]
    [InlineData("Conjured Mana Cake")]
    [InlineData("Conjured Sword of a Thousand Truths")]   // the supplier sends a product line
    [InlineData("conjured mana cake")]                    // matching is case-insensitive
    [InlineData("CONJURED MANA CAKE")]
    public void Conjured_is_matched_as_a_name_prefix(string name)
    {
        var item = UpdateOnce(name, sellIn: 5, quality: 10);

        Assert.Equal(8, item.Quality);
    }

    [Theory]
    [InlineData("Mana Cake, Conjured")]   // the word appears, but not as a prefix
    [InlineData("Unconjured Mana Cake")]
    [InlineData("+5 Dexterity Vest")]
    public void Items_that_are_not_prefixed_with_conjured_degrade_normally(string name)
    {
        var item = UpdateOnce(name, sellIn: 5, quality: 10);

        Assert.Equal(9, item.Quality);
    }

    [Fact]
    public void Conjured_wins_over_the_underlying_product_type()
    {
        var item = UpdateOnce("Conjured Aged Brie", sellIn: 5, quality: 10);

        // Brie would have improved to 11. Conjuring it does not make it immortal.
        Assert.Equal(8, item.Quality);
    }

    [Fact]
    public void Conjuring_does_not_change_legendary_items_that_were_already_safe()
    {
        var item = UpdateOnce("Sulfuras, Hand of Ragnaros", sellIn: 0, quality: 80);

        Assert.Equal(0, item.SellIn);
        Assert.Equal(80, item.Quality);
    }

    [Fact]
    public void The_shipped_sample_inventory_now_includes_a_conjured_item()
    {
        var app = new Program
        {
            Items = new List<Item>
            {
                new Item { Name = "+5 Dexterity Vest", SellIn = 10, Quality = 20 },
                new Item { Name = ManaCake, SellIn = 3, Quality = 6 }
            }
        };

        app.UpdateQuality();

        Assert.Equal(19, app.Items[0].Quality);   // ordinary: -1
        Assert.Equal(4, app.Items[1].Quality);    // conjured:  -2
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
