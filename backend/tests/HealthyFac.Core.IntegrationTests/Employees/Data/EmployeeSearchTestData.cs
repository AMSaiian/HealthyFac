using HealthyFac.Core.Application.Employees.SearchEmployeeInvites;
using HealthyFac.Core.Application.Employees.SearchEmployees;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.Employees.Data;

public static class EmployeeSearchTestData
{
    public const string OtherCompanyName = "Other Plant";
    public const string AnnEmail = "ann.search@plant.com";
    public const string BobEmail = "bob.search@plant.com";
    public const string CaraEmail = "cara.search@plant.com";
    public const string OtherEmail = "other.search@other.com";
    public const string PendingEmail = "pending.search@plant.com";
    public const string ExpiredEmail = "expired.search@plant.com";
    public const string AcceptedEmail = "accepted.search@plant.com";
    public const string OtherInviteEmail = "other.invite@other.com";
    public const string AnnFirstName = "Ann";
    public const string BobFirstName = "Bob";
    public const string CaraFirstName = "Cara";
    public const string OtherFirstName = "Zed";
    public const string LastName = "Searcher";
    public const int PageLimit = 50;

    public static SearchEmployeesRequest EmployeeRequest(
        Guid companyId,
        EmployeeGridFilter? filter = null,
        GridSort<EmployeeSortColumn>? sort = null
    ) =>
        new()
        {
            CompanyId = companyId,
            Offset = 0,
            Limit = PageLimit,
            Sort = sort,
            Filter = filter ?? new EmployeeGridFilter(),
        };

    public static SearchEmployeeInvitesRequest InviteRequest(
        Guid companyId,
        GridSort<EmployeeInviteSortColumn>? sort = null
    ) =>
        new()
        {
            CompanyId = companyId,
            Offset = 0,
            Limit = PageLimit,
            Sort = sort,
            Filter = new EmployeeInviteGridFilter(),
        };

    public static TextFilter Text(string value) => new() { Value = value };
}
