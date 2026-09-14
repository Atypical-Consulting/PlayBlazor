namespace PlayBlazor.Demo.FluentUI;

/// <summary>
/// The razor text of each demo fragment, verbatim — fed to the slot presets as their
/// <c>source</c> so the generated code panel shows copy-pasteable markup instead of a
/// placeholder comment. Keep each string in sync with its fragment in FluentDemoFragments.razor.
/// </summary>
public static class FluentDemoFragmentSources
{
    public const string RadioOptions = """
<FluentRadio Label="Apple" />
<FluentRadio Label="Banana" />
<FluentRadio Label="Orange" Disabled="true" />
<FluentRadio Label="Kiwi" />
""";
}
