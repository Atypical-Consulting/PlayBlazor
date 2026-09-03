using System.Reflection;
using PlayBlazor.Model;

namespace PlayBlazor.Discovery;

/// <summary>Supplies component descriptors. Reflection-based in v1; a source generator can replace it later.</summary>
public interface IComponentCatalogProvider
{
    /// <summary>Describes one component: its parameters, their controls, and their captured defaults.</summary>
    /// <param name="componentType">A closed component type; open generics must be closed by the caller.</param>
    /// <returns>The descriptor driving the playground for that component.</returns>
    ComponentDescriptor Describe(Type componentType);

    /// <summary>Describes every component an assembly exposes.</summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>One descriptor per public concrete component, ordered by display name.</returns>
    IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly);
}
