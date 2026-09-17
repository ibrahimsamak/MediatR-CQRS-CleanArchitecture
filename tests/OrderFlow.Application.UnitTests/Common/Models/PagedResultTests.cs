namespace OrderFlow.Application.UnitTests.Common.Models;

using FluentAssertions;
using OrderFlow.Application.Common.Models;

public sealed class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 7, 15)]
    public void TotalPages_RoundsUp(int totalCount, int pageSize, int expected)
    {
        var result = new PagedResult<string>([], 1, pageSize, totalCount);

        result.TotalPages.Should().Be(expected);
    }

    [Fact]
    public void ItCarriesTheItemsAndPagingItWasBuiltWith()
    {
        var result = new PagedResult<string>(["a", "b"], 2, 2, 4);

        result.items.Should().Equal("a", "b");
        result.page.Should().Be(2);
        result.pageSize.Should().Be(2);
        result.TotalCount.Should().Be(4);
    }
}
