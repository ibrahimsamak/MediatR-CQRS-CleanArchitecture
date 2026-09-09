using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Orders;
using OrderFlow.Infrastructure.BackgroundJobs;
using OrderFlow.Infrastructure.Caching;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.Interceptors;
using OrderFlow.Infrastructure.Persistence.Repositories;
using OrderFlow.Infrastructure.Time;
namespace OrderFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ConvertDomainEventsToOutboxInterceptor>();
        services.AddDbContext<OrderDbContext>((sp, options) =>
        {
            options.UseSqlServer(config.GetConnectionString("OrderDb"),
                sql => sql.EnableRetryOnFailure());
            options.AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>());
        });
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadStore, OrderReadStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddStackExchangeRedisCache(o => o.Configuration = config.GetConnectionString("Redis"));
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddHostedService<OutboxDispatcherService>();
        return services;

    }
}
