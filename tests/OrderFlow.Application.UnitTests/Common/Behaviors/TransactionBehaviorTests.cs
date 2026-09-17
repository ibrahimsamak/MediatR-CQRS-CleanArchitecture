namespace OrderFlow.Application.UnitTests.Common.Behaviors;

using FluentAssertions;
using OrderFlow.Application.Common.Behaviors;
using OrderFlow.Application.UnitTests.TestDoubles;

public sealed class TransactionBehaviorTests
{
    private sealed record PlainRequest;

    private sealed record TransactionalRequest : ITransactionalRequest;

    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task ARequestWithoutTheMarker_SkipsTheTransaction()
    {
        var behavior = new TransactionBehavior<PlainRequest, string>(_unitOfWork);

        var response = await behavior.Handle(new PlainRequest(), _ => Task.FromResult("handled"), CancellationToken.None);

        response.Should().Be("handled");
        _unitOfWork.TransactionCalls.Should().Be(0);
    }

    [Fact]
    public async Task AMarkedRequest_RunsInsideOneTransaction()
    {
        var behavior = new TransactionBehavior<TransactionalRequest, string>(_unitOfWork);
        var ranInsideTransaction = false;

        var response = await behavior.Handle(new TransactionalRequest(), _ =>
        {
            ranInsideTransaction = _unitOfWork.InTransaction;
            return Task.FromResult("handled");
        }, CancellationToken.None);

        response.Should().Be("handled");
        ranInsideTransaction.Should().BeTrue();
        _unitOfWork.TransactionCalls.Should().Be(1);
    }

    [Fact]
    public async Task TheHandlerCommitsInsideTheTransactionScope()
    {
        var behavior = new TransactionBehavior<TransactionalRequest, string>(_unitOfWork);

        await behavior.Handle(new TransactionalRequest(), async ct =>
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return "handled";
        }, CancellationToken.None);

        _unitOfWork.SavedInsideTransaction.Should().BeTrue();
    }

    [Fact]
    public async Task AFailingHandler_PropagatesSoTheTransactionCanRollBack()
    {
        var behavior = new TransactionBehavior<TransactionalRequest, string>(_unitOfWork);

        var act = () => behavior.Handle(
            new TransactionalRequest(),
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        _unitOfWork.InTransaction.Should().BeFalse();
    }
}
