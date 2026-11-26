using System.Data;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Testing.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Database;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.Persistence)]
public sealed class CoreDbContextTransactionTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task RunInTransactionAsync_WhenOperationSavesInside_CommitsAndReturnsGeneratedId()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        var companyId = await dbContext.RunInTransactionAsync(
            async cancellationToken =>
            {
                var company = new Company
                {
                    Name = TransactionTestData.CommittedCompanyName, TimeZoneId = TransactionTestData.CompanyTimeZoneId,
                };

                dbContext.Companies.Add(company);
                await dbContext.SaveChangesAsync(cancellationToken);

                return company.Id;
            }, TestContext.Current.CancellationToken);

        var companyCount = await CountCompaniesAsync(TransactionTestData.CommittedCompanyName);

        companyId.Should().NotBeEmpty();
        companyCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task RunInTransactionAsync_WhenOperationThrowsAfterSave_CommitsNothing()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(async cancellationToken =>
        {
            dbContext.Companies.Add(
                new Company
                {
                    Name = TransactionTestData.RolledBackCompanyName,
                    TimeZoneId = TransactionTestData.CompanyTimeZoneId,
                });

            await dbContext.SaveChangesAsync(cancellationToken);

            throw new TransactionTestException();
        });

        await act.Should().ThrowAsync<TransactionTestException>();

        var companyCount = await CountCompaniesAsync(TransactionTestData.RolledBackCompanyName);

        companyCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunInTransactionAsync_WithUnsavedChangesBeforeCall_ThrowsWithoutRunningOperation()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        dbContext.Companies.Add(
            new Company
            {
                Name = TransactionTestData.UnsavedCompanyName, TimeZoneId = TransactionTestData.CompanyTimeZoneId,
            });

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
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(_ =>
        {
            dbContext.Companies.Add(
                new Company
                {
                    Name = TransactionTestData.UnsavedAfterOperationCompanyName,
                    TimeZoneId = TransactionTestData.CompanyTimeZoneId,
                });

            return Task.CompletedTask;
        });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(PersistenceErrors.UnsavedChangesAfterOperation().Message);

        var companyCount = await CountCompaniesAsync(TransactionTestData.UnsavedAfterOperationCompanyName);

        companyCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunInTransactionAsync_WhenOperationCommitsTransaction_ThrowsAndSavesNothingAfterIt()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(async cancellationToken =>
        {
            dbContext.Companies.Add(
                new Company
                {
                    Name = TransactionTestData.EndedTransactionCompanyName,
                    TimeZoneId = TransactionTestData.CompanyTimeZoneId,
                });

            await dbContext.Database.CommitTransactionAsync(cancellationToken);
        });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(PersistenceErrors.TransactionEndedByOperation().Message);

        var companyCount = await CountCompaniesAsync(TransactionTestData.EndedTransactionCompanyName);

        companyCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunInTransactionAsync_WithSerializable_RunsSerializableTransaction()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var isolation = await dbContext.RunInTransactionAsync(
            _ => ReadIsolationAsync(dbContext), IsolationLevel.Serializable, TestContext.Current.CancellationToken);

        isolation.Should().Be(TransactionTestData.SerializableIsolation);
    }

    [Fact]
    public async Task RunInTransactionAsync_WithoutLevel_RunsReadCommittedTransaction()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var isolation = await dbContext.RunInTransactionAsync(
            _ => ReadIsolationAsync(dbContext), TestContext.Current.CancellationToken);

        isolation.Should().Be(TransactionTestData.ReadCommittedIsolation);
    }

    [Fact]
    public async Task RunInTransactionAsync_InsideOpenTransaction_JoinsWithoutCommit()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var isTransactionShared = false;

        var act = () => dbContext.RunInTransactionAsync(async outerToken =>
        {
            var outerTransaction = dbContext.Database.CurrentTransaction;

            await dbContext.RunInTransactionAsync(
                async innerToken =>
                {
                    isTransactionShared = dbContext.Database.CurrentTransaction == outerTransaction;

                    dbContext.Companies.Add(
                        new Company
                        {
                            Name = TransactionTestData.JoinedRolledBackCompanyName,
                            TimeZoneId = TransactionTestData.CompanyTimeZoneId,
                        });

                    await dbContext.SaveChangesAsync(innerToken);
                }, outerToken);

            throw new TransactionTestException();
        });

        await act.Should().ThrowAsync<TransactionTestException>();

        var companyCount = await CountCompaniesAsync(TransactionTestData.JoinedRolledBackCompanyName);

        isTransactionShared.Should().BeTrue();
        companyCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunInTransactionAsync_InsideOpenTransaction_KeepsTrackedEntities()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var state = await dbContext.RunInTransactionAsync(
            async outerToken =>
            {
                var company = new Company
                {
                    Name = TransactionTestData.TrackedCompanyName, TimeZoneId = TransactionTestData.CompanyTimeZoneId,
                };

                dbContext.Companies.Add(company);
                await dbContext.SaveChangesAsync(outerToken);
                await dbContext.RunInTransactionAsync(_ => Task.CompletedTask, outerToken);

                return dbContext.Entry(company).State;
            }, TestContext.Current.CancellationToken);

        state.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task RunInTransactionAsync_InsideOpenTransactionWithOtherLevel_Throws()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(
            outerToken => dbContext.RunInTransactionAsync(
                _ => Task.CompletedTask, IsolationLevel.Serializable, outerToken),
            IsolationLevel.ReadCommitted);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                PersistenceErrors
                    .IsolationLevelDiffersFromOpenTransaction(IsolationLevel.ReadCommitted, IsolationLevel.Serializable)
                    .Message);
    }

    [Fact]
    public async Task RunInTransactionAsync_InsideOpenTransactionWithUnsavedChanges_ThrowsWithoutRunningOperation()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var isOperationRun = false;

        var act = () => dbContext.RunInTransactionAsync(outerToken =>
        {
            dbContext.Companies.Add(
                new Company
                {
                    Name = TransactionTestData.JoinedUnsavedCompanyName,
                    TimeZoneId = TransactionTestData.CompanyTimeZoneId,
                });

            return dbContext.RunInTransactionAsync(
                _ =>
                {
                    isOperationRun = true;

                    return Task.CompletedTask;
                }, outerToken);
        });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(PersistenceErrors.UnsavedChangesBeforeTransaction().Message);

        isOperationRun.Should().BeFalse();
    }

    [Fact]
    public async Task RunInTransactionAsync_WhenJoinedOperationLeavesUnsavedChanges_Throws()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(outerToken => dbContext.RunInTransactionAsync(
            _ =>
            {
                dbContext.Companies.Add(
                    new Company
                    {
                        Name = TransactionTestData.JoinedUnsavedCompanyName,
                        TimeZoneId = TransactionTestData.CompanyTimeZoneId,
                    });

                return Task.CompletedTask;
            }, outerToken));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(PersistenceErrors.UnsavedChangesAfterOperation().Message);
    }

    [Fact]
    public async Task RunInTransactionAsync_WhenJoinedOperationCommits_Throws()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var act = () => dbContext.RunInTransactionAsync(outerToken => dbContext.RunInTransactionAsync(
            innerToken => dbContext.Database.CommitTransactionAsync(innerToken), outerToken));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(PersistenceErrors.TransactionEndedByOperation().Message);
    }

    private static async Task<string> ReadIsolationAsync(CoreDbContext dbContext)
    {
        var isolations = await dbContext.Database
            .SqlQueryRaw<string>(TransactionTestData.ShowIsolationSql)
            .ToListAsync();

        return isolations.Single();
    }

    private async Task<int> CountCompaniesAsync(string name)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Companies
            .CountAsync(company => company.Name == name);
    }
}
