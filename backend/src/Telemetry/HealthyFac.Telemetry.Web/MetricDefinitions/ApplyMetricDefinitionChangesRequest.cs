using System.Security.Cryptography;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationApplyMetricDefinitionChangesRequest =
    HealthyFac.Telemetry.Application.MetricDefinitions.ApplyMetricDefinitionChangesRequest;

namespace HealthyFac.Telemetry.Web.MetricDefinitions;

/// <summary>
/// The body of the apply. The company and the machine come from the token and the route. The deleted IDs are lists
/// of strings, because the ID convention of the JSON options covers only a property named <c>*Ids</c>.
/// </summary>
public record ApplyMetricDefinitionChangesRequest
{
    public required int ExpectedRevision { get; init; }

    public IReadOnlyList<CreatedMetricDefinition> Created { get; init; } = [];

    public IReadOnlyList<UpdatedMetricDefinition> Updated { get; init; } = [];

    public IReadOnlyList<string> Deleted { get; init; } = [];
}

public static class ApplyMetricDefinitionChangesRequestExtensions
{
    extension(ApplyMetricDefinitionChangesRequest request)
    {
        /// <summary>
        /// Builds the Application request. A deleted ID that is not a valid token names no definition, so it gets the
        /// same answer as an unknown definition.
        /// </summary>
        /// <param name="encryptedMachineId">The encrypted machine ID of the route.</param>
        /// <param name="companyId">The company of the token.</param>
        /// <param name="idEncryptor">Decrypts the IDs.</param>
        /// <returns>The Application request.</returns>
        /// <exception cref="NotFoundException">Thrown when an ID is not a valid token.</exception>
        public ApplicationApplyMetricDefinitionChangesRequest ToApplicationRequest(
            string encryptedMachineId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                ExpectedRevision = request.ExpectedRevision,
                Created = request.Created,
                Updated = request.Updated,
                Deleted = request.Deleted.Select(id => DecryptDeleted(id, idEncryptor)).ToList(),
            };
    }

    private static Guid DecryptDeleted(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(MetricDefinitionErrors.MetricNotFound, exception);
        }
    }
}
