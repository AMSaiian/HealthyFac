using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class SearchWorkOrdersTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithTitleFilter_MatchesTheStartOfTheTitle()
    {
        var set = await ArrangeAsync();

        var titles = await SearchTitlesAsync(set, new WorkOrderGridFilter { Title = new TextFilter { Value = "Bra" } });

        titles.Should().Equal(WorkOrderTestData.BravoTitle);
    }

    [Fact]
    public async Task Search_WithStatusFilter_UsesTheCurrentRow()
    {
        var set = await ArrangeAsync();

        var titles = await SearchTitlesAsync(
            set,
            new WorkOrderGridFilter
                { Statuses = new SetFilter<WorkOrderStatus> { Values = [WorkOrderStatus.Staged] } });

        titles.Should().BeEquivalentTo(WorkOrderTestData.AlphaTitle, WorkOrderTestData.BravoTitle);
    }

    [Fact]
    public async Task Search_WithPriorityAndCategoryFilters_ReturnsMatchingRows()
    {
        var set = await ArrangeAsync();

        var byPriority = await SearchTitlesAsync(
            set,
            new WorkOrderGridFilter
            {
                Priorities = new SetFilter<WorkOrderPriority> { Values = [WorkOrderPriority.Critical] },
            });
        var byCategory = await SearchTitlesAsync(
            set,
            new WorkOrderGridFilter
            {
                Categories = new SetFilter<WorkOrderCategory> { Values = [WorkOrderCategory.Repair] },
            });

        byPriority.Should().Equal(WorkOrderTestData.CharlieTitle);
        byCategory.Should().Equal(WorkOrderTestData.BravoTitle);
    }

    [Fact]
    public async Task Search_WithAssigneeAndMachineFilters_ReturnsMatchingRows()
    {
        var set = await ArrangeAsync();

        var byAssignee = await SearchTitlesAsync(
            set,
            new WorkOrderGridFilter { Assignee = new IdSetFilter { Ids = [set.Tenant.EmployeeId] } });
        var byMachine = await SearchTitlesAsync(
            set,
            new WorkOrderGridFilter { Machine = new IdSetFilter { Ids = [set.FirstMachineId] } });

        byAssignee.Should()
            .BeEquivalentTo(
                WorkOrderTestData.AlphaTitle,
                WorkOrderTestData.BravoTitle,
                WorkOrderTestData.DeltaTitle);
        byMachine.Should().BeEquivalentTo(WorkOrderTestData.AlphaTitle, WorkOrderTestData.DeltaTitle);
    }

    [Fact]
    public async Task Search_WithPlannedStartRange_UsesTheStartOfTheCurrentRow()
    {
        var set = await ArrangeAsync();
        var filter = new WorkOrderGridFilter
        {
            PlannedStart = new DateRangeFilter
            {
                From = ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "12:00").UtcDateTime,
                To = ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "14:00").UtcDateTime,
            },
        };

        var titles = await SearchTitlesAsync(set, filter);

        titles.Should().Equal(WorkOrderTestData.DeltaTitle);
    }

    [Fact]
    public async Task Search_WithIsDelayedFilter_FlagsOnlyStagedWorkOrdersBeforeNow()
    {
        var set = await ArrangeAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "14:00"));

        var delayed = await SearchTitlesAsync(set, new WorkOrderGridFilter { IsDelayed = true });
        var notDelayed = await SearchTitlesAsync(set, new WorkOrderGridFilter { IsDelayed = false });
        var rows = await _fixture.SearchWorkOrdersAsync(
            set.Tenant.CompanyId,
            set.Tenant.IdentityUserId,
            WorkOrderTestData.SearchRequest(set.Tenant.CompanyId, AccessScope.Company));

        delayed.Should().BeEquivalentTo(WorkOrderTestData.AlphaTitle, WorkOrderTestData.BravoTitle);
        notDelayed.Should()
            .BeEquivalentTo(
                WorkOrderTestData.CharlieTitle, WorkOrderTestData.DeltaTitle, WorkOrderTestData.OwnBacklogTitle);
        rows.Items.Where(row => row.IsDelayed)
            .Select(row => row.Title)
            .Should()
            .BeEquivalentTo(WorkOrderTestData.AlphaTitle, WorkOrderTestData.BravoTitle);
    }

    [Fact]
    public async Task Search_WithEachSortColumn_OrdersTheRows()
    {
        var set = await ArrangeAsync();

        var byTitle = await SortedTitlesAsync(set, WorkOrderSortColumn.Title, SortDirection.Descending);
        var byPriority = await SortedTitlesAsync(set, WorkOrderSortColumn.Priority, SortDirection.Ascending);
        var byStart = await SortedTitlesAsync(set, WorkOrderSortColumn.PlannedStart, SortDirection.Ascending);
        var byCreated = await SortedTitlesAsync(set, WorkOrderSortColumn.CreatedAt, SortDirection.Ascending);

        byTitle.Should()
            .Equal(
                WorkOrderTestData.OwnBacklogTitle,
                WorkOrderTestData.DeltaTitle,
                WorkOrderTestData.CharlieTitle,
                WorkOrderTestData.BravoTitle,
                WorkOrderTestData.AlphaTitle);
        byPriority.Should()
            .Equal(
                WorkOrderTestData.AlphaTitle,
                WorkOrderTestData.OwnBacklogTitle,
                WorkOrderTestData.DeltaTitle,
                WorkOrderTestData.BravoTitle,
                WorkOrderTestData.CharlieTitle);
        byStart.Take(count: 3)
            .Should()
            .Equal(WorkOrderTestData.AlphaTitle, WorkOrderTestData.BravoTitle, WorkOrderTestData.DeltaTitle);
        byCreated.Should()
            .Equal(
                WorkOrderTestData.AlphaTitle,
                WorkOrderTestData.BravoTitle,
                WorkOrderTestData.CharlieTitle,
                WorkOrderTestData.DeltaTitle,
                WorkOrderTestData.OwnBacklogTitle);
    }

    [Fact]
    public async Task Search_InOwnScope_ReturnsAssignedAndOwnBacklogRowsOnly()
    {
        var set = await ArrangeAsync();

        var response = await _fixture.SearchWorkOrdersAsync(
            set.Tenant.CompanyId,
            set.Tenant.IdentityUserId,
            WorkOrderTestData.SearchRequest(set.Tenant.CompanyId, AccessScope.Own));

        response.TotalCount.Should().Be(expected: 4);
        response.Items.Select(row => row.Title)
            .Should()
            .BeEquivalentTo(
                WorkOrderTestData.AlphaTitle,
                WorkOrderTestData.BravoTitle,
                WorkOrderTestData.DeltaTitle,
                WorkOrderTestData.OwnBacklogTitle);
    }

    [Fact]
    public async Task Search_InOwnScopeWithoutEmployee_ReturnsNoRowsEvenForOwnBacklog()
    {
        var set = await ArrangeAsync();
        var userWithoutEmployee = Guid.CreateVersion7();
        await _fixture.CreateWorkOrderAsync(
            set.Tenant.CompanyId,
            userWithoutEmployee,
            WorkOrderTestData.NewRequest(set.Tenant.CompanyId, AccessScope.Company));

        var response = await _fixture.SearchWorkOrdersAsync(
            set.Tenant.CompanyId,
            userWithoutEmployee,
            WorkOrderTestData.SearchRequest(set.Tenant.CompanyId, AccessScope.Own));

        response.TotalCount.Should().Be(expected: 0);
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_SortedByIsDelayed_PutsTheDelayedOnesFirstWhenDescending()
    {
        var set = await ArrangeAsync();

        var descending = await SortedTitlesAsync(set, WorkOrderSortColumn.IsDelayed, SortDirection.Descending);
        var ascending = await SortedTitlesAsync(set, WorkOrderSortColumn.IsDelayed, SortDirection.Ascending);

        descending[index: 0].Should().Be(WorkOrderTestData.AlphaTitle);
        ascending[^1].Should().Be(WorkOrderTestData.AlphaTitle);
    }

    [Fact]
    public async Task SearchBatch_WithSeveralSearches_ReturnsTheSinglePagesInTheOrderOfTheSearches()
    {
        var set = await ArrangeAsync();
        var filters = new[]
        {
            new WorkOrderGridFilter
                { Statuses = new SetFilter<WorkOrderStatus> { Values = [WorkOrderStatus.InProgress] } },
            new WorkOrderGridFilter { Statuses = new SetFilter<WorkOrderStatus> { Values = [WorkOrderStatus.Staged] } },
            new WorkOrderGridFilter { Title = new TextFilter { Value = "Bra" } },
        };
        var single = new List<GridResponse<WorkOrderRowResponse>>();
        foreach (var filter in filters)
        {
            single.Add(
                await _fixture.SearchWorkOrdersAsync(
                    set.Tenant.CompanyId,
                    set.Tenant.IdentityUserId,
                    WorkOrderTestData.SearchRequest(set.Tenant.CompanyId, AccessScope.Company, filter)));
        }

        var batch = await _fixture.SearchWorkOrderBatchAsync(
            set.Tenant.CompanyId,
            set.Tenant.IdentityUserId,
            WorkOrderTestData.BatchRequest(set.Tenant.CompanyId, AccessScope.Company, filters));

        batch.Pages.Select(page => page.TotalCount).Should().Equal(single.Select(page => page.TotalCount));
        batch.Pages.Select(page => page.Items.Select(row => row.Title))
            .Should()
            .BeEquivalentTo(
                single.Select(page => page.Items.Select(row => row.Title)), options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task SearchBatch_InOwnScope_AppliesTheScopeToEverySearch()
    {
        var set = await ArrangeAsync();
        var filters = new[]
            { new WorkOrderGridFilter(), new WorkOrderGridFilter { Title = new TextFilter { Value = "Cha" } } };

        var batch = await _fixture.SearchWorkOrderBatchAsync(
            set.Tenant.CompanyId,
            set.Tenant.IdentityUserId,
            WorkOrderTestData.BatchRequest(set.Tenant.CompanyId, AccessScope.Own, filters));

        batch.Pages[index: 0].TotalCount.Should().Be(expected: 4);
        batch.Pages[index: 0].Items.Select(row => row.Title).Should().NotContain(WorkOrderTestData.CharlieTitle);
        batch.Pages[index: 1].Items.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(WorkOrderTestData.InvalidBatchSizes), MemberType = typeof(WorkOrderTestData))]
    public async Task SearchBatch_WithTooFewOrTooManySearches_ThrowsValidation(int searchCount)
    {
        var set = await ArrangeAsync();
        var filters = Enumerable.Range(start: 0, searchCount).Select(_ => new WorkOrderGridFilter()).ToArray();

        var act = () => _fixture.SearchWorkOrderBatchAsync(
            set.Tenant.CompanyId,
            set.Tenant.IdentityUserId,
            WorkOrderTestData.BatchRequest(set.Tenant.CompanyId, AccessScope.Company, filters));

        await act.Should()
            .ThrowAsync<AppValidationException>()
            .WithMessage($"*{WorkOrderErrors.Validation.BatchSizeInvalid}*");
    }

    private async Task<IReadOnlyList<string>> SearchTitlesAsync(WorkOrderSet set, WorkOrderGridFilter filter)
    {
        var response = await _fixture.SearchWorkOrdersAsync(
            set.Tenant.CompanyId,
            set.Tenant.IdentityUserId,
            WorkOrderTestData.SearchRequest(set.Tenant.CompanyId, AccessScope.Company, filter));

        return response.Items.Select(row => row.Title).ToList();
    }

    private async Task<IReadOnlyList<string>> SortedTitlesAsync(
        WorkOrderSet set,
        WorkOrderSortColumn column,
        SortDirection direction
    )
    {
        var response = await _fixture.SearchWorkOrdersAsync(
            set.Tenant.CompanyId,
            set.Tenant.IdentityUserId,
            WorkOrderTestData.SearchRequest(
                set.Tenant.CompanyId,
                AccessScope.Company,
                sort: WorkOrderTestData.SortBy(column, direction)));

        return response.Items.Select(row => row.Title).ToList();
    }

    private async Task<WorkOrderSet> ArrangeAsync()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        var firstMachineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            WorkOrderTestData.MachineName,
            WorkOrderTestData.MachineSerialNumber);
        var secondMachineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            WorkOrderTestData.SecondMachineName,
            WorkOrderTestData.SecondMachineSerialNumber);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        var supervisorId = Guid.CreateVersion7();

        await CreateAsync(
            tenant, supervisorId, WorkOrderTestData.AlphaTitle, WorkOrderPriority.Low,
            WorkOrderCategory.Inspection, firstMachineId, time: "09:00");
        await CreateAsync(
            tenant, supervisorId, WorkOrderTestData.BravoTitle, WorkOrderPriority.High,
            WorkOrderCategory.Repair, secondMachineId, time: "10:00");
        await CreateAsync(
            tenant, supervisorId, WorkOrderTestData.CharlieTitle, WorkOrderPriority.Critical,
            WorkOrderCategory.Preventive, machineId: null, time: null);
        var delta = await CreateAsync(
            tenant, supervisorId, WorkOrderTestData.DeltaTitle, WorkOrderPriority.Medium,
            WorkOrderCategory.Other, firstMachineId, time: "13:00");
        await _fixture.ChangeStatusAsync(tenant.CompanyId, supervisorId, delta, WorkOrderStatus.InProgress);
        await CreateAsync(
            tenant, tenant.IdentityUserId, WorkOrderTestData.OwnBacklogTitle, WorkOrderPriority.Low,
            WorkOrderCategory.Other, machineId: null, time: null);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "09:30"));

        return new WorkOrderSet { Tenant = tenant, FirstMachineId = firstMachineId };
    }

    private async Task<Guid> CreateAsync(
        ShiftTenant tenant,
        Guid actorUserId,
        string title,
        WorkOrderPriority priority,
        WorkOrderCategory category,
        Guid? machineId,
        string? time
    )
    {
        var request = WorkOrderTestData.Described(
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                actorUserId == tenant.IdentityUserId ? AccessScope.Own : AccessScope.Company,
                machineId,
                time is null ? null : tenant.EmployeeId,
                time is null ? null : ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)),
            title,
            priority,
            category);
        var created = await _fixture.CreateWorkOrderAsync(tenant.CompanyId, actorUserId, request);

        return created.Id;
    }

    private sealed record WorkOrderSet
    {
        public required ShiftTenant Tenant { get; init; }

        public required Guid FirstMachineId { get; init; }
    }
}
