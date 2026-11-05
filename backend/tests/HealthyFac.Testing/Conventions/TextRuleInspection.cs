using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Testing.Conventions;

/// <summary>
/// Reads the validators of the input types by reflection and reports the strings that no text rule covers.
/// </summary>
internal static class TextRuleInspection
{
    public static IReadOnlyList<string> FindViolations(IEnumerable<Assembly> assemblies)
    {
        ValidationNames.UseJsonNames();

        var types = assemblies.SelectMany(assembly => assembly.GetSourceTypes()).ToList();
        var validatorTypes = FindValidatorTypes(types);
        var violations = new SortedSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Type>(types.Where(IsInputType).Concat(validatorTypes.Keys));
        var visited = new HashSet<Type>();

        while (queue.TryDequeue(out var type))
        {
            if (!visited.Add(type) || type.IsEnumeration())
            {
                continue;
            }

            var stringMembers = type.GetStringMembers();
            var nestedMembers = type.GetNestedMembers();
            foreach (var nestedType in nestedMembers.Select(member => member.NestedType))
            {
                queue.Enqueue(nestedType);
            }

            if (stringMembers.Count == 0 && nestedMembers.Count == 0)
            {
                continue;
            }

            if (!validatorTypes.TryGetValue(type, out var validatorType))
            {
                violations.Add($"{type.DisplayName}: has string members and no AbstractValidator");

                continue;
            }

            var descriptor = CreateValidator(validatorType).CreateDescriptor();

            foreach (var member in stringMembers.Where(member => !HasRule(descriptor, member, IsTextRule)))
            {
                violations.Add($"{type.DisplayName}.{member.Name}: no SingleLineText or MultilineText rule");
            }

            foreach (var member in nestedMembers.Where(member => !HasRule(descriptor, member.Property, IsChildRule)))
            {
                violations.Add($"{type.DisplayName}.{member.Property.Name}: no child validator rule");
            }
        }

        return violations.ToList();
    }

    private static Dictionary<Type, Type> FindValidatorTypes(IEnumerable<Type> types)
    {
        var validatorTypes = new Dictionary<Type, Type>();

        foreach (var type in types.Where(type => type is { IsClass: true, IsAbstract: false }
            && !type.ContainsGenericParameters))
        {
            for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                {
                    validatorTypes.TryAdd(baseType.GetGenericArguments()[0], type);

                    break;
                }
            }
        }

        return validatorTypes;
    }

    private static bool IsInputType(Type type) =>
        type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false }
        && type.ImplementsGeneric(typeof(IRequest<>));

    private static IValidator CreateValidator(Type validatorType)
    {
        var constructor = validatorType.GetConstructors().Single();
        var arguments = constructor.GetParameters()
            .Select(parameter => CreateStub(parameter.ParameterType))
            .ToArray();

        return (IValidator)constructor.Invoke(arguments);
    }

    private static object CreateStub(Type interfaceType) =>
        typeof(DispatchProxy)
            .GetMethods()
            .Single(method => method.Name == nameof(DispatchProxy.Create) && method.GetGenericArguments().Length == 2)
            .MakeGenericMethod(interfaceType, typeof(DefaultReturnProxy))
            .Invoke(obj: null, parameters: null)!;

    private static bool HasRule(
        IValidatorDescriptor descriptor,
        PropertyInfo member,
        Func<IRuleComponent, bool> isWanted
    ) =>
        new[] { member.Name, ValidationNames.GetJsonName(member) }
            .SelectMany(descriptor.GetRulesForMember)
            .SelectMany(rule => rule.Components)
            .Any(isWanted);

    private static bool IsTextRule(IRuleComponent component) =>
        component.Validator.GetType() is { IsGenericType: true } validatorType
        && validatorType.GetGenericTypeDefinition() == typeof(TextInputPropertyValidator<>);

    private static bool IsChildRule(IRuleComponent component) => component.Validator is IChildValidatorAdaptor;

    private static List<PropertyInfo> GetStringMembers(this Type type) =>
        type.GetReadableProperties()
            .Where(property => property.PropertyType == typeof(string) || IsStringCollection(property.PropertyType))
            .ToList();

    private static List<(PropertyInfo Property, Type NestedType)> GetNestedMembers(this Type type) =>
        type.GetReadableProperties()
            .Select(property => (Property: property, NestedType: GetElementOrSelf(property.PropertyType)))
            .Where(member => member.NestedType.IsProjectClassWithStrings())
            .ToList();

    private static IEnumerable<PropertyInfo> GetReadableProperties(this Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetMethod is { IsPublic: true }
                && property.GetIndexParameters().Length == 0
                && !property.IsDefined(typeof(JsonIgnoreAttribute), inherit: true));

    private static bool IsStringCollection(Type type) =>
        type != typeof(string)
        && typeof(IEnumerable).IsAssignableFrom(type)
        && GetElementOrSelf(type) == typeof(string);

    private static Type GetElementOrSelf(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(underlying))
        {
            return underlying;
        }

        if (underlying.IsArray)
        {
            return underlying.GetElementType()!;
        }

        return underlying.GetInterfaces()
                .Append(underlying)
                .FirstOrDefault(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0]
            ?? underlying;
    }

    private static bool IsEnumeration(this Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Enumeration<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProjectClassWithStrings(this Type type, HashSet<Type>? path = null)
    {
        if (!type.IsClass
            || type == typeof(string)
            || type.IsEnumeration()
            || !(type.Assembly.GetName().Name ?? string.Empty).StartsWith(
                ConventionConstants.ProjectAssemblyPrefix,
                StringComparison.Ordinal))
        {
            return false;
        }

        path ??= [];
        if (!path.Add(type))
        {
            return false;
        }

        return type.GetStringMembers().Count > 0
            || type.GetReadableProperties()
                .Any(property => GetElementOrSelf(property.PropertyType).IsProjectClassWithStrings(path));
    }

    /// <summary>Answers each call with the default value of the return type, so a validator can be built.</summary>
    public class DefaultReturnProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType is { IsValueType: true } returnType && returnType != typeof(void)
                ? Activator.CreateInstance(returnType)
                : null;
    }
}
