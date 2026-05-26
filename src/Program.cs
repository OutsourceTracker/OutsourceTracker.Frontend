using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using OutsourceTracker.Authentication;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Services;
using OutsourceTracker.Services.Equipment;
using OutsourceTracker.Services.Equipment.Trailers;

namespace OutsourceTracker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddHttpClient("API", client =>
            {
                if (builder.HostEnvironment.IsDevelopment())
                {
                    client.BaseAddress = new Uri("https://localhost:7253/");
                }
                else
                {
                    client.BaseAddress = new Uri("https://api.vandersluistrucking.com/");
                }
            });

            builder.Services.AddHttpClient("API_Secured", client =>
            {
                if (builder.HostEnvironment.IsDevelopment())
                {
                    client.BaseAddress = new Uri("https://localhost:7253/");
                }
                else
                {
                    client.BaseAddress = new Uri("https://api.vandersluistrucking.com/");
                }
            })
            .AddHttpMessageHandler<AuthHttpMessageHandler>();

            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AuthHttpMessageHandler>();
            builder.Services.AddScoped<ITokenService, JwtTokenService>();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            builder.Services.AddScoped<ClipboardService>();
            builder.Services.AddScoped<TrailerService>()
                .AddScoped<IEquipmentService<TrailerModel>>(s => s.GetRequiredService<TrailerService>());
            builder.Services.AddMemoryCache();
            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
                config.SnackbarConfiguration.PreventDuplicates = true;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.MaxDisplayedSnackbars = 5;
                config.SnackbarConfiguration.VisibleStateDuration = 4000;
                config.SnackbarConfiguration.HideTransitionDuration = 300;
                config.SnackbarConfiguration.ShowTransitionDuration = 300;
            });
            builder.Services.AddAuthorizationCore();

            await builder.Build().RunAsync();
        }
    }
}
