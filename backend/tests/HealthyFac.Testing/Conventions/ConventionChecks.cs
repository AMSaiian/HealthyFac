using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HealthyFac.Shared.Application;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Testing.Conventions;

/// <summary>
/// Finds code that breaks a repository rule that the compiler does not check. Each method returns one line per
/// violation, so a failed assertion names every place to fix.
/// </summary>
public static class ConventionChecks
{
    private static readonly Regex CapturedParameter = new(ConventionConstants.CompilerNames.CapturedParameterPattern);

    /// <summary>
    /// Finds the options classes without a <c>public const string SectionName</c>. An options class that another
    /// options class holds as a property, an item, or a value is a child section and needs no name.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the options classes without a section name.</returns>
    public static IReadOnlyList<string> FindOptionsWithoutSectionName(Assembly assembly)
    {
        var optionsTypes = assembly.GetSourceTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Name.EndsWith(ConventionConstants.Options.TypeSuffix, StringComparison.Ordinal))
            .ToList();
        var childTypes = optionsTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .SelectMany(property => property.PropertyType.ExpandWrappedTypes())
            .ToHashSet();

        return optionsTypes
            .Where(type => !childTypes.Contains(type) && !type.HasSectionName)
            .Select(type => type.DisplayName)
            .ToList();
    }

    /// <summary>
    /// Finds the extension methods whose prefix does not match the receiver: <c>Add</c> on
    /// <see cref="IServiceCollection"/> and <see cref="OptionsBuilder{TOptions}"/>, <c>Use</c> on
    /// <see cref="IApplicationBuilder"/> and <see cref="WebApplication"/>, and <c>Map</c> on
    /// <see cref="IEndpointRouteBuilder"/>. The check covers extension blocks too, because the compiler emits each
    /// member of a block as a classic extension method. It skips a method without a result: a setup method returns
    /// its builder, and a <see langword="void"/> helper only checks the builder (e.g., a duplicate registration
    /// guard).
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the methods with a wrong prefix.</returns>
    public static IReadOnlyList<string> FindExtensionMethodsWithWrongPrefix(Assembly assembly) =>
        assembly.GetSourceTypes()
            .Where(type => type is { IsAbstract: true, IsSealed: true })
            .SelectMany(type => type.GetMethods(TypeInspection.DeclaredMembers))
            .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false)
                && method.ReturnType != typeof(void))
            .Select(method => (Method: method, Prefix: GetExpectedPrefix(method.GetParameters()[0].ParameterType)))
            .Where(item => item.Prefix is not null
                && !item.Method.Name.StartsWith(item.Prefix, StringComparison.Ordinal))
            .Select(item => $"{item.Method.DeclaringType!.DisplayName}.{item.Method.Name}")
            .ToList();

    /// <summary>
    /// Finds the namespaces whose root folder has a technical name (e.g., <c>Services</c>). The root folder is the
    /// first segment after the assembly name. A technical sub-folder of a business folder (e.g.,
    /// <c>Machines.Persistence</c>) is allowed, so deeper segments are not checked. A deny list cannot prove that a
    /// name is a business name; it catches only the listed names.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <param name="technicalNames">The folder names that a root folder must not have.</param>
    /// <returns>The namespaces with a technical root folder.</returns>
    public static IReadOnlyList<string> FindTechnicalRootFolders(
        Assembly assembly,
        IReadOnlySet<string> technicalNames
    ) =>
        GetFolderPaths(assembly)
            .Where(path => technicalNames.Contains(path.Folders[0]))
            .Select(path => path.Namespace)
            .ToList();

    /// <summary>
    /// Finds the boolean properties, fields, and parameters without an <c>is</c>, <c>has</c>, or <c>can</c> prefix.
    /// The check skips the members whose names an override or an outside interface fixes. Local variables are not
    /// in the metadata, so the check cannot see them.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the members without a prefix.</returns>
    public static IReadOnlyList<string> FindBooleansWithoutPrefix(Assembly assembly) =>
        assembly.GetSourceTypes()
            .Where(type => !type.IsEnum && !typeof(Delegate).IsAssignableFrom(type))
            .SelectMany(FindBooleansWithoutPrefix)
            .ToList();

    /// <summary>
    /// Finds the <see cref="CancellationToken"/> parameters that are not named <c>cancellationToken</c>. The check
    /// skips a method that overrides or implements a member declared outside this repository (e.g.,
    /// <c>BackgroundService.ExecuteAsync</c>), because that declaration fixes the name, and it skips
    /// compiler-generated members (lambdas, local functions).
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the methods with the wrong parameter name.</returns>
    public static IReadOnlyList<string> FindCancellationTokensWithWrongName(Assembly assembly) =>
        assembly.GetSourceTypes()
            .SelectMany(FindCancellationTokensWithWrongName)
            .ToList();

    /// <summary>
    /// Finds the protected abstract methods whose name does not end with <c>Internal</c> or <c>InternalAsync</c>.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the methods with a wrong suffix.</returns>
    public static IReadOnlyList<string> FindAbstractStepsWithoutInternalSuffix(Assembly assembly) =>
        assembly.GetSourceTypes()
            .Where(type => type.IsClass)
            .SelectMany(type => type.GetMethods(TypeInspection.DeclaredMembers))
            .Where(method => method.IsAbstract
                && (method.IsFamily || method.IsFamilyOrAssembly || method.IsFamilyAndAssembly))
            .Where(method => !method.Name.EndsWith(ConventionConstants.StepNames.Suffix, StringComparison.Ordinal)
                && !method.Name.EndsWith(ConventionConstants.StepNames.AsyncSuffix, StringComparison.Ordinal))
            .Select(method => $"{method.DeclaringType!.DisplayName}.{method.Name}")
            .ToList();

    /// <summary>
    /// Finds the primary constructor parameters that the class body reads. The compiler stores such a parameter in
    /// a hidden mutable field named <c>&lt;name&gt;P</c>. A parameter that only initializes a field or passes to the
    /// base constructor gets no hidden field.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the captured parameters with their types.</returns>
    public static IReadOnlyList<string> FindCapturedPrimaryConstructorParameters(Assembly assembly) =>
        assembly.GetSourceTypes()
            .SelectMany(type =>
                type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance))
            .Select(field => (Field: field, Match: CapturedParameter.Match(field.Name)))
            .Where(item => item.Match.Success)
            .Select(item => DescribeCapturedParameter(item.Field, item.Match))
            .ToList();

    public static IReadOnlyList<string> FindPositionalRecords(Assembly assembly) =>
        assembly.GetSourceTypes()
            .Where(type => type.IsRecord && type.HasGeneratedDeconstruct)
            .Select(type => type.DisplayName)
            .ToList();

    /// <summary>
    /// Finds the handlers that implement <see cref="IRequestHandler{TRequest}"/> directly. Only the shared adapter
    /// may implement it, because a direct implementation skips the decorators.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the direct implementations.</returns>
    public static IReadOnlyList<string> FindDirectUnitHandlers(Assembly assembly)
    {
        var adapterType = SharedApplicationAssembly.Instance.GetType(
            ConventionConstants.UnitAdapterType,
            throwOnError: true
        );

        return assembly.GetSourceTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type != adapterType)
            .Where(type => type.ImplementsGeneric(typeof(IRequestHandler<>)))
            .Select(type => type.DisplayName)
            .ToList();
    }

    public static IReadOnlyList<string> FindControllers(Assembly assembly) =>
        assembly.GetSourceTypes()
            .Where(typeof(ControllerBase).IsAssignableFrom)
            .Select(type => type.DisplayName)
            .ToList();

    /// <summary>
    /// Finds convention-based middleware: a type whose constructor takes the <see cref="RequestDelegate"/> of the
    /// next middleware. Middleware implements <see cref="IMiddleware"/> instead.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the convention-based middleware types.</returns>
    public static IReadOnlyList<string> FindConventionMiddleware(Assembly assembly) =>
        FindConstructorParameters(assembly, parameterType => parameterType == typeof(RequestDelegate));

    /// <summary>
    /// Finds constructors that inject a concrete <see cref="DbContext"/>. A use case injects the Application
    /// interface of the service (e.g., <c>ICoreDbContext</c>).
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The names of the types and parameters that inject a concrete context.</returns>
    public static IReadOnlyList<string> FindDbContextInjections(Assembly assembly) =>
        FindConstructorParameters(assembly, typeof(DbContext).IsAssignableFrom);

    /// <summary>
    /// Finds the string members of use case requests and integration messages that no text rule covers. It checks
    /// every type that implements <c>IRequest</c> (a consumed message too), every type with an
    /// <c>AbstractValidator</c>, and every nested <c>HealthyFac</c> type that holds strings. A message that the
    /// service only publishes is out of scope. A type with a string
    /// member or a string list needs a validator. Each string member needs a text rule (<c>SingleLineText</c> or
    /// <c>MultilineText</c>), and each member of a nested type needs a child validator rule.
    /// </summary>
    /// <param name="assemblies">The Application assemblies to inspect.</param>
    /// <returns>One line for each type without a validator and each member without a rule.</returns>
    public static IReadOnlyList<string> FindStringsWithoutTextRule(IEnumerable<Assembly> assemblies) =>
        TextRuleInspection.FindViolations(assemblies);

    /// <summary>
    /// Finds the integration messages (types that implement <c>IIntegrationMessage</c>) that no
    /// <c>HealthyFac.&lt;Service&gt;.Messaging</c> assembly declares.
    /// </summary>
    /// <param name="assemblies">The assemblies of the service and of every Messaging project it references.</param>
    /// <returns>One line for each message in another assembly.</returns>
    public static IReadOnlyList<string> FindMessagesOutsideMessagingAssembly(IEnumerable<Assembly> assemblies) =>
        [.. MessageContractInspection.FindOutsideMessagingAssembly(assemblies)];

    /// <summary>
    /// Finds the <c>HealthyFac.&lt;Service&gt;.Messaging</c> assemblies that reference a <c>HealthyFac</c> assembly
    /// other than <c>Shared.Application</c> and <c>Shared.Domain</c>: a layer of any service, another Messaging
    /// project, or a later service. It reads the referenced assemblies, so no list of services needs an update.
    /// </summary>
    /// <param name="assemblies">The assemblies to inspect. Only the Messaging ones among them are checked.</param>
    /// <returns>One line for each disallowed reference.</returns>
    public static IReadOnlyList<string> FindMessagingServiceDependencies(IEnumerable<Assembly> assemblies) =>
        [.. MessageContractInspection.FindDisallowedReferences(assemblies)];

    /// <summary>
    /// Finds the integration messages that more than one type declares, by <c>MessageType</c> or by type name. A
    /// copy of a contract in a consumer sits in another namespace and drifts from the original, so the full name
    /// does not find it. The same type name in two namespaces counts as a copy.
    /// </summary>
    /// <param name="assemblies">The assemblies of the service and of every Messaging project it references.</param>
    /// <returns>One line for each name that two or more types share.</returns>
    public static IReadOnlyList<string> FindDuplicateMessages(IEnumerable<Assembly> assemblies) =>
        [.. MessageContractInspection.FindDuplicates(assemblies)];

    /// <summary>
    /// Finds the parts of a use case that are not in the namespace of their request: the handler, the input
    /// validator, and the pre-handler check. It is the closest reflection check to "one use-case file".
    /// </summary>
    /// <param name="assemblies">The Application assemblies to inspect.</param>
    /// <returns>One line for each part outside the namespace of its request.</returns>
    public static IReadOnlyList<string> FindUseCasePartsOutsideRequestNamespace(IEnumerable<Assembly> assemblies) =>
        UseCasePartInspection.FindViolations(assemblies);

    /// <summary>
    /// Lists each namespace of the assembly with its folders below the assembly root. A namespace outside the
    /// assembly name (e.g., an extension class in <c>Microsoft.Extensions.DependencyInjection</c>) has no folder in
    /// the project, so the method skips it.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The namespaces with their folder names, from the root folder down.</returns>
    private static IEnumerable<(string Namespace, string[] Folders)> GetFolderPaths(Assembly assembly)
    {
        var rootNamespace = $"{assembly.GetName().Name}.";

        return assembly.GetSourceTypes()
            .Select(type => type.Namespace)
            .OfType<string>()
            .Distinct()
            .Where(name => name.StartsWith(rootNamespace, StringComparison.Ordinal))
            .Select(name => (Namespace: name, Folders: name[rootNamespace.Length..].Split(separator: '.')));
    }

    private static string? GetExpectedPrefix(Type receiver) => receiver switch
    {
        _ when receiver == typeof(WebApplication) || receiver == typeof(IApplicationBuilder) =>
            ConventionConstants.ExtensionPrefixes.Pipeline,
        _ when typeof(IEndpointRouteBuilder).IsAssignableFrom(receiver) =>
            ConventionConstants.ExtensionPrefixes.Endpoints,
        _ when receiver == typeof(IServiceCollection)
            || (receiver.IsGenericType && receiver.GetGenericTypeDefinition() == typeof(OptionsBuilder<>)) =>
            ConventionConstants.ExtensionPrefixes.Registration,
        _ => null,
    };

    private static IEnumerable<string> FindBooleansWithoutPrefix(Type type)
    {
        var externalMethods = type.GetExternalInterfaceMethods();
        var properties = type.GetProperties(TypeInspection.DeclaredMembers)
            .Where(property => property.PropertyType.IsBoolean && !property.IsCompilerGenerated)
            .Where(property => !property.GetAccessors(nonPublic: true)
                .Any(accessor => accessor.IsNamedElsewhere(externalMethods)))
            .Select(property => property.Name);
        var fields = type.GetFields(TypeInspection.DeclaredMembers)
            .Where(field => field.FieldType.IsBoolean && !field.IsCompilerGenerated)
            .Select(field => field.Name);
        var methods = type.GetMethods(TypeInspection.DeclaredMembers)
            .Where(method => !method.IsSpecialName)
            .Cast<MethodBase>()
            .Concat(type.GetConstructors(TypeInspection.DeclaredMembers))
            .Where(method => !method.IsCompilerGenerated && !method.IsNamedElsewhere(externalMethods));
        var parameters = methods
            .SelectMany(method => method.GetParameters())
            .Where(parameter => parameter.ParameterType.IsBoolean && parameter.Name is not null)
            .Select(parameter => (Name: parameter.Name!, Display: $"{parameter.Member.Name}({parameter.Name})"));

        return properties.Concat(fields)
            .Select(name => (Name: name, Display: name))
            .Concat(parameters)
            .Where(member => !HasBooleanPrefix(member.Name))
            .Select(member => $"{type.DisplayName}.{member.Display}");
    }

    private static IEnumerable<string> FindCancellationTokensWithWrongName(Type type)
    {
        var externalMethods = type.GetExternalInterfaceMethods();

        return type.GetMethods(TypeInspection.DeclaredMembers)
            .Cast<MethodBase>()
            .Concat(type.GetConstructors(TypeInspection.DeclaredMembers))
            .Where(method => !method.IsCompilerGenerated && !method.IsDeclaredOutside(externalMethods))
            .SelectMany(method => method.GetParameters())
            .Where(parameter => parameter.ParameterType == typeof(CancellationToken)
                && parameter.Name != ConventionConstants.CancellationTokens.ParameterName)
            .Select(parameter => $"{type.DisplayName}.{parameter.Member.Name}({parameter.Name})");
    }

    private static string DescribeCapturedParameter(FieldInfo field, Match match)
    {
        var parameterName = match.Groups[ConventionConstants.CompilerNames.CapturedParameterGroup].Value;

        return $"{field.DeclaringType!.DisplayName}({parameterName})";
    }

    private static bool HasBooleanPrefix(string name)
    {
        var trimmed = name.TrimStart(ConventionConstants.Booleans.FieldPrefix);

        return ConventionConstants.Booleans.Prefixes.Any(prefix => trimmed.Length > prefix.Length
            && trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && char.IsUpper(trimmed[prefix.Length]));
    }

    private static IReadOnlyList<string> FindConstructorParameters(Assembly assembly, Func<Type, bool> isForbidden) =>
        assembly.GetSourceTypes()
            .SelectMany(type =>
                type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .SelectMany(constructor => constructor.GetParameters())
            .Where(parameter => isForbidden(parameter.ParameterType))
            .Select(parameter => $"{parameter.Member.DeclaringType!.DisplayName}({parameter.Name})")
            .ToList();
}
