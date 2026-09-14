using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.CodeGen;
using PlayBlazor.Discovery;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Diagnostics;
using PlayBlazor.UnitTests.Shell;

namespace PlayBlazor.UnitTests.CodeGen;

/// <summary>
/// The generated Razor snippet is the product: a visitor copies it out of the code panel and
/// pastes it into their own app. So every snippet this repo can generate is checked in, verbatim,
/// as a real <c>.razor</c> file under <c>CodeGen/Corpus/</c> — which means the Razor compiler
/// builds all of them on every build, and a snippet that does not compile breaks the build with
/// the same diagnostic the visitor would have got.
/// </summary>
/// <remarks>
/// <para>
/// This test is the other half of that arrangement: it regenerates every snippet and asserts the
/// corpus still matches, so the checked-in file cannot go stale and quietly stop covering the
/// curation it is supposed to cover. The compiler catches invalid markup; this catches a corpus
/// that has drifted away from what the generator now emits.
/// </para>
/// <para>
/// When it fails, it writes the regenerated corpus beside the checked-in one and names the path:
/// review the diff, copy it over, and rebuild — the build is what re-checks the new snippets.
/// </para>
/// <para>
/// Two snippets in the corpus trip RZ2012 (<c>[EditorRequired]</c> with no value), which is why
/// the test project sets <c>NoWarn=RZ2012</c> — see the comment there. Both are the known
/// <c>RenderFragment&lt;T&gt;</c> / icon-source gaps recorded in the curation comments, not new
/// defects, and neither is a syntax error: the markup parses, it is the value that is missing.
/// </para>
/// </remarks>
public class SnippetCorpusTests
{
    private const string Begin = "@* ==> generated snippets begin — see SnippetCorpusTests <== *@";

    private const string End = "@* ==> generated snippets end <== *@";

    [Test]
    [TestCaseSource(typeof(ExploredLibraries), nameof(ExploredLibraries.All))]
    public void CheckedInCorpus_MatchesWhatTheGeneratorEmits(Assembly assembly, Action<PlayBlazorOptions> configure)
    {
        var corpusPath = CorpusPath(assembly);
        File.Exists(corpusPath).Should().BeTrue($"the corpus {corpusPath} should be checked in");

        var checkedIn = Normalize(File.ReadAllText(corpusPath));
        var beginAt = checkedIn.IndexOf(Begin, StringComparison.Ordinal);
        var endAt = checkedIn.IndexOf(End, StringComparison.Ordinal);
        (beginAt >= 0 && endAt > beginAt).Should().BeTrue($"{corpusPath} should carry both markers");

        var present = checkedIn[(beginAt + Begin.Length)..endAt];
        var expected = Generate(assembly, configure);
        if (present == expected)
        {
            return;
        }

        var regenerated = checkedIn[..(beginAt + Begin.Length)] + expected + checkedIn[endAt..];
        var updated = corpusPath + ".regenerated";
        File.WriteAllText(updated, regenerated);
        Assert.Fail(
            $"The {assembly.GetName().Name} snippet corpus is stale. A regenerated copy is at{Environment.NewLine}"
            + $"  {updated}{Environment.NewLine}"
            + "Review the diff, replace the checked-in file with it, and rebuild — the build is "
            + "what compiles the new snippets.");
    }

    /// <summary>Every listed component's snippet, each under a comment naming it.</summary>
    private static string Generate(Assembly assembly, Action<PlayBlazorOptions> configure)
    {
        var options = new PlayBlazorOptions();
        configure(options);
        var catalog = new ReflectionCatalogProvider(options: options);

        var corpus = new StringBuilder().Append('\n');
        foreach (var component in CuratedSurface.Of(assembly, options, catalog))
        {
            corpus.Append('\n').Append("@* ").Append(component.DisplayName).Append(" *@").Append('\n')
                .Append(RazorSnippetGenerator.Generate(component, new PlaygroundState(), options))
                .Append('\n');
        }

        return corpus.ToString();
    }

    private static string CorpusPath(Assembly assembly)
        => Path.Combine(
            Path.GetDirectoryName(ThisFile())!,
            "Corpus",
            assembly.GetName().Name == "MudBlazor" ? "MudBlazorSnippets.razor" : "FluentUiSnippets.razor");

    private static string ThisFile([CallerFilePath] string path = "") => path;

    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
