using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlayBlazor.Discovery;

namespace PlayBlazor;

public static class PlayBlazorServiceCollectionExtensions
{
    public static IServiceCollection AddPlayBlazor(this IServiceCollection services)
    {
        services.TryAddSingleton<IComponentCatalogProvider>(static _ => new ReflectionCatalogProvider());
        return services;
    }
}
