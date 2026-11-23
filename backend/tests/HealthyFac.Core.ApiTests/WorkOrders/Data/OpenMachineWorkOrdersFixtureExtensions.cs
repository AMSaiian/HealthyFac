using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using HealthyFac.Testing.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.WorkOrders.Data;

public static class OpenMachineWorkOrdersFixtureExtensions
{
    extension(CoreApiFixture fixture)
    {
        public async Task<string> ArrangeMachineOfAnotherCompanyAsync()
        {
            using var scope = fixture.ScopeFactory.CreateActorScope(
                ServiceActor.DatabaseInitialization.For(ServiceName.Core));
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var company = new Company
            {
                Name = Guid.NewGuid().ToString(format: "N"),
                TimeZoneId = CoreApiTestData.CompanyTimeZoneId,
            };
            dbContext.Companies.Add(company);
            var machine = new Machine(
                company.Id,
                GatewayIds.Create(),
                MachineApiTestData.MachineName,
                Guid.NewGuid().ToString(format: "N"),
                description: null,
                new DateOnly(year: 2099, month: 1, day: 1),
                scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);

            dbContext.Machines.Add(machine);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(machine.Id);
        }

        public async Task<string> CreateMachineOrderAsync(
            TestTokenClaims claims,
            string encryptedMachineId,
            string title
        )
        {
            var body = JsonNode.Parse(fixture.CreateOrderBody(title))!.AsObject();
            body[propertyName: "machineId"] = encryptedMachineId;

            return await fixture.CreateOrderAsync(claims, body.ToJsonString());
        }

        public async Task<string> CreateRejectedMachineOrderAsync(
            TestTokenClaims claims,
            string encryptedMachineId,
            string encryptedAssigneeId,
            string title
        )
        {
            var body = JsonNode.Parse(
                fixture.CreateOrderBody(
                    title,
                    encryptedAssigneeId: encryptedAssigneeId,
                    plannedStartLocal: fixture.FutureStart(WorkOrderApiTestData.StartTime)))!.AsObject();
            body[propertyName: "machineId"] = encryptedMachineId;
            var id = await fixture.CreateOrderAsync(claims, body.ToJsonString());
            await fixture.RejectOrderAsync(claims, id);

            return id;
        }

        public async Task RejectOrderAsync(TestTokenClaims claims, string encryptedId)
        {
            var order = await fixture.ReadOrderAsync(claims, encryptedId);
            var body = new JsonObject
            {
                [propertyName: "status"] = "rejected",
                [propertyName: "rowVersion"] = order[propertyName: "rowVersion"]!.GetValue<uint>(),
            };

            var response = await fixture.SendShiftRequestAsync(
                HttpMethod.Post,
                $"{WorkOrderApiTestData.BasePath}/{encryptedId}/status",
                claims,
                body.ToJsonString());
            await response.EnsureSuccessAsync(TestContext.Current.CancellationToken);
        }

        public async Task<HttpResponseMessage> ListOpenOrdersAsync(
            TestTokenClaims claims,
            string encryptedMachineId,
            int? limit = OpenMachineWorkOrdersTestData.DefaultLimit
        )
        {
            var query = limit is null ? string.Empty : $"?limit={limit}";

            return await fixture.SendShiftRequestAsync(
                HttpMethod.Get,
                $"v1/machines/{encryptedMachineId}/open-work-orders{query}",
                claims);
        }
    }
}
