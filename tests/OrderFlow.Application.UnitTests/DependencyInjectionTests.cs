namespace OrderFlow.Application.UnitTests;

using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Orders.PlaceOrder;
using OrderFlow.Application.UnitTests.TestDoubles;
using OrderFlow.Domain.Orders;
using ValidationException = OrderFlow.Application.Common.Exceptions.ValidationException;

/// <summary>
/// Verifies the composition contract of <c>AddApplication</c>: handlers and validators are discovered,
/// and every request really travels validation → logging → transaction → handler.
/// </summary>
public sealed class DependencyInjectionTests
{
    private readonly FakeOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IOrderRepository>(_orders);
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton<ICacheService>(new InMemoryCacheService());
        return services.BuildServiceProvider();
    }

    private static PlaceOrderCommand AValidCommand() =>
        new("CUST-1001", "USD", "12 Market St", "Istanbul", "34000", "TR",
            [new PlaceOrderLine("SKU-1", 2, 19.99m)]);

    [Fact]
    public void AddApplication_RegistersTheMediator()
    {
        using var provider = BuildProvider();

        provider.GetService<ISender>().Should().NotBeNull();
        provider.GetService<IPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_DiscoversTheValidators()
    {
        using var provider = BuildProvider();

        provider.GetServices<IValidator<PlaceOrderCommand>>().Should().ContainSingle()
            .Which.Should().BeOfType<PlaceOrderCommandValidator>();
    }

    [Fact]
    public async Task AValidCommand_ReachesItsHandlerThroughThePipeline()
    {
        using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();

        var id = await sender.Send(AValidCommand());

        _orders.Added.Should().ContainSingle().Which.Id.Value.Should().Be(id);
        _unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task AnInvalidCommand_IsRejectedByTheValidationBehavior()
    {
        using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();
        var invalid = AValidCommand() with { CustomerId = "" };

        var act = () => sender.Send(invalid);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Subject.Single();
        exception.Errors.Should().ContainKey(nameof(PlaceOrderCommand.CustomerId));
        _orders.Added.Should().BeEmpty();
        _unitOfWork.TransactionCalls.Should().Be(0);
    }

    [Fact]
    public async Task ACommandMarkedTransactional_CommitsInsideATransaction()
    {
        using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(AValidCommand());

        _unitOfWork.TransactionCalls.Should().Be(1);
        _unitOfWork.SavedInsideTransaction.Should().BeTrue();
    }
}
