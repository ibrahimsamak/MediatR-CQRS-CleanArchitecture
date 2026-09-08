// Common/Behaviors/TransactionBehavior.cs
namespace OrderFlow.Application.Common.Behaviors;

using MediatR;
using OrderFlow.Application.Common.Interfaces;

public interface ITransactionalRequest;

public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
            return await next(cancellationToken);

        TResponse response = default!;
        await unitOfWork.ExecuteInTransactionAsync(async () => { response = await next(cancellationToken); }, cancellationToken);
        return response;
    }
}
