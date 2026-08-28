using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;

namespace PlayBlazor.Discovery;

public sealed class ReflectionCatalogProvider : IComponentCatalogProvider
{
    private readonly ConcurrentDictionary<Type, ComponentDescriptor> _cache = new();
    private readonly XmlDocSummaryReader? _xmlDocs;

    public ReflectionCatalogProvider(XmlDocSummaryReader? xmlDocs = null)
        => _xmlDocs = xmlDocs;

    public ComponentDescriptor Describe(Type componentType)
        => _cache.GetOrAdd(componentType, Build);

    public IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly)
        => assembly.GetTypes()
            .Where(static t => t.IsPublic && !t.IsAbstract && typeof(ComponentBase).IsAssignableFrom(t))
            .Select(static t => TryCloseGeneric(t))
            .OfType<Type>()
            .Select(Describe)
            .OrderBy(static d => d.DisplayName, StringComparer.Ordinal)
            .ToArray();

    private static Type? TryCloseGeneric(Type type)
    {
        if (!type.IsGenericTypeDefinition)
        {
            return type;
        }

        foreach (var candidate in new[] { typeof(string), typeof(int) })
        {
            try
            {
                var arguments = new Type[type.GetGenericArguments().Length];
                Array.Fill(arguments, candidate);
                return type.MakeGenericType(arguments);
            }
            catch (ArgumentException)
            {
                // Constraint not satisfied by this candidate — try the next one.
            }
        }

        return null;
    }

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
        var nullability = new NullabilityInfoContext();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<ParameterAttribute>() is null)
            {
                continue;
            }

            var (kind, isNullable) = ControlKindResolver.Resolve(property.PropertyType);
            if (!isNullable && !property.PropertyType.IsValueType)
            {
                // Nullable<T> is visible on the type; reference-type `string?` only on the property.
                isNullable = nullability.Create(property).WriteState == NullabilityState.Nullable;
            }

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

            var (group, groupOrder) = ResolveCategory(property);
            parameters.Add(new ParameterDescriptor(
                Name: property.Name,
                Type: property.PropertyType,
                Kind: kind,
                IsNullable: isNullable,
                DefaultValue: defaultValue,
                HasDefault: hasDefault,
                Summary: _xmlDocs?.GetPropertySummary(property),
                Group: group,
                GroupOrder: groupOrder));
        }

        return new ComponentDescriptor(
            Type: type,
            DisplayName: StripArity(type.Name),
            Category: type.Namespace ?? string.Empty,
            Summary: _xmlDocs?.GetTypeSummary(type),
            Parameters: parameters,
            Warning: warning,
            CanInstantiate: instance is not null);
    }

    /// <summary>
    /// Groups a parameter by any <c>CategoryAttribute</c> the library declares — matched by
    /// type name so MudBlazor's own attribute (string <c>Name</c>, int <c>Order</c>) and
    /// <see cref="System.ComponentModel.CategoryAttribute"/> (string <c>Category</c>) both
    /// work without a compile-time dependency.
    /// </summary>
    private static (string Group, int Order) ResolveCategory(PropertyInfo property)
    {
        foreach (var attribute in property.GetCustomAttributes(inherit: true))
        {
            var type = attribute.GetType();
            if (type.Name != "CategoryAttribute")
            {
                continue;
            }

            var name = type.GetProperty("Name")?.GetValue(attribute) as string
                       ?? type.GetProperty("Category")?.GetValue(attribute) as string;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var order = type.GetProperty("Order")?.GetValue(attribute) is int o ? o : int.MaxValue - 1;
            return (name, order);
        }

        return ("General", int.MaxValue);
    }

    private static string StripArity(string typeName)
    {
        var backtick = typeName.IndexOf('`');
        return backtick < 0 ? typeName : typeName[..backtick];
    }
}
