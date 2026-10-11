using GildedRose.Console;
using GildedRose.Console.Updaters;
using Xunit;

namespace GildedRose.Tests;

/// <summary>
/// Dispatch rules and the awkward inputs - the cases that are easy to get wrong
/// precisely because nobody thinks to try them.
/// </summary>
public class ItemUpdaterFactoryTests
{
    private readonly ItemUpdaterFactory _factory = new();

    [Theory]
    [InlineData("+5 Dexterity Vest", typeof(StandardItemUpdater))]
    [InlineData("Elixir of the Mongoose", typeof(StandardItemUpdater))]
    [InlineData("Aged Brie", typeof(AgedBrieUpdater))]
    [InlineData("Backstage passes to a TAFKAL80ETC concert", typeof(BackstagePassUpdater))]
    [InlineData("Sulfuras, Hand of Ragnaros", typeof(LegendaryItemUpdater))]
    [InlineData("Conjured Mana Cake", typeof(ConjuredItemUpdater))]
    public void Each_known_name_maps_to_its_updater(string name, Type expected)
    {
        var updater = _factory.For(new Item { Name = name });

        Assert.IsType(expected, updater);
    }

    [Theory]
    [InlineData("Backstage passes to a Spice Girls reunion")]
    [InlineData("backstage passes to anything at all")]
    public void Backstage_passes_are_matched_by_prefix_not_by_the_one_concert_in_the_sample_data(string name)
    {
        Assert.IsType<BackstagePassUpdater>(_factory.For(new Item { Name = name }));
    }

    [Theory]
    [InlineData("Sulfuras, Hand of Ragnaros")]
    [InlineData("Sulfuras, Boot of Ragnaros")]
    [InlineData("sulfuras, hand of ragnaros")]
    public void Legendary_items_are_matched_by_prefix(string name)
    {
        Assert.IsType<LegendaryItemUpdater>(_factory.For(new Item { Name = name }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Something nobody has ever stocked")]
    public void An_unrecognised_name_falls_back_to_ordinary_stock(string name)
    {
        Assert.IsType<StandardItemUpdater>(_factory.For(new Item { Name = name }));
    }

    [Fact]
    public void A_null_name_does_not_throw_and_is_treated_as_ordinary_stock()
    {
        Assert.IsType<StandardItemUpdater>(_factory.For(new Item { Name = null! }));
    }

    [Fact]
    public void A_null_item_is_rejected_rather_than_quietly_ignored()
    {
        Assert.Throws<ArgumentNullException>(() => _factory.For(null!));
    }

    [Fact]
    public void Updaters_are_reused_because_they_hold_no_state()
    {
        var first = _factory.For(new Item { Name = "+5 Dexterity Vest" });
        var second = _factory.For(new Item { Name = "Elixir of the Mongoose" });

        Assert.Same(first, second);
    }

    [Fact]
    public void An_updater_rejects_a_null_item()
    {
        var updater = _factory.For(new Item { Name = "Aged Brie" });

        Assert.Throws<ArgumentNullException>(() => updater.Update(null!));
    }

    /// <summary>
    /// The test that earns the enum.
    /// </summary>
    /// <remarks>
    /// A sixth category added without an updater used to fall through to ordinary
    /// stock, age at the wrong rate and report nothing. This enumerates
    /// <see cref="ItemCategory"/> rather than re-listing it, so the same mistake now
    /// fails here instead of surfacing in a stock valuation.
    /// </remarks>
    [Fact]
    public void Every_category_has_an_updater_registered_against_it()
    {
        foreach (var category in Enum.GetValues<ItemCategory>())
        {
            Assert.NotNull(_factory.ForCategory(category));
        }
    }

    [Fact]
    public void Every_category_resolves_to_a_distinct_updater()
    {
        var updaters = Enum.GetValues<ItemCategory>()
            .Select(_factory.ForCategory)
            .ToArray();

        Assert.Equal(updaters.Length, updaters.Distinct().Count());
    }

    [Fact]
    public void Dispatch_follows_the_published_precedence_rather_than_a_second_set_of_rules()
    {
        var item = new Item { Name = "Conjured Aged Brie" };

        Assert.Same(
            _factory.ForCategory(ItemCategoryClassifier.Classify(item.Name)),
            _factory.For(item));
    }
}
