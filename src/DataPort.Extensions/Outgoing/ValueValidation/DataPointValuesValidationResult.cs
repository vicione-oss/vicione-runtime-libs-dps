using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

/// <summary>
/// Represents the result of validating a collection of DataPointValue instances.
/// Contains both successfully validated values and any failures encountered.
/// </summary>
/// <param name="ValidValues">List of successfully validated DataPointValue instances.</param>
/// <param name="Failures">List of validation failures encountered during validation.</param>
public record DataPointValuesValidationResult<TDataPointValue>(
    IReadOnlyList<TDataPointValue> ValidValues,
    IReadOnlyList<DataPointValueValidationFailure> Failures) where TDataPointValue : IDataPointValue
{
    /// <summary>
    /// True if all values passed validation; false if any failures occurred
    /// </summary>
    public bool IsValid => Failures.Count == 0;
}
