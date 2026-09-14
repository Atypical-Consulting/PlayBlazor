using System.Reflection;
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
/// When it fails, it writes the regenerated corpus into the test working directory and names the
/// path: review the diff, copy it over the checked-in file, and rebuild — the build is what
/// re-checks the new snippets.
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
        var fileName = CorpusFileName(assembly);
        var checkedIn = Normalize(ReadEmbeddedCorpus(fileName));
        var beginAt = checkedIn.IndexOf(Begin, StringComparison.Ordinal);
        var endAt = checkedIn.IndexOf(End, StringComparison.Ordinal);
        (beginAt >= 0 && endAt > beginAt).Should().BeTrue($"{fileName} should carry both markers");

        var present = checkedIn[(beginAt + Begin.Length)..endAt];
        var expected = Generate(assembly, configure);
        if (present == expected)
        {
            return;
        }

        var regenerated = checkedIn[..(beginAt + Begin.Length)] + expected + checkedIn[endAt..];
        var updated = Path.Combine(TestContext.CurrentContext.WorkDirectory, fileName + ".regenerated");
        File.WriteAllText(updated, regenerated);
        Assert.Fail(
            $"The {assembly.GetName().Name} snippet corpus is stale. A regenerated copy is at{Environment.NewLine}"
            + $"  {updated}{Environment.NewLine}"
            + $"Review the diff, copy it over tests/PlayBlazor.UnitTests/CodeGen/Corpus/{fileName}, "
            + "and rebuild — the build is what compiles the new snippets.");
    }

    /// <summary>
    /// The corpus as it was checked in, read out of the assembly rather than off disk.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT a source path. Resolving the file through <c>[CallerFilePath]</c> (or any
    /// other compile-time path) passes on a dev machine and can never pass on CI: setting
    /// <c>ContinuousIntegrationBuild</c> turns on <c>DeterministicSourcePaths</c>, which rewrites
    /// every compile-time path to <c>/_/…</c> so the binaries are reproducible — a directory that
    /// exists on no machine. Embedding keeps the assertion exactly as strong, because the embedded
    /// bytes ARE the checked-in file: the same build that compiles the corpus as Razor embeds it.
    /// </remarks>
    private static string ReadEmbeddedCorpus(string fileName)
    {
        var self = typeof(SnippetCorpusTests).Assembly;
        var resource = self.GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith("." + fileName, StringComparison.Ordinal));

        resource.Should().NotBeNull(
            "{0} should be embedded — see the EmbeddedResource item in the test project. Embedded: {1}",
            fileName,
            string.Join(", ", self.GetManifestResourceNames()));

        using var stream = self.GetManifestResourceStream(resource!)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
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

    private static string CorpusFileName(Assembly assembly)
        => assembly.GetName().Name == "MudBlazor" ? "MudBlazorSnippets.razor" : "FluentUiSnippets.razor";

    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
