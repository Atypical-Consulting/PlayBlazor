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
                Summary: _xmlDocs?.GetPropertySummary(property)));
        }

        return new ComponentDescriptor(
            Type: type,
            DisplayName: StripArity(type.Name),
            Category: type.Namespace ?? string.Empty,
            Summary: _xmlDocs?.GetTypeSummary(type),
            Parameters: parameters,
            Warning: warning);
    }

    private static string StripArity(string typeName)
    {
        var backtick = typeName.IndexOf('`');
        return backtick < 0 ? typeName : typeName[..backtick];
    }
}
