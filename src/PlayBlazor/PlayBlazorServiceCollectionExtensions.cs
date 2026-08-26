using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlayBlazor.Discovery;

namespace PlayBlazor;

public static class PlayBlazorServiceCollectionExtensions
{
    public static IServiceCollection AddPlayBlazor(this IServiceCollection services, Action<PlayBlazorOptions>? configure = null)
    {
        var options = new PlayBlazorOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IComponentCatalogProvider>(static _ => new ReflectionCatalogProvider());
        return services;
    }
}
