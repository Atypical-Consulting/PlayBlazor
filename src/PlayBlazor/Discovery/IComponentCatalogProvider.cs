using System.Reflection;
using PlayBlazor.Model;

namespace PlayBlazor.Discovery;

/// <summary>Supplies component descriptors. Reflection-based in v1; a source generator can replace it later.</summary>
public interface IComponentCatalogProvider
{
    ComponentDescriptor Describe(Type componentType);

    IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly);
}
