using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using HealthyFac.Testing.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.WorkOrders.Data;

public static class WorkOrderApiFixtureExtensions
{
    extension(CoreApiFixture fixture)
    {
        public TestTokenClaims CreateWorkerClaims(Guid subject) =>
            fixture.CreateShiftClaims(
                subject,
                WorkOrderApiTestData.ReadPermission,
                WorkOrderApiTestData.WritePermission);

        public TestTokenClaims CreateSupervisorOrderClaims(Guid subject) =>
            fixture.CreateShiftClaims(
                subject,
                WorkOrderApiTestData.ReadPermission,
                WorkOrderApiTestData.WritePermission,
                WorkOrderApiTestData.ManagePermission,
                WorkOrderApiTestData.ShiftsManagePermission);

        /// <summary>
        /// Gives the employee a weekly template of work blocks from 30 days ago, so a future work day has blocks.
        /// </summary>
        public async Task SeedWorkWeekAsync(Guid employeeId)
        {
            using var scope = fixture.ScopeFactory.CreateActorScope(
                ServiceActor.DatabaseInitialization.For(ServiceName.Core));

            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            var blocks = Weekday.All
                .Where(weekday => weekday != Weekday.Saturday && weekday != Weekday.Sunday)
                .SelectMany(weekday => new[]
                {
                    (weekday, Times(WorkOrderApiTestData.Morning)),
                    (weekday, Times(WorkOrderApiTestData.Afternoon)),
                })
                .ToList();

            dbContext.ShiftTemplateVersions.Add(
                new ShiftTemplateVersion(
                    employeeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(value: -30), blocks));

            await dbContext.SaveChangesAsync();
        }

        public string CreateOrderBody(
            string? title = null,
            int estimateMinutes = WorkOrderApiTestData.ValidEstimate,
            string priority = "high",
            string? description = null,
            string? encryptedAssigneeId = null,
            string? plannedStartLocal = null
        )
        {
            var body = new JsonObject
            {
                ["title"] = title ?? WorkOrderApiTestData.Title,
                ["description"] = description,
                ["priority"] = priority,
                ["category"] = "repair",
                ["estimateMinutes"] = estimateMinutes,
                ["fileIds"] = new JsonArray(),
            };

            if (encryptedAssigneeId is not null)
            {
                body["assigneeId"] = encryptedAssigneeId;
                body["plannedStartLocal"] = plannedStartLocal;
            }

            return body.ToJsonString();
        }

        /// <summary>
        /// The next work day at least three days ahead, as a local start of the first morning block plus the offset.
        /// </summary>
        public string FutureStart(string time)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(WorkOrderApiTestData.FirstFutureDaysAhead);

            while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                date = date.AddDays(value: 1);
            }

            return $"{date:yyyy-MM-dd}T{time}";
        }

        public async Task<string> CreateOrderAsync(TestTokenClaims claims, string body)
        {
            var response = await fixture.SendShiftRequestAsync(
                HttpMethod.Post,
                WorkOrderApiTestData.BasePath,
                claims,
                body);

            await response.EnsureSuccessAsync(TestContext.Current.CancellationToken);
            var created = await response.Content.ReadFromJsonAsync<JsonObject>();

            return created!["id"]!.GetValue<string>();
        }

        public async Task<JsonObject> ReadOrderAsync(TestTokenClaims claims, string encryptedId)
        {
            var response = await fixture.SendShiftRequestAsync(
                HttpMethod.Get,
                $"{WorkOrderApiTestData.BasePath}/{encryptedId}",
                claims);

            await response.EnsureSuccessAsync(TestContext.Current.CancellationToken);

            return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        }

        public async Task<IReadOnlyList<string>> SearchTitlesAsync(TestTokenClaims claims)
        {
            var response = await fixture.SendShiftRequestAsync(
                HttpMethod.Post,
                WorkOrderApiTestData.SearchPath,
                claims,
                WorkOrderApiTestData.SearchBody);

            await response.EnsureSuccessAsync(TestContext.Current.CancellationToken);
            var page = await response.Content.ReadFromJsonAsync<JsonObject>();

            return [.. page!["items"]!.AsArray().Select(item => item!["id"]!.GetValue<string>())];
        }
    }

    private static BlockTimes Times(string range)
    {
        var parts = range.Split(separator: '-');

        return new BlockTimes
        {
            Start = TimeOnly.Parse(parts[0], CultureInfo.InvariantCulture),
            End = TimeOnly.Parse(parts[1], CultureInfo.InvariantCulture),
        };
    }
}
