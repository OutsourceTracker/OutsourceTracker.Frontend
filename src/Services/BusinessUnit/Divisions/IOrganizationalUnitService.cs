using OutsourceTracker.BusinessUnit.Divisions;

namespace OutsourceTracker.Services.BusinessUnit.Divisions;

public interface IOrganizationalUnitService
{
    Task<List<OrganizationalUnit>> GetAllAsync();

    Task<OrganizationalUnit?> GetByIdAsync(Guid id);

    Task<OrganizationalUnit?> CreateAsync(OrganizationalUnitCreateModel model);

    Task<OrganizationalUnit?> UpdateAsync(Guid id, OrganizationalUnitCreateModel model);

    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Triggers a full recalculation of TotalAccounts on the server.
    /// Returns the number of OUs whose counts were updated.
    /// </summary>
    Task<int> RecalculateAccountCountsAsync();
}

/// <summary>
/// DTO matching the backend OUCreateModel.
/// </summary>
public class OrganizationalUnitCreateModel
{
    public string ShortCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
