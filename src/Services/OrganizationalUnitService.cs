using OutsourceTracker.BusinessUnit.Divisions;

namespace OutsourceTracker.Services;

public class OrganizationalUnitService : BaseBackendService<Guid, OrganizationalUnit>
{
    protected override string EndPoint => "OU";

    public OrganizationalUnitService(IServiceProvider services) : base(services)
    {
    }

    
}
