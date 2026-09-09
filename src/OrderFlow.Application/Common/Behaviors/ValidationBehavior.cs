
using FluentValidation;
using MediatR;
//using OrderFlow.Application.Common.Exceptions;
using ValidationException = OrderFlow.Application.Common.Exceptions.ValidationException;

namespace OrderFlow.Application.Common.Behaviors;


public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        // A fresh context per validator: FluentValidation accumulates failures into a shared
        // context, which would report the same failure once per validator.
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(g => g.ErrorMessage).ToArray());
            throw new ValidationException(errors);

        }
        return await next(cancellationToken);
    }
}