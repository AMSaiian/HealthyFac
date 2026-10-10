using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.UnitTests.Observability.Data;

public static class ActivityOutcomeTestData
{
    public static TheoryData<Exception> Rejections => new()
    {
        new AppValidationException([new ValidationFailure("Name", "required")]),
        new NotFoundException("missing"),
        new UnauthorizedAccessException("anonymous"),
        new ForbiddenAccessException("denied"),
        new ConflictException("duplicate"),
        new UnprocessableException("rule"),
    };

    public static TheoryData<Exception> Failures => new()
    {
        new InvalidOperationException("broken"),
        new NullReferenceException(),
        new TimeoutException(),
    };
}
