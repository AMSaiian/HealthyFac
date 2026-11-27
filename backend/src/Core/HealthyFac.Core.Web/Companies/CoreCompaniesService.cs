using Grpc.Core;
using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.Contracts.Companies;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using ApplicationGetCurrentCompanyRequest = HealthyFac.Core.Application.Companies.GetCurrentCompanyRequest;
using RpcGetCurrentCompanyRequest = HealthyFac.Core.Contracts.Companies.GetCurrentCompanyRequest;

namespace HealthyFac.Core.Web.Companies;

/// <summary>
/// Serves the company of the actor to the services that read it (Insights) on the gRPC port. Any signed-in actor may
/// read its own company, as on the HTTP endpoint. The method runs the use case of that endpoint.
/// </summary>
/// <param name="handler">Reads the company of the actor.</param>
[Authorize]
public sealed class CoreCompaniesService(
    IRequestHandler<ApplicationGetCurrentCompanyRequest, GetCurrentCompanyResponse> handler
) : CoreCompanies.CoreCompaniesBase
{
    private readonly IRequestHandler<ApplicationGetCurrentCompanyRequest, GetCurrentCompanyResponse> _handler =
        handler;

    public override async Task<CompanyReply> GetCurrentCompany(
        RpcGetCurrentCompanyRequest request,
        ServerCallContext context
    )
    {
        var response = await _handler.HandleAsync(new ApplicationGetCurrentCompanyRequest(), context.CancellationToken);

        return new CompanyReply { Name = response.Name, TimeZoneId = response.TimeZoneId };
    }
}
