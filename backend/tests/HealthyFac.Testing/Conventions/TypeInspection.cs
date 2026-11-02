using System.Reflection;
using System.Runtime.CompilerServices;

namespace HealthyFac.Testing.Conventions;

internal static class TypeInspection
{
    public const BindingFlags DeclaredMembers = BindingFlags.DeclaredOnly
        | BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static;

    extension(Assembly assembly)
    {
        /// <summary>
        /// Lists the types that the source code declares. The compiler adds types for closures, iterators, anonymous
        /// types, and the skeletons of extension blocks; their names hold a <c>&lt;</c>, so the filter skips them and
        /// every type nested in them.
        /// </summary>
        /// <returns>The declared types of the assembly.</returns>
        public IEnumerable<Type> GetSourceTypes() =>
            assembly.GetTypes()
                .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), false))
                .Where(type => !(type.FullName ?? type.Name).Contains(
                    ConventionConstants.CompilerNames.Marker,
                    StringComparison.Ordinal
                ));
    }

    extension(Type type)
    {
        public string DisplayName => type.FullName ?? type.Name;

        public bool IsBoolean => type == typeof(bool)
            || type == typeof(bool?)
            || (type.IsByRef && type.GetElementType()!.IsBoolean);

        public bool IsRecord => type.GetMethods(DeclaredMembers)
            .Any(method => method.Name == ConventionConstants.CompilerNames.RecordPrintMembers);

        /// <summary>
        /// Checks for the <c>Deconstruct</c> method that the compiler generates only for a record with a primary
        /// constructor. A hand-written <c>Deconstruct</c> has no <see cref="CompilerGeneratedAttribute"/>.
        /// </summary>
        public bool HasGeneratedDeconstruct => type.GetMethods(DeclaredMembers)
            .Any(method => method.Name == ConventionConstants.CompilerNames.Deconstruct
                && method.IsDefined(typeof(CompilerGeneratedAttribute), false));

        public bool HasSectionName =>
            type.GetField(ConventionConstants.Options.SectionNameField, BindingFlags.Public | BindingFlags.Static) is
            {
                IsLiteral: true,
            } sectionName
            && sectionName.FieldType == typeof(string);

        public bool ImplementsGeneric(Type definition) => type.GetInterfaces()
            .Any(implemented => implemented.IsGenericType && implemented.GetGenericTypeDefinition() == definition);

        /// <summary>
        /// Lists the type and every type that it wraps: the item type of an array and the type arguments of a generic
        /// type, at any depth (e.g., the value type of a dictionary of lists).
        /// </summary>
        /// <returns>The type and its wrapped types.</returns>
        public IEnumerable<Type> ExpandWrappedTypes()
        {
            yield return type;

            var wrappedTypes = type.IsArray ? [type.GetElementType()!] : type.GetGenericArguments();
            foreach (var wrappedType in wrappedTypes.SelectMany(wrapped => wrapped.ExpandWrappedTypes()))
            {
                yield return wrappedType;
            }
        }

        /// <summary>
        /// Lists the methods of the type that implement an interface from outside this repository. Their names and
        /// parameter names come from that interface, so a naming rule of this repository does not apply to them.
        /// </summary>
        /// <returns>The implementing methods.</returns>
        public HashSet<MethodInfo> GetExternalInterfaceMethods()
        {
            if (type.IsInterface)
            {
                return [];
            }

            return type.GetInterfaces()
                .Where(implemented => !(implemented.Assembly.GetName().Name ?? string.Empty).StartsWith(
                    ConventionConstants.ProjectAssemblyPrefix,
                    StringComparison.Ordinal
                ))
                .SelectMany(implemented => type.GetInterfaceMap(implemented).TargetMethods)
                .ToHashSet();
        }
    }

    extension(MemberInfo member)
    {
        public bool IsCompilerGenerated => member.IsDefined(typeof(CompilerGeneratedAttribute), false)
            || member.Name.Contains(ConventionConstants.CompilerNames.Marker, StringComparison.Ordinal);
    }

    extension(MethodBase method)
    {
        /// <summary>
        /// Checks whether another declaration fixes the name of the method and of its parameters: an override, or an
        /// implementation of an interface from outside this repository.
        /// </summary>
        /// <param name="externalMethods">The methods that implement an outside interface.</param>
        /// <returns><see langword="true"/> when another declaration fixes the names.</returns>
        public bool IsNamedElsewhere(HashSet<MethodInfo> externalMethods) =>
            method is MethodInfo info
            && (info.GetBaseDefinition().DeclaringType != info.DeclaringType || externalMethods.Contains(info));

        /// <summary>
        /// Checks whether a declaration outside this repository fixes the parameter names of the method: an override
        /// of an outside base method, or an implementation of an outside interface. An override of a method of this
        /// repository returns <see langword="false"/>, so a naming rule still applies to it.
        /// </summary>
        /// <param name="externalMethods">The methods that implement an outside interface.</param>
        /// <returns><see langword="true"/> when an outside declaration fixes the names.</returns>
        public bool IsDeclaredOutside(HashSet<MethodInfo> externalMethods) =>
            method is MethodInfo info
            && (externalMethods.Contains(info)
                || !(info.GetBaseDefinition().DeclaringType?.Assembly.GetName().Name ?? string.Empty).StartsWith(
                    ConventionConstants.ProjectAssemblyPrefix,
                    StringComparison.Ordinal
                ));
    }
}
