using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.UnitTests.Observability.Data;

public static class ActivityOutcomeTestData
{
    public static TheoryData<Exception> Rejections => new()
    {
        new AppValidationException([new ValidationFailure(propertyName: "Name", errorMessage: "required")]),
        new NotFoundException(message: "missing"),
        new UnauthorizedAccessException(message: "anonymous"),
        new ForbiddenAccessException(message: "denied"),
        new ConflictException(message: "duplicate"),
        new UnprocessableException(message: "rule"),
    };

    public static TheoryData<Exception> Failures => new()
    {
        new InvalidOperationException(message: "broken"),
        new NullReferenceException(),
        new TimeoutException(),
    };
}
