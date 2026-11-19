using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Testing.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.WorkOrders.Data;

public static class WorkOrderDraftApiFixtureExtensions
{
    extension(CoreApiFixture fixture)
    {
        public TestTokenClaims CreateDraftClaims(params string[] permissions) =>
            fixture.CreateShiftClaims(Guid.CreateVersion7(), permissions);

        public async Task<string> ArrangeWorkOrderDraftAsync()
        {
            var machineId = fixture.IdEncryptor.Decrypt(
                await fixture.ArrangeMachineAsync(Guid.NewGuid().ToString(format: "N")));
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var draft = new WorkOrderDraft(
                Guid.CreateVersion7(),
                fixture.CompanyId,
                machineId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                WorkOrderApiTestData.Title,
                description: null,
                WorkOrderPriority.High,
                WorkOrderCategory.Repair,
                WorkOrderApiTestData.ValidEstimate);

            dbContext.WorkOrderDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(draft.Id);
        }

        public string CreateApplyBody(int estimateMinutes = WorkOrderApiTestData.ValidEstimate, uint? rowVersion = null)
        {
            var body = new JsonObject
            {
                [propertyName: "title"] = WorkOrderApiTestData.Title,
                [propertyName: "priority"] = "high",
                [propertyName: "category"] = "repair",
                [propertyName: "estimateMinutes"] = estimateMinutes,
            };
            if (rowVersion is not null)
            {
                body[propertyName: "rowVersion"] = rowVersion;
            }

            return body.ToJsonString();
        }

        public async Task<HttpResponseMessage> PostDraftAsync(
            TestTokenClaims claims,
            string encryptedId,
            string action,
            string body
        ) =>
            await fixture.SendShiftRequestAsync(
                HttpMethod.Post,
                $"{WorkOrderDraftApiTestData.BasePath}/{encryptedId}/{action}",
                claims,
                body);

        public async Task<JsonObject> ReadDraftAsync(TestTokenClaims claims, string encryptedId)
        {
            var response = await fixture.SendShiftRequestAsync(
                HttpMethod.Get,
                $"{WorkOrderDraftApiTestData.BasePath}/{encryptedId}",
                claims);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        }
    }
}
