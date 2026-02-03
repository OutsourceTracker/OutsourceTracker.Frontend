using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OutsourceTracker.Services;

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
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(/* builder.HostEnvironment.BaseAddress */ "https://dispatch.vandersluis.club/" ) });

            await builder.Build().RunAsync();
        }
    }
}
