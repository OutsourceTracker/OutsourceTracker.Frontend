using OutsourceTracker.BusinessUnit.Accounts;

namespace OutsourceTracker.Services.BusinessUnit.Accounts;

public interface IAccountService
{
    Task<List<OrganizationalAccount>> GetAllAsync();

    Task<OrganizationalAccount?> GetByIdAsync(Guid id);

    Task<OrganizationalAccount?> CreateAsync(AccountCreateModel model);

    Task<OrganizationalAccount?> UpdateAsync(Guid id, AccountCreateModel model);

    Task<bool> DeleteAsync(Guid id);
}

/// <summary>
/// DTO matching the backend AccountCreateModel for create/update operations.
/// </summary>
public class AccountCreateModel
{
    public string ShortCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CostCenter { get; set; }
    public string? GroupEmail { get; set; }
    public string? Address { get; set; }
    public Guid OUID { get; set; }
}
