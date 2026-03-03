using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OutsourceTracker.Equipment;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services;
using OutsourceTracker.Services.ModelService;
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

            builder.Services.AddMsalAuthentication(options =>
            {
                builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
                options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
                options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");
                options.ProviderOptions.DefaultAccessTokenScopes.Add("email");
                options.ProviderOptions.DefaultAccessTokenScopes.Add("api://51032fa1-b18c-464f-9a43-34819040dfa7/access_as_user");
                options.ProviderOptions.LoginMode = "redirect";
                options.ProviderOptions.Cache.StoreAuthStateInCookie = true;
                options.ProviderOptions.Authentication.PostLogoutRedirectUri = "/login";
            });

            builder.Services
                .AddScoped<ClientHeaderHandler>()
                .AddHttpClient("API", client =>
                {

                    //if (builder.HostEnvironment.IsDevelopment())
                    //{
                    //    client.BaseAddress = new Uri("https://localhost:7253/");
                    //}
                    //else
                    //{
                    //    client.BaseAddress = new Uri("https://api.vandersluistrucking.com/");
                    //}
                    client.BaseAddress = new Uri("https://api.vandersluistrucking.com/");
                })
                .AddHttpMessageHandler<ClientHeaderHandler>();

            builder.Services.AddHttpClient("Graph", client =>
            {
                client.BaseAddress = new Uri("https://graph.microsoft.com");
            });
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<UserPhotoService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AppVersionService>();
            builder.Services.AddScoped<TrailerService>()
                .AddScoped<IModelCreateService<TrailerViewModel, HttpResponseMessage>>(sp => sp.GetRequiredService<TrailerService>())
                .AddScoped<IModelDeleteService<TrailerViewModel, HttpResponseMessage>>(sp => sp.GetRequiredService<TrailerService>())
                .AddScoped<IModelLookupService<TrailerViewModel>>(sp => sp.GetRequiredService<TrailerService>())
                .AddScoped<IModelUpdateService<TrailerViewModel, HttpResponseMessage>>(sp => sp.GetRequiredService<TrailerService>())
                .AddScoped<ITrackableLocationService<TrailerViewModel, HttpResponseMessage>>(sp => sp.GetRequiredService<TrailerService>());
            builder.Services.AddScoped<IMapService, GoogleMapsHelper>();
            await builder.Build().RunAsync();
        }
    }
}
