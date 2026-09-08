using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderFlow.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, Exception?> _handling =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(0, "HandlingRequest"), "Handling {Request}");

    private static readonly Action<ILogger, string, double, Exception?> _handled =
        LoggerMessage.Define<string, double>(LogLevel.Information, new EventId(1, "HandledRequest"), "Handled {Request} in {Elapsed}ms");

    private static readonly Action<ILogger, string, Exception?> _failed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "RequestFailed"), "{Request} failed");

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            var name = typeof(TRequest).Name;
            _handling(_logger, name, null);
        }

        try
        {
            var response = await next(cancellationToken);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var name = typeof(TRequest).Name;
                var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                _handled(_logger, name, elapsedMs, null);
            }

            return response;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                var name = typeof(TRequest).Name;
                _failed(_logger, name, ex);
            }

            throw;
        }
    }
}