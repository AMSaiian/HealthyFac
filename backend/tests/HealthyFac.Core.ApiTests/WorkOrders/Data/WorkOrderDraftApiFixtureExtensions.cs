using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Shared.Infrastructure.HttpFailures;
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
            var proposal = DraftProposal.ForCase(Guid.CreateVersion7(), fixture.CompanyId, Guid.CreateVersion7());
            dbContext.DraftProposals.Add(proposal);

            var draft = new WorkOrderDraft(
                Guid.CreateVersion7(),
                fixture.CompanyId,
                machineId,
                proposal.Id,
                WorkOrderApiTestData.Title,
                description: null,
                WorkOrderPriority.High,
                WorkOrderCategory.Repair,
                WorkOrderApiTestData.ValidEstimate);

            dbContext.WorkOrderDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(draft.Id);
        }

        public string CreateApplyBody(
            int estimateMinutes = WorkOrderApiTestData.ValidEstimate,
            uint? rowVersion = null,
            string? encryptedAssigneeId = null,
            string? plannedStartLocal = null
        )
        {
            var body = new JsonObject
            {
                ["title"] = WorkOrderApiTestData.Title,
                ["priority"] = "high",
                ["category"] = "repair",
                ["estimateMinutes"] = estimateMinutes,
            };

            if (rowVersion is not null)
            {
                body["rowVersion"] = rowVersion;
            }

            if (encryptedAssigneeId is not null)
            {
                body["assigneeId"] = encryptedAssigneeId;
            }

            if (plannedStartLocal is not null)
            {
                body["plannedStartLocal"] = plannedStartLocal;
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

            await response.EnsureSuccessAsync(TestContext.Current.CancellationToken);

            return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        }
    }
}
