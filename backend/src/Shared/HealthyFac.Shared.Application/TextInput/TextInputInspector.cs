using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using FluentValidation.Results;

namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Walks the strings of a request or a message: its string properties, the strings in its collections, and the
/// strings of nested objects. It reads the readable public properties of each type once and caches them. It visits
/// each object once, so a reference cycle ends the walk.
/// </summary>
internal static class TextInputInspector
{
    private const char MemberSeparator = '.';

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<TextInputMember>> MembersByType = new();

    private static readonly HashSet<Type> LeafTypes =
    [
        typeof(decimal),
        typeof(Guid),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(TimeSpan),
        typeof(Uri),
        typeof(Type),
    ];

    /// <summary>
    /// Finds every string that breaks a rule of <see cref="TextInputPolicy.FindViolation"/>.
    /// </summary>
    /// <param name="root">The request or the message.</param>
    /// <returns>One failure per invalid string. The property name holds the path (e.g., <c>Items[0].Name</c>); the
    /// failure holds no attempted value, so the response and the log never show the text.</returns>
    public static List<ValidationFailure> Inspect(object root)
    {
        var failures = new List<ValidationFailure>();
        InspectObject(root, string.Empty, new HashSet<object>(ReferenceEqualityComparer.Instance), failures);

        return failures;
    }

    private static void InspectObject(
        object value,
        string path,
        HashSet<object> visited,
        List<ValidationFailure> failures
    )
    {
        var type = value.GetType();
        if (!type.IsValueType && !visited.Add(value))
        {
            return;
        }

        foreach (var member in MembersByType.GetOrAdd(type, FindMembers))
        {
            InspectValue(
                member.Property.GetValue(value),
                path.Length == 0 ? member.Property.Name : $"{path}{MemberSeparator}{member.Property.Name}",
                member.Kind,
                visited,
                failures);
        }
    }

    /// <summary>
    /// Checks a string, walks a collection, or walks a nested object. A string element of a collection is
    /// single-line text.
    /// </summary>
    private static void InspectValue(
        object? value,
        string path,
        TextInputKind kind,
        HashSet<object> visited,
        List<ValidationFailure> failures
    )
    {
        switch (value)
        {
            case null:
                return;
            case string text:
                var violation = TextInputPolicy.FindViolation(text, kind);
                if (violation is not null)
                {
                    failures.Add(new ValidationFailure(path, violation));
                }

                return;
            case IEnumerable items when !IsLeafCollection(value.GetType()):
                if (!value.GetType().IsValueType && !visited.Add(value))
                {
                    return;
                }

                var index = 0;
                foreach (var item in items)
                {
                    InspectValue(item, $"{path}[{index}]", TextInputKind.SingleLine, visited, failures);
                    index++;
                }

                return;
            default:
                if (!IsLeaf(value.GetType()))
                {
                    InspectObject(value, path, visited, failures);
                }

                return;
        }
    }

    private static IReadOnlyList<TextInputMember> FindMembers(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetMethod?.IsPublic == true
                && property.GetIndexParameters().Length == 0
                && !IsLeaf(property.PropertyType))
            .Select(property => new TextInputMember
            {
                Property = property,
                Kind = TextInputPolicy.KindOf(property),
            })
            .ToList();

    private static bool IsLeaf(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsPrimitive || underlying.IsEnum || LeafTypes.Contains(underlying);
    }

    /// <summary>
    /// Tells a collection that holds no string and no object (e.g., a byte array or a list of IDs), so the walk skips
    /// it instead of visiting every element.
    /// </summary>
    private static bool IsLeafCollection(Type type)
    {
        var elementType = type.IsArray
            ? type.GetElementType()
            : type.GetInterfaces()
                .Append(type)
                .FirstOrDefault(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];

        return elementType is not null && IsLeaf(elementType);
    }

    private sealed record TextInputMember
    {
        public required PropertyInfo Property { get; init; }

        public required TextInputKind Kind { get; init; }
    }
}
