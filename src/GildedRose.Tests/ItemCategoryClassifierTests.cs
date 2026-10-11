using GildedRose.Console.Updaters;
using Xunit;

namespace GildedRose.Tests;

/// <summary>
/// Classification, tested apart from dispatch.
/// </summary>
/// <remarks>
/// These used to be reachable only through the factory, which meant every
/// name-matching assertion was really asserting two things at once and naming
/// neither. Splitting the classifier out lets the matching rules be checked
/// directly - including the precedence order, which was previously inferred from
/// the behaviour of one carefully chosen name.
/// </remarks>
public class ItemCategoryClassifierTests
{
    [Theory]
    [InlineData("+5 Dexterity Vest", ItemCategory.Standard)]
    [InlineData("Elixir of the Mongoose", ItemCategory.Standard)]
    [InlineData("Aged Brie", ItemCategory.AgedBrie)]
    [InlineData("Backstage passes to a TAFKAL80ETC concert", ItemCategory.BackstagePass)]
    [InlineData("Sulfuras, Hand of Ragnaros", ItemCategory.Legendary)]
    [InlineData("Conjured Mana Cake", ItemCategory.Conjured)]
    public void Each_known_name_classifies_to_its_category(string name, ItemCategory expected)
    {
        Assert.Equal(expected, ItemCategoryClassifier.Classify(name));
    }

    [Theory]
    [InlineData("Backstage passes to a Spice Girls reunion", ItemCategory.BackstagePass)]
    [InlineData("backstage passes to anything at all", ItemCategory.BackstagePass)]
    [InlineData("Sulfuras, Boot of Ragnaros", ItemCategory.Legendary)]
    [InlineData("sulfuras, hand of ragnaros", ItemCategory.Legendary)]
    [InlineData("conjured mana cake", ItemCategory.Conjured)]
    public void Product_lines_are_matched_by_prefix_and_case_is_ignored(string name, ItemCategory expected)
    {
        Assert.Equal(expected, ItemCategoryClassifier.Classify(name));
    }

    [Fact]
    public void Aged_Brie_is_matched_exactly_because_it_names_a_product_not_a_line()
    {
        Assert.Equal(ItemCategory.AgedBrie, ItemCategoryClassifier.Classify("aged brie"));
        Assert.Equal(ItemCategory.Standard, ItemCategoryClassifier.Classify("Aged Brie Deluxe"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Something nobody has ever stocked")]
    [InlineData(null)]
    public void Anything_unrecognised_is_ordinary_stock(string? name)
    {
        Assert.Equal(ItemCategory.Standard, ItemCategoryClassifier.Classify(name));
    }

    [Fact]
    public void Conjured_is_tested_before_every_other_category()
    {
        Assert.Equal(ItemCategory.Conjured, ItemCategoryClassifier.Precedence[0]);
    }

    [Fact]
    public void Conjuring_wins_over_what_the_product_otherwise_is()
    {
        Assert.Equal(ItemCategory.Conjured, ItemCategoryClassifier.Classify("Conjured Aged Brie"));
    }

    [Fact]
    public void Ordinary_stock_is_tested_last_so_that_it_cannot_shadow_a_real_category()
    {
        Assert.Equal(ItemCategory.Standard, ItemCategoryClassifier.Precedence[^1]);
    }

    [Fact]
    public void Every_category_appears_in_the_precedence_order_exactly_once()
    {
        var precedence = ItemCategoryClassifier.Precedence;

        Assert.Equal(Enum.GetValues<ItemCategory>().Order(), precedence.Order());
        Assert.Equal(precedence.Count, precedence.Distinct().Count());
    }
}
