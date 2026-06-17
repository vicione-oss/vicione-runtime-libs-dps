using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ViciOne.Suite.System.Runtime;

/// <summary>
/// Contains utility functions for traversing complex object types and analyzing their structures,
/// properties, values and extracts the contained simple types from them. Each simple type is
/// accessible through a PropertyGetter function that can be used to extract the value from the
/// simple type within the complex object. The methods in this class are thread-safe.
/// </summary>
internal class TypeAnalyzer : IDisposable
{
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();
    private readonly ConcurrentDictionary<Type, bool> _simpleTypeCache = new();
    private readonly ConcurrentDictionary<Type, string> _commentCache = new();
    private readonly ILogger? _logger;

    private static readonly HashSet<Type> s_simpleTypes =
    [
        typeof(decimal),
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(BigInteger)
    ];

    internal TypeAnalyzer(ILogger? logger)
        => _logger = logger;

    public IEnumerable<TypeInfo> AnalyzeType<T>()
        => AnalyzeType(typeof(T));

    public IEnumerable<TypeInfo> AnalyzeType(Type type)
        => TraverseTypeInternal(type, string.Empty);

    private IEnumerable<TypeInfo> TraverseTypeInternal(Type inputType, string path)
    {
        if (inputType.IsEnum)
        {
            yield return HandleEnum();
        }
        else if (IsSimpleType(inputType))
        {
            yield return HandleSimpleType();
        }
        else if (typeof(IEnumerable).IsAssignableFrom(inputType))
        {
            yield return HandleCollections();
        }
        else
        {
            foreach (var typeInfo in HandleProperties())
                yield return typeInfo;
        }

        TypeInfo HandleEnum()
        {
            _logger?.AddingEnumTypeInfo(inputType, path);
            return new()
            {
                SimpleType = inputType,
                Path = path,
                IsNullable = false,
                Name = inputType.Name,
                Comment = GetComment(inputType)
            };
        }

        TypeInfo HandleSimpleType()
        {
            _logger?.AddingSimpleTypeInfo(inputType, path);
            return new()
            {
                SimpleType = inputType,
                Path = path,
                IsNullable = IsNullableType(inputType),
                Name = inputType.Name,
                Comment = GetComment(inputType)
            };
        }

        TypeInfo HandleCollections()
            => new()
            {
                SimpleType = inputType,
                Path = path,
                IsNullable = IsNullableType(inputType),
                Name = inputType.Name,
                Comment = GetComment(inputType)
            };

        IEnumerable<TypeInfo> HandleProperties()
        {
            foreach (var property in GetPublicProperties(inputType))
            {
                TryCreatePropertyGetter(property, out var propertyGetter);
                TryCreatePropertySetter(property, out var propertySetter);

                foreach (var item in TraverseTypeInternal(property.PropertyType, $"{path}.{property.Name}".TrimStart('.')))
                    yield return item with { PropertyGetter = propertyGetter, PropertySetter = propertySetter };
            }
        }
    }

    private bool IsSimpleType(Type type)
        => _simpleTypeCache.GetOrAdd(type, CheckIfSimpleType);

    private bool CheckIfSimpleType(Type type)
    {
        if (type.IsPrimitive || s_simpleTypes.Contains(type))
            return true;

        if (TryGetNullableType(type, out var nullableUnderlyingType))
            return IsSimpleType(nullableUnderlyingType);

        return false;
    }

    private static bool IsNullableType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return true;
        return false;
    }

    private static bool TryGetNullableType(Type type, [NotNullWhen(true)] out Type? nullableUnderlyingType)
    {
        nullableUnderlyingType = Nullable.GetUnderlyingType(type);

        if (nullableUnderlyingType is not null)
            return true;
        return false;
    }

    private PropertyInfo[] GetPublicProperties(Type type)
        => _propertyCache.GetOrAdd(type, t => t.GetProperties());

    private string GetComment(Type type)
        => _commentCache.GetOrAdd(type, GenerateComment);

    internal static string GenerateComment(Type type)
    {
        var sb = new StringBuilder();

        // If the type has a DescriptionAttribute, use its description as the comment
        if (type.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() is DescriptionAttribute descriptionAttribute)
            sb.AppendLine(descriptionAttribute.Description);

        // If the type is an enum, list out all the names and values as part of the comment
        if (type.IsEnum)
        {
            foreach (var enumEntry in Enum.GetValues(type).Cast<Enum>())
            {
                var id = enumEntry.GetHashCode();
                var name = enumEntry.ToString();
                var enumDescription = type.GetField(name)
                    ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    ?.FirstOrDefault() is DescriptionAttribute enumDescriptionAttribute
                    ? enumDescriptionAttribute.Description : name;

                sb.AppendLine(CultureInfo.InvariantCulture, $"{id} = {enumDescription}");
            }
        }

        if (sb.Length == 0)
            sb.AppendLine(CultureInfo.InvariantCulture, $"This is a {type.FullName}.");

        return sb.ToString().TrimEnd();
    }

    private static bool TryCreatePropertyGetter(PropertyInfo property, [NotNullWhen(true)] out Func<object, object?>? getPropertyValue)
    {
        if (property.GetMethod is null || property.GetMethod.IsPrivate)
        {
            getPropertyValue = null;
            return false;
        }

        getPropertyValue = property.GetValue;

        return true;
    }

    private static bool TryCreatePropertySetter(PropertyInfo property, [NotNullWhen(true)] out Action<object, object?>? setPropertyValue)
    {
        if (property.SetMethod is null || property.SetMethod.IsPrivate)
        {
            setPropertyValue = null;
            return false;
        }

        setPropertyValue = property.SetValue;

        return true;
    }

    public void ClearCaches()
    {
        _propertyCache.Clear();
        _simpleTypeCache.Clear();
        _commentCache.Clear();
    }

    public void Dispose()
        => ClearCaches();
}
