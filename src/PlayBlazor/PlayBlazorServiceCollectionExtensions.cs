using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlayBlazor.Discovery;

namespace PlayBlazor;

/// <summary>Registers PlayBlazor in the host's service collection.</summary>
public static class PlayBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services <see cref="PlaygroundView" />, <see cref="PlaygroundExplorer" /> and the
    /// workspace need: the host <see cref="PlayBlazorOptions" /> and a component catalog provider.
    /// Both are registered only if absent, so a host may substitute its own provider beforehand.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">
    /// Configures presets, scaffolds, variants, exclusions and the theme wrapper. Omit it for
    /// bare reflection over the components with no host customization.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddPlayBlazor(this IServiceCollection services, Action<PlayBlazorOptions>? configure = null)
    {
        var options = new PlayBlazorOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IComponentCatalogProvider>(static _ => new ReflectionCatalogProvider());
        return services;
    }
}
