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
        // Not `static`: the provider needs the options this call just configured, and the container
        // itself — a library whose components take constructor dependencies (Fluent UI v5 gives
        // nearly every component a LibraryConfiguration) cannot be instantiated without it.
        services.TryAddSingleton<IComponentCatalogProvider>(
            sp => new ReflectionCatalogProvider(options: options, services: sp));
        return services;
    }
}
