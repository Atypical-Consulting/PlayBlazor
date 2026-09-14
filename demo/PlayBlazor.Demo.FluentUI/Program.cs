using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using PlayBlazor;
using PlayBlazor.Demo.FluentUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddFluentUIComponents();
builder.Services.AddPlayBlazor(FluentPlaygroundConfig.Configure);

await builder.Build().RunAsync();
