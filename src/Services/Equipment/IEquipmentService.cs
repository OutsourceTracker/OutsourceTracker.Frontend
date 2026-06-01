using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Services.DataModels;
using OutsourceTracker.Services.ModelService;

namespace OutsourceTracker.Services.Equipment;

public interface IEquipmentService<TModel> where TModel : class
{
    public Task<TModel?> GetAsync(Guid id, bool ignoreCache = false);

    public Task<TModel?> CreateAsync(TrailerCreateRequest model);

    /// <summary>
    /// Creates multiple trailers in a single request (bulk operation).
    /// Returns details about successes and failures.
    /// </summary>
    public Task<BulkCreateResult<TrailerModel>?> CreateManyAsync(IEnumerable<TrailerCreateRequest> models);

    /// <summary>
    /// Deletes multiple trailers in a single request.
    /// </summary>
    public Task<BulkDeleteResult?> DeleteManyAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Applies the same changes to multiple trailers in a single request.
    /// </summary>
    public Task<BulkUpdateResult<TrailerModel>?> UpdateManyAsync(IEnumerable<Guid> ids, IDictionary<string, object> changes);

    public Task<bool> DeleteAsync(Guid id);

    public IAsyncEnumerable<TModel> List(object? searchQuery = null);
}
