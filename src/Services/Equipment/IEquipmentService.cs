using OutsourceTracker.Equipment.Trailers;

namespace OutsourceTracker.Services.Equipment;

public interface IEquipmentService<TModel> where TModel : class
{
    public Task<TModel?> GetAsync(Guid id, bool ignoreCache = false);

    public Task<TModel?> CreateAsync(TrailerCreateRequest model);

    public Task<bool> DeleteAsync(Guid id);

    public IAsyncEnumerable<TModel> ListAsync(object? searchQuery = null);
}
