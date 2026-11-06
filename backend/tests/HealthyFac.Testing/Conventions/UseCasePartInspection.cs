using System.Reflection;
using FluentValidation;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Testing.Conventions;

/// <summary>
/// Compares the namespace of each use case request with the namespaces of its handler, input validator, and
/// pre-handler check.
/// </summary>
internal static class UseCasePartInspection
{
    public static IReadOnlyList<string> FindViolations(IEnumerable<Assembly> assemblies)
    {
        var types = assemblies.SelectMany(assembly => assembly.GetSourceTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
            .ToList();
        var violations = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var request in types.Where(type => type.ImplementsGeneric(typeof(IRequest<>))))
        {
            foreach (var part in types.Where(type => IsPartOf(type, request)))
            {
                if (part.Namespace != request.Namespace)
                {
                    violations.Add($"{part.DisplayName}: not in the namespace of {request.DisplayName}");
                }
            }
        }

        return violations.ToList();
    }

    private static bool IsPartOf(Type candidate, Type request) =>
        candidate.GetInterfaces()
            .Any(implemented => implemented.IsGenericType
                && implemented.GetGenericArguments()[0] == request
                && (implemented.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                    || implemented.GetGenericTypeDefinition() == typeof(IPreHandlerCheck<>)))
        || IsValidatorOf(candidate, request);

    private static bool IsValidatorOf(Type candidate, Type request)
    {
        for (var baseType = candidate.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType
                && baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>)
                && baseType.GetGenericArguments()[0] == request)
            {
                return true;
            }
        }

        return false;
    }
}
