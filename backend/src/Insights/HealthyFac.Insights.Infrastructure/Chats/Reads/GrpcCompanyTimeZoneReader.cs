using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.Companies;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.WorkOrders;
using HealthyFac.Shared.Infrastructure.Rpc;

namespace HealthyFac.Insights.Infrastructure.Chats.Reads;

/// <summary>
/// Reads the time zone of the company from Core over gRPC with the user client: the call carries the exchanged token of
/// the user and no actor header. A call that the caller cancelled ends in an <see cref="OperationCanceledException"/>,
/// and an outage ends in a <see cref="ProviderUnavailableException"/>.
/// </summary>
/// <param name="clientFactory">Creates the named user client.</param>
public sealed class GrpcCompanyTimeZoneReader(GrpcClientFactory clientFactory) : ICompanyTimeZoneReader
{
    private const string UnavailableMessage = "Core did not answer the company request.";

    private readonly GrpcClientFactory _clientFactory = clientFactory;

    public async Task<string> ReadTimeZoneIdAsync(CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient<CoreCompanies.CoreCompaniesClient>(CoreClientNames.User.Companies);

        try
        {
            var reply = await client.GetCurrentCompanyAsync(
                new GetCurrentCompanyRequest(),
                cancellationToken: cancellationToken);

            return reply.TimeZoneId;
        }
        catch (RpcException exception) when (RpcFailures.IsCancelledByCaller(exception, cancellationToken))
        {
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
        catch (RpcException exception) when (RpcFailures.IsOutage(exception))
        {
            throw new ProviderUnavailableException(UnavailableMessage, exception);
        }
    }
}
