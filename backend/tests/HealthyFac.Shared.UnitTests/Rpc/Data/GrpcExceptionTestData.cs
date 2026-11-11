using FluentValidation.Results;
using Grpc.Core;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Shared.UnitTests.Rpc.Data;

public static class GrpcExceptionTestData
{
    public const string SecretDetail = "connection string with a password";

    public static TheoryData<Exception, StatusCode> Mappings =>
        new()
        {
            { new NotFoundException(message: "missing"), StatusCode.NotFound },
            { new ForbiddenAccessException(message: "denied"), StatusCode.PermissionDenied },
            {
                new AppValidationException([new ValidationFailure(propertyName: "Name", errorMessage: "bad")]),
                StatusCode.InvalidArgument
            },
            { new OperationCanceledException(), StatusCode.Cancelled },
            { new InvalidOperationException(SecretDetail), StatusCode.Internal },
        };
}
