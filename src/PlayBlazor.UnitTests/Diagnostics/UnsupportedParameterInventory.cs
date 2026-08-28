using MudBlazor;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor.UnitTests.Diagnostics;

public class UnsupportedParameterInventory
{
    [Test]
    [Explicit("Diagnostic inventory — run on demand; prints a report instead of asserting.")]
    public void ListUnsupportedParameterTypes()
    {
        var provider = new ReflectionCatalogProvider();
        var groups = provider.Discover(typeof(MudButton).Assembly)
            .SelectMany(c => c.Parameters
                .Where(p => p.Kind == ControlKind.Unsupported)
                .Select(p => (Component: c.DisplayName, Parameter: p)))
            .GroupBy(x => Pretty(x.Parameter.Type))
            .OrderByDescending(g => g.Count());

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
