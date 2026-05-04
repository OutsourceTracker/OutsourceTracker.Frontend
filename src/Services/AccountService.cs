using OutsourceTracker.BusinessUnit.Accounts;

namespace OutsourceTracker.Services;

public class AccountService : BaseBackendService<Guid, OrganizationalAccount>
{
    protected override string EndPoint => "Account";

    public AccountService(IServiceProvider services) : base(services)
    {
    }
}
