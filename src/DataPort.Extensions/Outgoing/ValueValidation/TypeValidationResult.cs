using System;
using System.Collections.Generic;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

/// <summary>
/// Represents the result of validating a value's type against a data point's expected types.
/// </summary>
/// <param name="IsValid">
/// <c>true</c> if the value's type matches one of the allowed types; otherwise, <c>false</c>.
/// </param>
/// <param name="ActualType">
/// The actual CLR type of the value being validated, or <c>null</c> if the value was <c>null</c>.
/// </param>
/// <param name="AllowedTypes">
/// The list of CLR types that are acceptable for the data point.
/// When <paramref name="IsValid"/> is <c>false</c>, this list indicates what types were expected.
/// </param>
public readonly record struct TypeValidationResult(
    bool IsValid,
    Type? ActualType,
    IReadOnlyList<Type> AllowedTypes);
