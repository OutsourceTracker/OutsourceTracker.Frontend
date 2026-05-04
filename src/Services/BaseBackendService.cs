using Microsoft.AspNetCore.Components;
using OutsourceTracker.Services.DataModels;
using OutsourceTracker.Services.ModelService;
using System.Net.Http.Json;

namespace OutsourceTracker.Services;

public abstract class BaseBackendService<TID, TModel> : IDataModelService<TID, TModel>
    where TID : struct
    where TModel : class, IServiceModel<TID>   // Stronger constraint for ID access
{
    protected HttpClient HttpClient { get; }
    protected ILogger Logger { get; }
    protected NavigationManager Navigation { get; }   // Added for auth redirect support

    protected abstract string EndPoint { get; }

    protected Dictionary<TID, TModel> Cache { get; } = new Dictionary<TID, TModel>();

    protected BaseBackendService(IServiceProvider services)
    {
        var factory = services.GetRequiredService<ILoggerFactory>();
        Logger = factory.CreateLogger(GetType());

        HttpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("API");
        Navigation = services.GetRequiredService<NavigationManager>();
    }

    // ===================================================================
    // Hook points for inheriting classes
    // ===================================================================

    protected virtual Task BeforeRequestAsync(HttpRequestMessage message, CancellationToken ct) => Task.CompletedTask;
    protected virtual Task AfterRequestAsync(HttpResponseMessage response, CancellationToken ct) => Task.CompletedTask;

    protected virtual Task OnModelCreatedAsync(TModel model, CancellationToken ct) => Task.CompletedTask;
    protected virtual Task OnModelUpdatedAsync(TModel model, CancellationToken ct) => Task.CompletedTask;
    protected virtual Task OnModelDeletedAsync(TID id, CancellationToken ct) => Task.CompletedTask;

    protected virtual TModel? TransformReceivedModel(TModel? model) => model;

    protected virtual void OnCacheUpdated(TID id, TModel model) { }
    protected virtual void OnCacheRemoved(TID id) { }

    // ===================================================================
    // Core CRUD
    // ===================================================================

    public async Task<ModelResult> Create<T>(T? modelParameters = default, CancellationToken cancellationToken = default)
    {
        IModelResultBuilder r = ModelResult.Builder();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, EndPoint);
            if (modelParameters != null)
                message.Content = JsonContent.Create(modelParameters);

            await BeforeRequestAsync(message, cancellationToken);

            using var response = await SendMessage(message, cancellationToken);
            await AfterRequestAsync(response, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                IdResponse? id = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken);

                if (id == null)
                {
                    Logger.LogWarning("Create response did not contain an ID for {ModelType}", typeof(TModel).Name);
                    throw new InvalidDataException("Create response did not contain an ID");
                }

                ModelResult getResult = await GetItemRaw(id.Id, cancellationToken);

                if (!getResult.Success || getResult.Data is not TModel createdModel)
                {
                    Logger.LogWarning("Failed to retrieve created {ModelType} with ID {Id}", typeof(TModel).Name, id.Id);
                    throw new Exception($"Failed to retrieve created model with ID {id.Id}");
                }

                TModel? created = getResult.Data as TModel;
                created = TransformReceivedModel(created);

                if (created == null)
                    return r.AddError("DeserializationError", "Failed to deserialize created model").Build();

                lock (Cache)
                {
                    Cache[created.Id] = created;
                    OnCacheUpdated(created.Id, created);
                }

                await OnModelCreatedAsync(created, cancellationToken);

                Logger.LogInformation("Created {ModelType} with ID {Id} in {Elapsed}ms", typeof(TModel).Name, created.Id, stopwatch.ElapsedMilliseconds);

                return r.WithResult(created).WithSuccess().Build();
            }
            else
            {
                return await HandleErrorResponseAsync(response, r, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Failed to create {ModelType}", typeof(TModel).Name);
            return r.AddError("UnexpectedError", ex.Message).Build();
        }
    }

    public async Task<ModelResult> Update<T>(TID modelId, T modelParameters, CancellationToken cancellationToken = default)
    {
        IModelResultBuilder r = ModelResult.Builder();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Put, $"{EndPoint}/{modelId}");
            message.Content = JsonContent.Create(modelParameters);

            await BeforeRequestAsync(message, cancellationToken);

            using var response = await SendMessage(message, cancellationToken);
            await AfterRequestAsync(response, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Refresh from server (most reliable)
                var getResult = await GetItemRaw(modelId, cancellationToken);

                if (getResult.Success && getResult.Data is TModel updatedModel)
                {
                    updatedModel = TransformReceivedModel(updatedModel)!;

                    lock (Cache)
                    {
                        Cache[modelId] = updatedModel;
                        OnCacheUpdated(modelId, updatedModel);
                    }

                    await OnModelUpdatedAsync(updatedModel, cancellationToken);

                    Logger.LogInformation("Updated {ModelType} {Id} in {Elapsed}ms", typeof(TModel).Name, modelId, stopwatch.ElapsedMilliseconds);

                    return r.WithResult(updatedModel).WithSuccess().Build();
                }

                return getResult;
            }
            else
            {
                return await HandleErrorResponseAsync(response, r, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Failed to update {ModelType} {Id}", typeof(TModel).Name, modelId);
            return r.AddError("UnexpectedError", ex.Message).Build();
        }
    }

    public async Task<ModelResult> Delete(TID modelId, CancellationToken cancellationToken = default)
    {
        IModelResultBuilder r = ModelResult.Builder();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, $"{EndPoint}/{modelId}");
            await BeforeRequestAsync(message, cancellationToken);

            using var response = await SendMessage(message, cancellationToken);
            await AfterRequestAsync(response, cancellationToken);

            lock (Cache)
            {
                if (Cache.Remove(modelId))
                    OnCacheRemoved(modelId);
            }

            await OnModelDeletedAsync(modelId, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Deleted {ModelType} {Id} in {Elapsed}ms", typeof(TModel).Name, modelId, stopwatch.ElapsedMilliseconds);
                return r.WithSuccess().Build();
            }
            else
            {
                return await HandleErrorResponseAsync(response, r, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Failed to delete {ModelType} {Id}", typeof(TModel).Name, modelId);
            return r.AddError("UnexpectedError", ex.Message).Build();
        }
    }

    public async Task<ModelResult> Get(TID modelId, CancellationToken cancellationToken = default)
    {
        if (Cache.TryGetValue(modelId, out TModel? cached))
            return ModelResult.Builder().WithResult(cached).WithSuccess().Build();

        return await GetItemRaw(modelId, cancellationToken);
    }

    public async Task<ModelResult> Search<T>(T? searchParameters = default, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, EndPoint);
        using HttpResponseMessage response = await SendMessage(message, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var list = response.Content.ReadFromJsonAsAsyncEnumerable<TModel>(cancellationToken);
            var resultList = new List<TModel>();
            await foreach (TModel? item in list)
            {
                if (item == null)
                {
                    continue;
                }

                if (item is IServiceModel<TID> serviceModel)
                {
                    lock (Cache)
                    {
                        if (Cache.TryGetValue(serviceModel.Id, out TModel? cached))
                        {
                            cached.ApplyObjectToModel(item);
                            OnCacheUpdated(serviceModel.Id, cached);
                            resultList.Add(cached);
                        }
                        else
                        {
                            Cache[serviceModel.Id] = item;
                            OnCacheUpdated(serviceModel.Id, item);
                            resultList.Add(item);
                        }
                    }
                }
                else
                {
                    resultList.Add(item);
                }
            }

            return ModelResult.Builder().WithResult(resultList).WithSuccess().Build();
        }
        else
        {
            return await HandleErrorResponseAsync(response, ModelResult.Builder(), cancellationToken);
        }
    }

    // ===================================================================
    // Protected helpers
    // ===================================================================

    protected virtual async Task<HttpResponseMessage> SendMessage(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Sending {Method} to {Uri}", message.Method, message.RequestUri);
            var response = await HttpClient.SendAsync(message, cancellationToken);
            Logger.LogDebug("Received {StatusCode} from {Uri}", response.StatusCode, message.RequestUri);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while sending HTTP request");
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }
    }

    private async Task<ModelResult> GetItemRaw(TID modelId, CancellationToken cancellationToken = default)
    {
        IModelResultBuilder r = ModelResult.Builder();

        using var message = new HttpRequestMessage(HttpMethod.Get, $"{EndPoint}/{modelId}");
        await BeforeRequestAsync(message, cancellationToken);

        using var response = await SendMessage(message, cancellationToken);
        await AfterRequestAsync(response, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            TModel? model = await response.Content.ReadFromJsonAsync<TModel>(cancellationToken);
            model = TransformReceivedModel(model);

            if (model == null)
                return r.AddError("DeserializationError", "Failed to deserialize model").Build();

            lock (Cache)
            {
                Cache[modelId] = model;
                OnCacheUpdated(modelId, model);
            }

            return r.WithResult(model).WithSuccess().Build();
        }
        else
        {
            return await HandleErrorResponseAsync(response, r, cancellationToken);
        }
    }

    private async Task<ModelResult> HandleErrorResponseAsync(HttpResponseMessage response, IModelResultBuilder builder, CancellationToken ct)
    {
        string errorContent = await response.Content.ReadAsStringAsync(ct);
        Logger.LogWarning("Request failed: {StatusCode} - {Content}", response.StatusCode, errorContent);

        return builder
            .AddError("RequestFailed", $"HTTP {(int)response.StatusCode}: {errorContent}")
            .Build();
    }

    protected async Task<TModel> ForceCacheUpdateAsync(TID modelId, CancellationToken cancellationToken = default)
    {
        try
        {
            var getResult = await GetItemRaw(modelId, cancellationToken);
            if (!getResult.Success || getResult.Data is not IServiceModel<TID> model)
                throw new Exception($"Failed to refresh model {modelId} from server");

            lock (Cache)
            {
                if (Cache.TryGetValue(model.Id, out var cachedModel))
                {
                    cachedModel.ApplyObjectToModel<TModel>(model);
                    OnCacheUpdated(model.Id, cachedModel);
                    Logger.LogInformation("Cache for model {Id} updated from server", model.Id);
                    return cachedModel;
                }
                else
                {
                    Cache[model.Id] = (TModel)model;
                    OnCacheUpdated(model.Id, (TModel)model);
                    Logger.LogInformation("Model {Id} added to cache from server", model.Id);
                    return (TModel)model;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh model {Id} from server", modelId);
            throw;
        }
    }

    private class IdResponse
    {
        public TID Id { get; set; }
    }
}