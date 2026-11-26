using System.Reflection;
using FluentValidation.Validators;

namespace HealthyFac.Testing.Conventions;

/// <summary>
/// Reads the validators by reflection and reports the <c>NotEmpty</c> rules on nullable value properties.
/// </summary>
internal static class NullableRuleInspection
{
    public static IReadOnlyList<string> FindNotEmptyOnNullableValues(IEnumerable<Assembly> assemblies)
    {
        var types = assemblies.SelectMany(assembly => assembly.GetSourceTypes()).ToList();
        var violations = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var validatorType in TextRuleInspection.FindValidatorTypes(types).Values)
        {
            var descriptor = TextRuleInspection.CreateValidator(validatorType).CreateDescriptor();

            foreach (var rule in descriptor.GetMembersWithValidators()
                .SelectMany(group => descriptor.GetRulesForMember(group.Key)))
            {
                if (Nullable.GetUnderlyingType(rule.TypeToValidate) is not null
                    && rule.Components.Any(component => component.Validator is INotEmptyValidator))
                {
                    violations.Add($"{validatorType.Name}.{rule.PropertyName}");
                }
            }
        }

        return [.. violations];
    }
}
