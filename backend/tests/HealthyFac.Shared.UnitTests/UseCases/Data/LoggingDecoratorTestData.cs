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
        { new NotFoundException("missing"), LogLevel.Information },
        { new ConflictException("duplicate"), LogLevel.Information },
        { new ConflictException("repeated") { Level = LogLevel.Warning }, LogLevel.Warning },
        { new UnprocessableException("rule"), LogLevel.Information },
        { new UnauthorizedAccessException("anonymous"), LogLevel.Warning },
        { new ForbiddenAccessException("denied"), LogLevel.Warning },
        { new ErrorLevelRejectionException(), LogLevel.Error },
    };

    public static TheoryData<Exception> IgnoredRejections => new()
    {
        new AppValidationException([new ValidationFailure("Name", "required")]),
        new NotFoundException("quiet") { Level = null },
    };
}
