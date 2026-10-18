using System.Reflection;
using NUnit.Framework;
using PlayBlazor;

namespace PlayBlazor.UnitTests.Diagnostics;

/// <summary>
/// The libraries the diagnostic sweeps inventory — one case per demo app, so a sweep reports
/// on exactly the configuration that app ships.
/// </summary>
public static class ExploredLibraries
{
    private static TestCaseData Mud => new(
        typeof(MudBlazor.MudButton).Assembly,
        (Action<PlayBlazorOptions>)PlayBlazor.DemoHost.PlaygroundConfig.Configure)
        { TestName = "MudBlazor" };

    private static TestCaseData Fluent => new(
        typeof(Microsoft.FluentUI.AspNetCore.Components.FluentButton).Assembly,
        (Action<PlayBlazorOptions>)PlayBlazor.Demo.FluentUI.FluentPlaygroundConfig.Configure)
        { TestName = "FluentUI" };

    private static TestCaseData Daisy => new(
        typeof(DaisyBlazor.Button).Assembly,
        (Action<PlayBlazorOptions>)PlayBlazor.Demo.Daisy.DaisyPlaygroundConfig.Configure)
        { TestName = "DaisyBlazor" };

    /// <summary>One NUnit case per explored library: assembly and host configuration, named for the report.</summary>
    public static IEnumerable<TestCaseData> All
    {
        get
        {
            yield return Mud;
            yield return Fluent;
            yield return Daisy;
        }
    }
}
