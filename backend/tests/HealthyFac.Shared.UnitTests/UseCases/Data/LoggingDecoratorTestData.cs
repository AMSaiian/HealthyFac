using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.Extensions.Logging;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.UnitTests.UseCases.Data;

public static class LoggingDecoratorTestData
{
    public const string TestSourceName = "HealthyFac.Shared.UnitTests.UseCases";
    public const string ParentActivityName = "Test.Parent";

    public static TheoryData<Exception, LogLevel> RejectionsWithLevel => new()
    {
        { new NotFoundException(message: "missing"), LogLevel.Information },
        { new ConflictException(message: "duplicate"), LogLevel.Information },
        { new ConflictException(message: "repeated") { Level = LogLevel.Warning }, LogLevel.Warning },
        { new UnprocessableException(message: "rule"), LogLevel.Information },
        { new UnauthorizedAccessException(message: "anonymous"), LogLevel.Warning },
        { new ForbiddenAccessException(message: "denied"), LogLevel.Warning },
        { new ErrorLevelRejectionException(), LogLevel.Error },
    };

    public static TheoryData<Exception> IgnoredRejections => new()
    {
        new AppValidationException([new ValidationFailure(propertyName: "Name", errorMessage: "required")]),
        new NotFoundException(message: "quiet") { Level = null },
    };
}
