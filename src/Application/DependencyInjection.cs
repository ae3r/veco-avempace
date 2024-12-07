using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


namespace Application;

/// <summary>
/// DependencyInjection class
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// AddApplication
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddOpenBehavior(typeof(RequestExceptionProcessorBehavior<,>));
        });

        services.AddLazyCache();
        return services;
    }

    /// <summary>
    /// AddWorkflowSteps
    /// </summary>
    /// <param name="services"></param>
    /// <param name="predicate"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddWorkflowSteps(this IServiceCollection services, Func<Type, bool> predicate, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }
        assemblies
           .SelectMany(x => x.GetExportedTypes()
           .Where(y => y.IsClass && !y.IsAbstract && !y.IsGenericType && !y.IsNested))
           .Where(predicate)
           .ToList()
           .ForEach(type =>
                    services.Add(new ServiceDescriptor(type, type, ServiceLifetime.Transient))
           );
        return services;
    }
}
