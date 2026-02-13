using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OutsourceTracker.Services;
using OutsourceTracker.Tools;

namespace OutsourceTracker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped<TrailerService>();
            builder.Services.AddScoped<IMapTool, GoogleMapsHelper>();
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://api.vandersluistrucking.com/") });

            await builder.Build().RunAsync();
        }
    }
}
