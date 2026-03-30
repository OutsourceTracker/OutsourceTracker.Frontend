using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services;
using OutsourceTracker.Services.Authentication;
using OutsourceTracker.Services.ModelService;

namespace OutsourceTracker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services
                .AddScoped<AuthMessageHandler>()
                .AddHttpClient("API", client =>
                {
#if DEBUG
                    client.BaseAddress = new Uri("https://localhost:7253/");
#else
                    client.BaseAddress = new Uri("https://api.vandersluistrucking.com/");
#endif
                })
                .AddHttpMessageHandler<AuthMessageHandler>();


            builder.Services.AddHttpClient();
            
            builder.Services.AddScoped<AuthenticationStateProvider, TokenService>()
                .AddScoped<LoginService>()
                .AddAuthorizationCore();

            builder.Services.AddScoped<UserPhotoService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AppVersionService>();
            builder.Services.AddScoped<TrailerService>()
                .AddScoped<IModelCreateService<TrailerViewModel, HttpResponseMessage>>(sp => sp.GetRequiredService<TrailerService>())
                .AddScoped<IModelDeleteService<TrailerViewModel, HttpResponseMessage>>(sp => sp.GetRequiredService<TrailerService>())
                .AddScoped<IModelLookupService<TrailerViewModel>>(sp => sp.GetRequiredService<TrailerService>())
                .AddScoped<IModelUpdateService<TrailerViewModel, HttpResponseMessage>>(sp => sp.GetRequiredService<TrailerService>())
                .AddScoped<ITrackableLocationService<TrailerViewModel, HttpResponseMessage>>(sp => sp.GetRequiredService<TrailerService>());

            await builder.Build().RunAsync();
        }
    }
}
