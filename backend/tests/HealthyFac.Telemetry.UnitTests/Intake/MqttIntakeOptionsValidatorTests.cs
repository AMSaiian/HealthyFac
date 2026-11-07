using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Telemetry.UnitTests.Intake.Data;

namespace HealthyFac.Telemetry.UnitTests.Intake;

public sealed class MqttIntakeOptionsValidatorTests
{
    private readonly MqttIntakeOptionsValidator _validator = new();

    [Fact]
    public void Validate_WhenDisabled_NeedsNoSetting()
    {
        var result = _validator.Validate(new MqttIntakeOptions { IsEnabled = false, Host = string.Empty });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEnabledWithPassword_IsValid()
    {
        var result = _validator.Validate(new MqttIntakeOptions { IsEnabled = true, Password = "secret" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEnabledWithoutPassword_ReportsPassword()
    {
        var result = _validator.Validate(new MqttIntakeOptions { IsEnabled = true });

        result.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(MqttIntakeConstants.Messages.PasswordRequired);
    }

    [Theory]
    [MemberData(
        nameof(MqttIntakeOptionsValidatorTestData.InvalidGroups),
        MemberType = typeof(MqttIntakeOptionsValidatorTestData))]
    public void Validate_WithGroupThatBreaksTheTopicFilter_ReportsGroup(string group)
    {
        var result = _validator.Validate(
            new MqttIntakeOptions { IsEnabled = true, Password = "secret", SharedGroup = group });

        result.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(MqttIntakeConstants.Messages.SharedGroupInvalid);
    }

    [Theory]
    [MemberData(
        nameof(MqttIntakeOptionsValidatorTestData.LongestValidClientIds),
        MemberType = typeof(MqttIntakeOptionsValidatorTestData))]
    public void Validate_WithClientIdThatFitsWithTheIndexSuffix_IsValid(int consumerCount, int clientIdLength)
    {
        var result = _validator.Validate(
            new MqttIntakeOptions
            {
                IsEnabled = true,
                Password = "secret",
                ConsumerCount = consumerCount,
                ClientId = new string(c: 'a', clientIdLength),
            });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(MqttIntakeOptionsValidatorTestData.TooLongClientIds),
        MemberType = typeof(MqttIntakeOptionsValidatorTestData))]
    public void Validate_WithClientIdThatBreaksTheLimitWithTheIndexSuffix_ReportsClientId(
        int consumerCount,
        int clientIdLength
    )
    {
        var result = _validator.Validate(
            new MqttIntakeOptions
            {
                IsEnabled = true,
                Password = "secret",
                ConsumerCount = consumerCount,
                ClientId = new string(c: 'a', clientIdLength),
            });

        result.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(MqttIntakeConstants.Messages.ClientIdInvalid);
    }

    [Theory]
    [MemberData(
        nameof(MqttIntakeOptionsValidatorTestData.ValidDrainTimeouts),
        MemberType = typeof(MqttIntakeOptionsValidatorTestData))]
    public void Validate_WithDrainTimeoutBelowTheStopTime_IsValid(int drainTimeoutSeconds)
    {
        var result = _validator.Validate(
            new MqttIntakeOptions { IsEnabled = true, Password = "secret", DrainTimeoutSeconds = drainTimeoutSeconds });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(MqttIntakeOptionsValidatorTestData.InvalidDrainTimeouts),
        MemberType = typeof(MqttIntakeOptionsValidatorTestData))]
    public void Validate_WithDrainTimeoutAtOrAboveTheStopTime_ReportsDrainTimeout(int drainTimeoutSeconds)
    {
        var result = _validator.Validate(
            new MqttIntakeOptions { IsEnabled = true, Password = "secret", DrainTimeoutSeconds = drainTimeoutSeconds });

        result.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(MqttIntakeConstants.Messages.DrainTimeoutOutOfRange);
    }
}
