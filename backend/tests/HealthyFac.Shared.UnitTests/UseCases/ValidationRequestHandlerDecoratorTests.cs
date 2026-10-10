using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases.Decorators;
using HealthyFac.Shared.UnitTests.UseCases.Data;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.UnitTests.UseCases;

public sealed class ValidationRequestHandlerDecoratorTests
{
    private const string ErrorsKey = "@Errors";

    private readonly CapturingLogger<ValidationRequestHandlerDecorator<ValidatedRequest, int>> _logger = new();

    private readonly ValidationRequestHandlerDecorator<ValidatedRequest, int> _decorator;

    public ValidationRequestHandlerDecoratorTests()
    {
        _decorator = new ValidationRequestHandlerDecorator<ValidatedRequest, int>(
            new FixedResultHandler(),
            [new ValidatedRequestValidator()],
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_LogsInformationWithErrorsPerProperty()
    {
        var act = () => _decorator.HandleAsync(new ValidatedRequest { Name = string.Empty });

        await act.Should().ThrowAsync<AppValidationException>();
        var entry = _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Information).Subject;
        entry.Exception.Should().BeNull();
        var errors = entry.Properties[ErrorsKey].Should().BeAssignableTo<IDictionary<string, string[]>>().Subject;
        errors["Name"].Should().ContainSingle(ValidationMessages.NameRequired);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationPasses_LogsNoInformationEntryAndCallsHandler()
    {
        var result = await _decorator.HandleAsync(new ValidatedRequest { Name = "Press" });

        result.Should().Be("Press".Length);
        _logger.Entries.Should().NotContain(e => e.Level >= LogLevel.Information);
    }
}
