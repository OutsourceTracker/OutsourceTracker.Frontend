using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using OutsourceTracker.Authentication;
using OutsourceTracker.BusinessUnit.Accounts;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Services;
using OutsourceTracker.Services.BusinessUnit.Accounts;
using OutsourceTracker.Services.BusinessUnit.Divisions;
using OutsourceTracker.Services.Equipment;
using OutsourceTracker.Services.Equipment.Trailers;
using OutsourceTracker.Services.Zones;

namespace OutsourceTracker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Log("=== Program.Main started ===");

                var builder = WebAssemblyHostBuilder.CreateDefault(args);
                Log("WebAssemblyHostBuilder created");

                builder.RootComponents.Add<App>("#app");
                builder.RootComponents.Add<HeadOutlet>("head::after");
                Log("Root components registered");

                // HTTP Clients
                builder.Services.AddHttpClient("API", client =>
                {
                    var url = builder.HostEnvironment.IsDevelopment()
                        ? "https://localhost:7253/"
                        : "https://api.vandersluistrucking.com/";
                    client.BaseAddress = new Uri(url);
                });
                Log("API HttpClient registered");

                builder.Services.AddHttpClient("API_Secured", client =>
                {
                    var url = builder.HostEnvironment.IsDevelopment()
                        ? "https://localhost:7253/"
                        : "https://api.vandersluistrucking.com/";
                    client.BaseAddress = new Uri(url);
                })
                .AddHttpMessageHandler<AuthHttpMessageHandler>();
                Log("API_Secured HttpClient registered");

                // Core services
                builder.Services.AddScoped<UserService>();
                builder.Services.AddScoped<AuthHttpMessageHandler>();
                builder.Services.AddScoped<ITokenService, JwtTokenService>();
                builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
                builder.Services.AddScoped<ClipboardService>();
                Log("Core services registered");

                // Business services
                builder.Services.AddScoped<TrailerService>()
                    .AddScoped<IEquipmentService<TrailerModel>>(s => s.GetRequiredService<TrailerService>());
                builder.Services.AddScoped<AccountService>()
                    .AddScoped<IAccountService>(s => s.GetRequiredService<AccountService>());
                builder.Services.AddScoped<OrganizationalUnitService>()
                    .AddScoped<IOrganizationalUnitService>(s => s.GetRequiredService<OrganizationalUnitService>());
                builder.Services.AddScoped<ZoneService>()
                    .AddScoped<IZoneService>(s => s.GetRequiredService<ZoneService>());
                Log("Business services registered");

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
                Log("MudServices + Authorization registered");

                Log("Building host...");
                var host = builder.Build();
                Log("Host built successfully");

                Log("Starting RunAsync()...");
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("=== FATAL STARTUP ERROR ===");
                Console.Error.WriteLine(ex.ToString());
                Console.Error.WriteLine("=== END FATAL ERROR ===");

                // Try to show a visible message on the loading screen
                try
                {
                    await Task.Delay(10);
                    await JSRuntimeExtensions.InvokeVoidAsync(null!, "updateLoadingStatus", "ERROR: " + ex.Message);
                }
                catch { }

                throw;
            }
        }

        private static void Log(string message)
        {
            var ts = DateTime.UtcNow.ToString("HH:mm:ss.fff");
            var msg = $"[Startup {ts}] {message}";
            Console.WriteLine(msg);

            // Also update the visible loading status (best effort)
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1);
                    await JSRuntimeExtensions.InvokeVoidAsync(null!, "updateLoadingStatus", message);
                }
                catch { /* too early */ }
            });
        }
    }
}
