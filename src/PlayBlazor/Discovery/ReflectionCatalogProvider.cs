using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;

namespace PlayBlazor.Discovery;

public sealed class ReflectionCatalogProvider : IComponentCatalogProvider
{
    private readonly ConcurrentDictionary<Type, ComponentDescriptor> _cache = new();

    public ComponentDescriptor Describe(Type componentType)
        => _cache.GetOrAdd(componentType, Build);

    public IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly)
        => throw new NotImplementedException(); // Task 4

    private ComponentDescriptor Build(Type type)
    {
        string? warning = null;
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(type);
        }
        catch (Exception)
        {
            warning = "Defaults not captured: the component could not be instantiated.";
        }

        var parameters = new List<ParameterDescriptor>();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<ParameterAttribute>() is null)
            {
                continue;
            }

            var (kind, isNullable) = ControlKindResolver.Resolve(property.PropertyType);

            object? defaultValue = null;
            var hasDefault = false;
            if (instance is not null && property.CanRead)
            {
                try
                {
                    defaultValue = property.GetValue(instance);
                    hasDefault = true;
                }
                catch (Exception)
                {
                    // A throwing getter leaves this parameter without a known default.
                }
            }

            parameters.Add(new ParameterDescriptor(
                Name: property.Name,
                Type: property.PropertyType,
                Kind: kind,
                IsNullable: isNullable,
                DefaultValue: defaultValue,
                HasDefault: hasDefault,
                Summary: null));
        }

        return new ComponentDescriptor(
            Type: type,
            DisplayName: StripArity(type.Name),
            Category: type.Namespace ?? string.Empty,
            Summary: null,
            Parameters: parameters,
            Warning: warning);
    }

    private static string StripArity(string typeName)
    {
        var backtick = typeName.IndexOf('`');
        return backtick < 0 ? typeName : typeName[..backtick];
    }
}
