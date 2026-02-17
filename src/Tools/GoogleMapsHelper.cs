using Microsoft.JSInterop;
using OutsourceTracker.Services;
using System.Dynamic;

namespace OutsourceTracker.Tools;

public class GoogleMapsHelper : IMapService, IDisposable
{
    private IJSRuntime JS { get; }
    private bool MapInitialized = false;
    private readonly string ContainerId = "map";
    private readonly string MapId = "91655a72ee45e0e184bfe567";
    private DotNetObjectReference<GoogleMapsHelper> _dotNetRef;

    public GoogleMapsHelper(IJSRuntime js)
    {
        JS = js;
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    public async Task<bool> InitializeMapAsync(double? initLat = null, double? initLng = null, object? additionalArgs = null)
    {
        dynamic args = new ExpandoObject();
        if (initLat.HasValue && initLng.HasValue)
        {
            args.center = new { lat = initLat.Value, lng = initLng.Value };
        }
        else
        {
            args.center = new { lat = 47.23299, lng = -122.22583 };
        }
        args.zoom = 15;
        args.mapTypeId = "hybrid";
        args.mapId = MapId;


        // Merge additionalArgs if provided
        if (additionalArgs != null)
        {
            var additionalDict = (IDictionary<string, object>)additionalArgs;
            var argsDict = (IDictionary<string, object>)args;
            foreach (var kvp in additionalDict)
            {
                argsDict[kvp.Key] = kvp.Value;
            }
        }

        if (await JS.InvokeAsync<bool>("map.initialize", new object[] { ContainerId, (object)args }))
        {
            MapInitialized = true;
            return true;
        }

        return false;
    }

    public async Task<string> CreateMapMarker(string markerId, string title, double lat, double lng, double accuracy = 0, string? infoHtml = null)
    {
        // No need for MapInitialized check here; JS handles pending markers
        return await JS.InvokeAsync<string>("map.createMapMarker", markerId, title, lat, lng, accuracy, infoHtml);
    }

    public async Task DeleteMapMarker(string markerId)
    {
        if (!MapInitialized)
        {
            // Optionally handle, but JS can delete from storage
            return;
        }
        await JS.InvokeVoidAsync("map.deleteMapMarker", markerId);
    }

    public async Task EditMapMarker(string markerId, string title, double lat, double lng, double accuracy = 0, string? infoHtml = null)
    {
        // No need for MapInitialized check; JS handles updates to params
        await JS.InvokeVoidAsync("map.editMapMarker", markerId, title, lat, lng, accuracy, infoHtml);
    }

    public async Task FocusMapMarker(string markerId, int? zoom = null)
    {
        if (!MapInitialized)
        {
            throw new InvalidOperationException("Map not initialized");
        }
        await JS.InvokeVoidAsync("map.focusMapMarker", markerId, zoom);
    }

    public async Task ClearMapMarkers()
    {
        if (!MapInitialized)
        {
            throw new InvalidOperationException("Map not initialized");
        }

        await JS.InvokeVoidAsync("map.clearMapMarkers");
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }
}