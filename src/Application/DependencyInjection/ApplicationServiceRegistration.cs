using Microsoft.Extensions.DependencyInjection;

namespace BeautyCommerce.Application;

/// <summary>
/// Marker assembly cho Application layer.
/// </summary>
public static class AssemblyMarker
{
    public static Type GetType() => typeof(AssemblyMarker);
}

/// <summary>
/// Extension methods để đăng ký Application services.
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Auto-register MediatR handlers, Validators, etc.
        // Các service cụ thể sẽ được đăng ký qua MediatR và FluentValidation
        
        return services;
    }
}
