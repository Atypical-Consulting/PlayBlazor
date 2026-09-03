using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Rendering;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class PermalinkTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        _context.Services.AddPlayBlazor();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private static string EncodeDenseTrue()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
        var state = new PlaygroundState();
        state.Set("Dense", true);
        return PlaygroundStateSerializer.Encode(descriptor, state, new PlaygroundEnvironment());
    }

    [Test]
    public void NavigatingWithPermalink_RestoresState()
    {
        var navigation = _context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"{navigation.BaseUri}?pb-BasicFixture={EncodeDenseTrue()}");

        var cut = _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(BasicFixture)));

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True");
    }

    [Test]
    public void ShareButton_CopiesUrlWithEncodedState()
    {
        _context.JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true);
        var cut = _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(BasicFixture)));
        cut.FindAll("input[type=checkbox]")[0].Change(true); // Dense

        cut.Find(".pb-share").Click();

        var copied = (string?)_context.JSInterop.Invocations
            .Single(i => i.Identifier == "navigator.clipboard.writeText").Arguments[0];
        copied.Should().Contain("pb-BasicFixture=");

        // The copied link must itself restore the state.
        var encoded = copied!.Split("pb-BasicFixture=")[1];
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
        var restored = new PlaygroundState();
        PlaygroundStateSerializer.Decode(Uri.UnescapeDataString(encoded), descriptor, restored, new PlaygroundEnvironment());
        restored.IsModified("Dense").Should().BeTrue();
    }
}
