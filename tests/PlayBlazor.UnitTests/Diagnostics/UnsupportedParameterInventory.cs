using System.Reflection;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor.UnitTests.Diagnostics;

public class UnsupportedParameterInventory
{
    [Test]
    [Explicit("Diagnostic inventory — run on demand; prints a report instead of asserting.")]
    [TestCaseSource(typeof(ExploredLibraries), nameof(ExploredLibraries.All))]
    public void ListUnsupportedParameterTypes(Assembly assembly, Action<PlayBlazorOptions> configure)
    {
        var options = new PlayBlazorOptions();
        configure(options);
        var provider = new ReflectionCatalogProvider(options: options);
        var groups = provider.Discover(assembly)
            .SelectMany(c => c.Parameters
                .Where(p => p.Kind == ControlKind.Unsupported)
                .Select(p => (Component: c.DisplayName, Parameter: p)))
            .GroupBy(x => Pretty(x.Parameter.Type))
            .OrderByDescending(g => g.Count());

        TestContext.Out.WriteLine($"=== {assembly.GetName().Name} ===");
        foreach (var group in groups)
        {
            var examples = string.Join(", ", group.Take(4).Select(x => $"{x.Component}.{x.Parameter.Name}"));
            TestContext.Out.WriteLine($"{group.Count(),4} × {group.Key,-60} e.g. {examples}");
        }
    }

    private static string Pretty(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } u)
        {
            return Pretty(u) + "?";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        return $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(",", type.GetGenericArguments().Select(Pretty))}>";
    }
}
