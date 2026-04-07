using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Outbox;
using BeautyCommerce.Infrastructure.Saga;
using Polly;
using Polly.Extensions.Http;

namespace BeautyCommerce.Infrastructure;

/// <summary>
/// Marker assembly cho Infrastructure layer.
/// </summary>
public static class AssemblyMarker
{
    public static Type GetType() => typeof(AssemblyMarker);
}

/// <summary>
/// Extension methods để đăng ký Infrastructure services.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                      .EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null)));

        // Resilience Policies (Polly)
        services.AddHttpClient("ResilientClient")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

        // Services
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IOrderSaga, OrderSaga>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(new Random().Next(0, 100)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }
}
