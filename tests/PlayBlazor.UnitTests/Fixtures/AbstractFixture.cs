using Microsoft.AspNetCore.Components;

namespace PlayBlazor.UnitTests.Fixtures;

public abstract class AbstractFixture : ComponentBase
{
    [Parameter]
    public bool Visible { get; set; }
}
