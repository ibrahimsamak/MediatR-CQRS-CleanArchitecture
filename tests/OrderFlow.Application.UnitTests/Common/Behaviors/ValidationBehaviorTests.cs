namespace OrderFlow.Application.UnitTests.Common.Behaviors;

using FluentAssertions;
using FluentValidation;
using OrderFlow.Application.Common.Behaviors;
using ValidationException = OrderFlow.Application.Common.Exceptions.ValidationException;

public sealed class ValidationBehaviorTests
{
    private sealed record Request(string Name, int Age);

    private sealed class NameValidator : AbstractValidator<Request>
    {
        public NameValidator() => RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
    }

    private sealed class AgeValidator : AbstractValidator<Request>
    {
        public AgeValidator()
        {
            RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be positive.");
            RuleFor(x => x.Age).LessThan(150).WithMessage("Age must be realistic.");
        }
    }

    private static ValidationBehavior<Request, string> Behavior(params IValidator<Request>[] validators) => new(validators);

    [Fact]
    public async Task WithNoValidators_TheRequestPassesStraightThrough()
    {
        var called = false;

        var response = await Behavior().Handle(new Request("", 0), _ =>
        {
            called = true;
            return Task.FromResult("handled");
        }, CancellationToken.None);

        response.Should().Be("handled");
        called.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEveryValidatorPasses_TheHandlerRuns()
    {
        var response = await Behavior(new NameValidator(), new AgeValidator())
            .Handle(new Request("Ada", 36), _ => Task.FromResult("handled"), CancellationToken.None);

        response.Should().Be("handled");
    }

    [Fact]
    public async Task WhenValidationFails_TheHandlerIsNeverReached()
    {
        var called = false;

        var act = () => Behavior(new NameValidator()).Handle(new Request("", 36), _ =>
        {
            called = true;
            return Task.FromResult("handled");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        called.Should().BeFalse();
    }

    [Fact]
    public async Task FailuresFromEveryValidatorAreReportedTogether()
    {
        var act = () => Behavior(new NameValidator(), new AgeValidator())
            .Handle(new Request("", -1), _ => Task.FromResult("handled"), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Subject.Single();
        exception.Errors.Should().ContainKeys(nameof(Request.Name), nameof(Request.Age));
        exception.Errors[nameof(Request.Name)].Should().Equal("Name is required.");
    }

    [Fact]
    public async Task MessagesForTheSamePropertyAreGroupedUnderOneKey()
    {
        var act = () => Behavior(new AgeValidator())
            .Handle(new Request("Ada", 200), _ => Task.FromResult("handled"), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Subject.Single();
        exception.Errors.Should().ContainSingle();
        exception.Errors[nameof(Request.Age)].Should().Equal("Age must be realistic.");
    }

}
