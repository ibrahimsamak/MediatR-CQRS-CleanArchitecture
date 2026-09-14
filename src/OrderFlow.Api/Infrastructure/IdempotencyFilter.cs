namespace OrderFlow.Api.Infrastructure;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OrderFlow.Application.Common.Interfaces;

public sealed class IdempotencyFilter(ICacheService cache) : IAsyncActionFilter
{
    private const string HeaderName = "Idempotency-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing Idempotency-Key header"
            });
            return;
        }

        var cacheKey = $"idem:{keyValues}";
        var cached = await cache.GetAsync<CachedResponse>(cacheKey);
        if (cached is not null)
        {
            context.Result = new ObjectResult(cached.Body) { StatusCode = cached.StatusCode };
            return;
        }

        var executed = await next();
        if (executed.Result is ObjectResult { StatusCode: >= 200 and < 300 } ok)
            await cache.SetAsync(cacheKey, new CachedResponse(ok.StatusCode ?? 200, ok.Value), TimeSpan.FromHours(24));
    }

    private sealed record CachedResponse(int StatusCode, object? Body);
}
