using DaisyBlazor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PlayBlazor;
using PlayBlazor.Demo.Daisy;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddDaisyBlazor();
builder.Services.AddPlayBlazor(DaisyPlaygroundConfig.Configure);

await builder.Build().RunAsync();
