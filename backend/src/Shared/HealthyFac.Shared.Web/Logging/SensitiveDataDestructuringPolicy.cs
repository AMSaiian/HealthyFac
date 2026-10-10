using System.Collections.Concurrent;
using System.Reflection;
using HealthyFac.Shared.Application.Logging;
using Serilog.Core;
using Serilog.Events;

namespace HealthyFac.Shared.Web.Logging;

/// <summary>
/// Destructuring policy that masks properties marked with sensitive data attributes.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly ConcurrentDictionary<Type, TypeMaskingPlan> PlanCache = new();

    /// <summary>
    /// Attempts to destructure the specified value while masking any sensitive properties.
    /// </summary>
    /// <param name="value">The object instance to destructure.</param>
    /// <param name="propertyValueFactory">The factory to create child log event property values.</param>
    /// <param name="result">The resulting structured log event property value when successful.</param>
    /// <returns><see langword="true"/> if the object was destructured; otherwise, <see langword="false"/>.</returns>
    public bool TryDestructure(
        object? value,
        ILogEventPropertyValueFactory propertyValueFactory,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
        out LogEventPropertyValue? result
    )
    {
        if (value is null)
        {
            result = null;
            return false;
        }

        var type = value.GetType();
        if (IsSimpleType(type))
        {
            result = null;
            return false;
        }

        var plan = PlanCache.GetOrAdd(type, CreatePlan);
        if (!plan.IsWholeClassMasked && !plan.HasSensitiveProperties)
        {
            result = null;
            return false;
        }

        if (plan.IsWholeClassMasked)
        {
            result = new ScalarValue(plan.ClassMask ?? MaskingConstants.DefaultMask);
            return true;
        }

        var properties = new List<LogEventProperty>(plan.Properties.Count);
        foreach (var property in plan.Properties)
        {
            object? propertyValue;
            try
            {
                propertyValue = property.PropertyInfo.GetValue(value);
            }
            catch
            {
                propertyValue = MaskingConstants.EvaluationErrorMessage;
            }

            properties.Add(
                new LogEventProperty(
                    property.PropertyInfo.Name,
                    property.SensitiveAttribute is not null
                        ? MaskSensitiveValue(propertyValue, property.SensitiveAttribute)
                        : propertyValueFactory.CreatePropertyValue(propertyValue, destructureObjects: true)
                ));
        }

        result = new StructureValue(properties, type.Name);
        return true;
    }

    private static TypeMaskingPlan CreatePlan(Type type)
    {
        var classAttribute = type.GetCustomAttribute<SensitiveDataAttribute>(inherit: true);
        if (classAttribute is not null)
        {
            return new TypeMaskingPlan
            {
                IsWholeClassMasked = true,
                ClassMask = classAttribute.Mask,
                HasSensitiveProperties = true,
                Properties = [],
            };
        }

        var readableProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToList();

        var propertyInfos = new List<PropertyMaskingInfo>(readableProperties.Count);
        var hasSensitiveProperties = false;

        foreach (var prop in readableProperties)
        {
            var attribute = prop.GetCustomAttribute<SensitiveDataAttribute>(inherit: true);
            if (attribute is not null)
            {
                hasSensitiveProperties = true;
            }

            propertyInfos.Add(
                new PropertyMaskingInfo
                {
                    PropertyInfo = prop,
                    SensitiveAttribute = attribute,
                });
        }

        return new TypeMaskingPlan
        {
            IsWholeClassMasked = false,
            ClassMask = null,
            HasSensitiveProperties = hasSensitiveProperties,
            Properties = propertyInfos,
        };
    }

    private static LogEventPropertyValue MaskSensitiveValue(
        object? propertyValue,
        SensitiveDataAttribute attribute
    )
    {
        if (propertyValue is null)
        {
            return new ScalarValue(null);
        }

        if (propertyValue is string)
        {
            return new ScalarValue(SensitiveDataMasker.Mask(propertyValue, attribute));
        }

        if (propertyValue is System.Collections.IDictionary dictionary)
        {
            var entries = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>(dictionary.Count);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                entries.Add(
                    new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                        new ScalarValue(entry.Key?.ToString()),
                        new ScalarValue(SensitiveDataMasker.Mask(entry.Value, attribute))
                    ));
            }

            return new DictionaryValue(entries);
        }

        if (propertyValue is System.Collections.IEnumerable enumerable)
        {
            var elements = new List<LogEventPropertyValue>();
            foreach (var item in enumerable)
            {
                elements.Add(new ScalarValue(SensitiveDataMasker.Mask(item, attribute)));
            }

            return new SequenceValue(elements);
        }

        return new ScalarValue(SensitiveDataMasker.Mask(propertyValue, attribute));
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Uri);
    }

    private sealed class TypeMaskingPlan
    {
        public required bool IsWholeClassMasked { get; init; }

        public required string? ClassMask { get; init; }

        public required bool HasSensitiveProperties { get; init; }

        public required IReadOnlyList<PropertyMaskingInfo> Properties { get; init; }
    }

    private sealed class PropertyMaskingInfo
    {
        public required PropertyInfo PropertyInfo { get; init; }

        public required SensitiveDataAttribute? SensitiveAttribute { get; init; }
    }
}
