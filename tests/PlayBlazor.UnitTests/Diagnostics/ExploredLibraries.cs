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
    /// <summary>One NUnit case per explored library: assembly and host configuration, named for the report.</summary>
    public static IEnumerable<TestCaseData> All
    {
        get
        {
            yield return new TestCaseData(
                typeof(MudBlazor.MudButton).Assembly,
                (Action<PlayBlazorOptions>)PlayBlazor.DemoHost.PlaygroundConfig.Configure)
                { TestName = "MudBlazor" };

            yield return new TestCaseData(
                typeof(Microsoft.FluentUI.AspNetCore.Components.FluentButton).Assembly,
                (Action<PlayBlazorOptions>)PlayBlazor.Demo.FluentUI.FluentPlaygroundConfig.Configure)
                { TestName = "FluentUI" };

            // DaisyBlazor is not yet a case here: `demo/PlayBlazor.Demo.Daisy` does not exist
            // because `DaisyBlazor.Components` 1.0.0 is not on NuGet and `@daisyblazor/tailwind`
            // is not on npm — the release PR (phmatray/blazor-tailwind-ui#20) is still open. Once
            // that app exists (Task 8), add a third case here the same shape as the two above:
            // typeof(DaisyBlazor.Button).Assembly and PlayBlazor.Demo.Daisy.DaisyPlaygroundConfig.Configure,
            // TestName = "DaisyBlazor".
        }
    }
}
