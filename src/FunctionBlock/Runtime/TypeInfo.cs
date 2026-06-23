using System;

namespace ViciOne.Suite.System.Runtime;

internal readonly record struct TypeInfo
{
    public Type SimpleType { get; init; }

    public string Path { get; init; }

    public bool IsNullable { get; init; }

    public string Name { get; init; }

    public string Comment { get; init; }

    public readonly Func<object, object?>? PropertyGetter { get; init; }
    public readonly Action<object, object?>? PropertySetter { get; init; }
}
