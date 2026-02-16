using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.ValueValidation;

/// <summary>
/// Represents the result of validating a collection of DataPointValue instances.
/// Contains both successfully validated values and any failures encountered.
/// </summary>
/// <param name="IsValid">True if all values passed validation; false if any failures occurred.</param>
/// <param name="ValidValues">List of successfully validated DataPointValue instances.</param>
/// <param name="Failures">List of validation failures encountered during validation.</param>
public record DataPointValuesValidationResult<TDataPoint, TDataPointValue>(
    bool IsValid,
    IReadOnlyList<TDataPointValue> ValidValues,
    IReadOnlyList<DataPointValueValidationFailure<TDataPoint>> Failures)
    where TDataPoint : IDataPoint
    where TDataPointValue : IDataPointValue<TDataPoint>;
