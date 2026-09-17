namespace OrderFlow.Application.UnitTests.Orders.ListOrders;

using FluentAssertions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.ListOrders;

public sealed class ListOrdersQueryHandlerTests
{
    private sealed class RecordingReadStore : IOrderReadStore
    {
        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public string? Status { get; private set; }

        public int Calls { get; private set; }

        public Task<PagedResult<OrderDto>> ListAsync(int page, int pageSize, string? status, CancellationToken ct)
        {
            Calls++;
            Page = page;
            PageSize = pageSize;
            Status = status;
            return Task.FromResult(new PagedResult<OrderDto>([], page, pageSize, 0));
        }
    }

    private readonly RecordingReadStore _readStore = new();

    private ListOrdersQueryHandler Handler => new(_readStore);

    [Fact]
    public async Task Handle_PassesPagingAndFilterThroughToTheReadStore()
    {
        await Handler.Handle(new ListOrdersQuery(3, 25, "Placed"), CancellationToken.None);

        _readStore.Calls.Should().Be(1);
        _readStore.Page.Should().Be(3);
        _readStore.PageSize.Should().Be(25);
        _readStore.Status.Should().Be("Placed");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Handle_ClampsAPageBelowOneToTheFirstPage(int page)
    {
        await Handler.Handle(new ListOrdersQuery(page), CancellationToken.None);

        _readStore.Page.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(20, 20)]
    [InlineData(100, 100)]
    [InlineData(1000, 100)]
    public async Task Handle_ClampsPageSizeToBetweenOneAndOneHundred(int requested, int expected)
    {
        await Handler.Handle(new ListOrdersQuery(1, requested), CancellationToken.None);

        _readStore.PageSize.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_DefaultsToTheFirstPageOfTwentyWithNoFilter()
    {
        await Handler.Handle(new ListOrdersQuery(), CancellationToken.None);

        _readStore.Page.Should().Be(1);
        _readStore.PageSize.Should().Be(20);
        _readStore.Status.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsTheReadStoreResultUnchanged()
    {
        var result = await Handler.Handle(new ListOrdersQuery(2, 10), CancellationToken.None);

        result.page.Should().Be(2);
        result.pageSize.Should().Be(10);
        result.items.Should().BeEmpty();
    }
}
