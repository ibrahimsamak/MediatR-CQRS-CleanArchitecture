namespace OrderFlow.Application.UnitTests.Common.Behaviors;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Common.Behaviors;
using OrderFlow.Application.UnitTests.TestDoubles;

public sealed class LoggingBehaviorTests
{
    private sealed record Request;

    private readonly CapturingLogger<LoggingBehavior<Request, string>> _logger = new();

    private LoggingBehavior<Request, string> Behavior => new(_logger);

    [Fact]
    public async Task ASuccessfulRequest_IsLoggedBeforeAndAfterTheHandler()
    {
        var response = await Behavior.Handle(new Request(), _ => Task.FromResult("handled"), CancellationToken.None);

        response.Should().Be("handled");
        _logger.Entries.Should().HaveCount(2);
        _logger.Entries.Should().OnlyContain(e => e.Level == LogLevel.Information);
        _logger.Entries[0].Message.Should().Contain(nameof(Request));
        _logger.Entries[1].Message.Should().Contain(nameof(Request)).And.Contain("ms");
    }

    [Fact]
    public async Task AFailingRequest_IsLoggedAsAnErrorAndRethrown()
    {
        var boom = new InvalidOperationException("boom");

        var act = () => Behavior.Handle(new Request(), _ => throw boom, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(boom);

        var failure = _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error).Subject;
        failure.Exception.Should().BeSameAs(boom);
        failure.Message.Should().Contain(nameof(Request));
    }

    [Fact]
    public async Task AFailingRequest_IsNotLoggedAsCompleted()
    {
        var act = () => Behavior.Handle(new Request(), _ => throw new InvalidOperationException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        _logger.Entries.Should().HaveCount(2);
        _logger.Entries.Count(e => e.Level == LogLevel.Information).Should().Be(1);
    }

    [Fact]
    public async Task TheCancellationTokenIsHandedToTheHandler()
    {
        using var cts = new CancellationTokenSource();
        var observed = CancellationToken.None;

        await Behavior.Handle(new Request(), ct =>
        {
            observed = ct;
            return Task.FromResult("handled");
        }, cts.Token);

        observed.Should().Be(cts.Token);
    }
}
