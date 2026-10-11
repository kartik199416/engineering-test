using GildedRose.Console;
using Xunit;

namespace GildedRose.Tests;

/// <summary>
/// These pin down what <see cref="Program.UpdateQuality"/> does today, before any
/// of it is restructured. They are written against the legacy implementation and
/// are expected to survive the refactor untouched - if one of them has to be
/// edited to get the refactored code green, the refactor changed behaviour and
/// that is the bug, not the test.
///
/// Conjured items are deliberately absent. Today they are treated as ordinary
/// items, and that is precisely the behaviour the new requirement changes, so
/// pinning it here would only guarantee a test I'd have to delete later.
/// </summary>
public class CharacterisationTests
{
    private const string Brie = "Aged Brie";
    private const string Pass = "Backstage passes to a TAFKAL80ETC concert";
    private const string Sulfuras = "Sulfuras, Hand of Ragnaros";
    private const string Vest = "+5 Dexterity Vest";
    private const string Elixir = "Elixir of the Mongoose";

    // ---------- ordinary items ----------

    [Theory]
    // sellIn, quality, expected sellIn, expected quality
    [InlineData(10, 20, 9, 19)]   // comfortably before the sell date
    [InlineData(1, 20, 0, 19)]    // last day on sale, still single rate
    [InlineData(0, 10, -1, 8)]    // sell date passes during this update: double rate
    [InlineData(-1, 10, -2, 8)]   // already expired, stays at double rate
    public void Ordinary_item_degrades_by_one_before_the_sell_date_and_two_after(
        int sellIn, int quality, int expectedSellIn, int expectedQuality)
    {
        var item = UpdateOnce(Vest, sellIn, quality);

        Assert.Equal(expectedSellIn, item.SellIn);
        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData(5, 0, 0)]     // nothing left to lose
    [InlineData(5, 1, 0)]     // single decrement lands exactly on the floor
    [InlineData(0, 1, 0)]     // double rate must not overshoot into negatives
    [InlineData(-3, 1, 0)]    // same, well past the sell date
    public void Quality_never_falls_below_zero(int sellIn, int quality, int expectedQuality)
    {
        var item = UpdateOnce(Elixir, sellIn, quality);

        Assert.Equal(expectedQuality, item.Quality);
    }

    [Fact]
    public void Ordinary_item_keeps_degrading_day_after_day()
    {
        var item = UpdateFor(12, Vest, sellIn: 10, quality: 20);

        Assert.Equal(-2, item.SellIn);
        Assert.Equal(6, item.Quality);
    }

    [Fact]
    public void Sell_in_goes_negative_rather_than_stopping_at_zero()
    {
        var item = UpdateFor(3, Vest, sellIn: 1, quality: 50);

        Assert.Equal(-2, item.SellIn);
    }

    // ---------- Aged Brie ----------

    [Theory]
    [InlineData(2, 0, 1, 1)]      // improves instead of degrading
    [InlineData(0, 10, -1, 12)]   // improves twice as fast once the date passes
    [InlineData(-5, 10, -6, 12)]  // and keeps doing so
    public void Aged_brie_improves_with_age(
        int sellIn, int quality, int expectedSellIn, int expectedQuality)
    {
        var item = UpdateOnce(Brie, sellIn, quality);

        Assert.Equal(expectedSellIn, item.SellIn);
        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData(2, 50, 50)]    // already at the ceiling
    [InlineData(2, 49, 50)]    // single increment lands exactly on it
    [InlineData(0, 49, 50)]    // double increment must not push past it
    [InlineData(-1, 50, 50)]
    public void Aged_brie_never_rises_above_fifty(int sellIn, int quality, int expectedQuality)
    {
        var item = UpdateOnce(Brie, sellIn, quality);

        Assert.Equal(expectedQuality, item.Quality);
    }

    [Fact]
    public void Aged_brie_accumulates_quality_over_several_days()
    {
        var item = UpdateFor(5, Brie, sellIn: 2, quality: 0);

        Assert.Equal(-3, item.SellIn);
        Assert.Equal(8, item.Quality);
    }

    // ---------- backstage passes ----------

    [Theory]
    [InlineData(15, 20, 14, 21)]  // more than ten days out: +1
    [InlineData(11, 20, 10, 21)]  // eleven days: still +1 - the boundary is read before the decrement
    [InlineData(10, 20, 9, 22)]   // ten days or fewer: +2
    [InlineData(6, 20, 5, 22)]    // six days: still +2
    [InlineData(5, 20, 4, 23)]    // five days or fewer: +3
    [InlineData(1, 20, 0, 23)]    // day of the concert
    [InlineData(0, 20, -1, 0)]    // concert is over: worthless
    [InlineData(-1, 20, -2, 0)]   // and stays worthless
    public void Backstage_pass_value_climbs_then_collapses(
        int sellIn, int quality, int expectedSellIn, int expectedQuality)
    {
        var item = UpdateOnce(Pass, sellIn, quality);

        Assert.Equal(expectedSellIn, item.SellIn);
        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData(15, 50, 50)]   // at the ceiling in the +1 band
    [InlineData(10, 49, 50)]   // +2 would overshoot by one
    [InlineData(5, 48, 50)]    // +3 would overshoot by one
    [InlineData(5, 49, 50)]    // +3 would overshoot by two
    public void Backstage_pass_never_rises_above_fifty(int sellIn, int quality, int expectedQuality)
    {
        var item = UpdateOnce(Pass, sellIn, quality);

        Assert.Equal(expectedQuality, item.Quality);
    }

    [Fact]
    public void Backstage_pass_reaches_the_ceiling_on_the_day_of_the_concert()
    {
        var item = UpdateFor(15, Pass, sellIn: 15, quality: 20);

        Assert.Equal(0, item.SellIn);
        Assert.Equal(50, item.Quality);
    }

    [Fact]
    public void Backstage_pass_is_worthless_the_day_after_the_concert()
    {
        var item = UpdateFor(16, Pass, sellIn: 15, quality: 20);

        Assert.Equal(-1, item.SellIn);
        Assert.Equal(0, item.Quality);
    }

    // ---------- Sulfuras ----------

    [Theory]
    [InlineData(0, 80)]
    [InlineData(-1, 80)]
    [InlineData(5, 80)]
    public void Sulfuras_never_changes(int sellIn, int quality)
    {
        var item = UpdateFor(10, Sulfuras, sellIn, quality);

        Assert.Equal(sellIn, item.SellIn);
        Assert.Equal(quality, item.Quality);
    }

    // ---------- the shipped inventory ----------

    [Fact]
    public void The_sample_inventory_updates_as_it_always_has()
    {
        var app = new Program
        {
            Items = new List<Item>
            {
                new Item { Name = Vest, SellIn = 10, Quality = 20 },
                new Item { Name = Brie, SellIn = 2, Quality = 0 },
                new Item { Name = Elixir, SellIn = 5, Quality = 7 },
                new Item { Name = Sulfuras, SellIn = 0, Quality = 80 },
                new Item { Name = Pass, SellIn = 15, Quality = 20 }
            }
        };

        app.UpdateQuality();

        Assert.Equal(new[] { 9, 1, 4, 0, 14 }, app.Items.Select(i => i.SellIn));
        Assert.Equal(new[] { 19, 1, 6, 80, 21 }, app.Items.Select(i => i.Quality));
    }

    [Fact]
    public void Every_item_in_the_list_is_updated_not_just_the_first()
    {
        var app = new Program
        {
            Items = new List<Item>
            {
                new Item { Name = Vest, SellIn = 5, Quality = 10 },
                new Item { Name = Vest, SellIn = 5, Quality = 10 },
                new Item { Name = Vest, SellIn = 5, Quality = 10 }
            }
        };

        app.UpdateQuality();

        Assert.All(app.Items, i => Assert.Equal(9, i.Quality));
    }

    [Fact]
    public void An_empty_inventory_is_not_an_error()
    {
        var app = new Program();

        app.UpdateQuality();

        Assert.Empty(app.Items);
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
