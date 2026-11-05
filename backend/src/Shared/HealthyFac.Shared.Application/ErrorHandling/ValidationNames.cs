using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentValidation;

namespace HealthyFac.Shared.Application.ErrorHandling;

/// <summary>
/// Names the keys of validation errors after the JSON names of the members, so a client finds the failed field
/// under the name that it sent. A display name in a message stays the split member name (e.g., "Serial Number").
/// </summary>
public static partial class ValidationNames
{
    private const char PathSeparator = '.';
    private const string WordBoundaryPattern = "(?<=[a-z0-9])(?=[A-Z])";

    private static readonly Lock Gate = new();
    private static bool IsConfigured;

    /// <summary>
    /// Sets the global property name and display name resolvers of FluentValidation. A second call changes
    /// nothing.
    /// </summary>
    public static void UseJsonNames()
    {
        lock (Gate)
        {
            if (IsConfigured)
            {
                return;
            }

            ValidatorOptions.Global.PropertyNameResolver = ResolvePropertyName;
            ValidatorOptions.Global.DisplayNameResolver = ResolveDisplayName;
            IsConfigured = true;
        }
    }

    /// <summary>
    /// Reads the JSON name of a member: its <see cref="JsonPropertyNameAttribute"/> when present, else the camel
    /// case of the member name, as <see cref="JsonSerializerOptions.Web"/> writes it.
    /// </summary>
    /// <param name="member">The property or field.</param>
    /// <returns>The JSON name.</returns>
    public static string GetJsonName(MemberInfo member) =>
        member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
        ?? JsonNamingPolicy.CamelCase.ConvertName(member.Name);

    private static string? ResolvePropertyName(Type type, MemberInfo? member, LambdaExpression? expression)
    {
        var path = expression is null ? null : BuildPath(expression.Body);

        return path ?? (member is null ? null : GetJsonName(member));
    }

    private static string? ResolveDisplayName(Type type, MemberInfo? member, LambdaExpression? expression) =>
        member is null ? null : WordBoundary().Replace(member.Name, replacement: " ");

    private static string? BuildPath(Expression expression)
    {
        switch (expression)
        {
            case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert:
                return BuildPath(convert.Operand);
            case MemberExpression member:
                var parent = member.Expression is null ? null : BuildPath(member.Expression);
                var name = GetJsonName(member.Member);

                return string.IsNullOrEmpty(parent) ? name : $"{parent}{PathSeparator}{name}";
            case ParameterExpression:
                return string.Empty;
            default:
                return null;
        }
    }

    [GeneratedRegex(WordBoundaryPattern)]
    private static partial Regex WordBoundary();
}
