using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Database;

public sealed class CoreDbContextTransactionTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task RunInTransactionAsync_WhenOperationSavesInside_CommitsAndReturnsGeneratedId()
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        var companyId = await dbContext.RunInTransactionAsync(async cancellationToken =>
        {
            var company = new Company { Name = TransactionTestData.CommittedCompanyName };
            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync(cancellationToken);

            return company.Id;
        });

        companyId.Should().NotBeEmpty();
        (await CountCompaniesAsync(TransactionTestData.CommittedCompanyName)).Should().Be(1);
    }

    [Fact]
    public async Task RunInTransactionAsync_WhenOperationThrowsAfterSave_CommitsNothing()
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(async cancellationToken =>
        {
            dbContext.Companies.Add(new Company { Name = TransactionTestData.RolledBackCompanyName });
            await dbContext.SaveChangesAsync(cancellationToken);

            throw new TransactionTestException();
        });

        await act.Should().ThrowAsync<TransactionTestException>();
        (await CountCompaniesAsync(TransactionTestData.RolledBackCompanyName)).Should().Be(0);
    }

    [Fact]
    public async Task RunInTransactionAsync_WithUnsavedChangesBeforeCall_ThrowsWithoutRunningOperation()
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
        dbContext.Companies.Add(new Company { Name = TransactionTestData.UnsavedCompanyName });
        var isOperationRun = false;

        var act = () => dbContext.RunInTransactionAsync(_ =>
        {
            isOperationRun = true;

            return Task.CompletedTask;
        });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(PersistenceErrors.UnsavedChangesBeforeTransaction().Message);
        isOperationRun.Should().BeFalse();
    }

    [Fact]
    public async Task RunInTransactionAsync_WhenOperationLeavesUnsavedChanges_ThrowsAndCommitsNothing()
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(_ =>
        {
            dbContext.Companies.Add(new Company { Name = TransactionTestData.UnsavedAfterOperationCompanyName });

            return Task.CompletedTask;
        });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(PersistenceErrors.UnsavedChangesAfterOperation().Message);
        (await CountCompaniesAsync(TransactionTestData.UnsavedAfterOperationCompanyName)).Should().Be(0);
    }

    [Fact]
    public async Task RunInTransactionAsync_WhenOperationCommitsTransaction_ThrowsAndSavesNothingAfterIt()
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(async cancellationToken =>
        {
            dbContext.Companies.Add(new Company { Name = TransactionTestData.EndedTransactionCompanyName });
            await dbContext.Database.CommitTransactionAsync(cancellationToken);
        });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(PersistenceErrors.TransactionEndedByOperation().Message);
        (await CountCompaniesAsync(TransactionTestData.EndedTransactionCompanyName)).Should().Be(0);
    }

    private async Task<int> CountCompaniesAsync(string name)
    {
        using var scope = fixture.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Companies
            .CountAsync(company => company.Name == name);
    }
}
