using HealthyFac.Shared.Domain.Enumerations;
using HealthyFac.Shared.UnitTests.Enumerations.Data;

namespace HealthyFac.Shared.UnitTests.Enumerations;

public sealed class EnumerationTests
{
    [Theory]
    [MemberData(
        nameof(EnumerationTestData.DeclarationOrder),
        MemberType = typeof(EnumerationTestData)
    )]
    public void All_WhenMembersDeclared_PreservesDeclarationOrder(IReadOnlyList<string> expectedValues)
    {
        MachineStateTestEnumeration.All
            .Select(member => member.Value)
            .Should()
            .Equal(expectedValues);
    }

    [Theory]
    [MemberData(
        nameof(EnumerationTestData.KnownValues),
        MemberType = typeof(EnumerationTestData)
    )]
    public void FromValue_WithKnownValue_ReturnsDeclaredMember(
        string value,
        MachineStateTestEnumeration expected
    )
    {
        MachineStateTestEnumeration.FromValue(value).Should().BeSameAs(expected);
    }

    [Fact]
    public void FromValue_WithUnknownValue_ThrowsInvalidOperationException()
    {
        var act = () => MachineStateTestEnumeration.FromValue(EnumerationTestData.UnknownValue);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(
        nameof(EnumerationTestData.UnknownValues),
        MemberType = typeof(EnumerationTestData)
    )]
    public void TryFromValue_WithUnknownValue_ReturnsFalse(string value)
    {
        MachineStateTestEnumeration.TryFromValue(value, out _).Should().BeFalse();
    }

    [Theory]
    [MemberData(
        nameof(EnumerationTestData.MiscasedKnownValues),
        MemberType = typeof(EnumerationTestData)
    )]
    public void TryFromValue_WithMiscasedKnownValue_ResolvesMember(string value)
    {
        MachineStateTestEnumeration.TryFromValue(value, out var member).Should().BeTrue();
        member.Value.Should().BeOneOf("operational", "inMaintenance");
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrueAndMatchingHashCode()
    {
        var resolved = MachineStateTestEnumeration.FromValue("operational");

        resolved.Should().Be(MachineStateTestEnumeration.Operational);
        resolved.GetHashCode().Should().Be(MachineStateTestEnumeration.Operational.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        MachineStateTestEnumeration.Operational
            .Should()
            .NotBe(MachineStateTestEnumeration.OutOfService);
    }
}
