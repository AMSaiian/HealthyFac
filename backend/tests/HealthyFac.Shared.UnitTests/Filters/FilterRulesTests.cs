using System.Globalization;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.UnitTests.Filters.Data;

namespace HealthyFac.Shared.UnitTests.Filters;

public sealed class FilterRulesTests
{
    [Fact]
    public void IsTextFilter_WithNullFilter_Passes()
    {
        var holder = new TextFilterHolder { Name = null };

        var result = new TextFilterHolderValidator().Validate(holder);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsTextFilter_WithValueAboveLimit_Fails()
    {
        ValidationNames.UseJsonNames();

        var holder = new TextFilterHolder { Name = new TextFilter { Value = FilterRulesTestData.LongValue } };
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        try
        {
            var result = new TextFilterHolderValidator().Validate(holder);

            var error = result.Errors.Should().ContainSingle().Subject;

            error.PropertyName.Should().Be("name.value");
            error.ErrorMessage.Should().Be(FilterRulesTestData.LengthMessage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
