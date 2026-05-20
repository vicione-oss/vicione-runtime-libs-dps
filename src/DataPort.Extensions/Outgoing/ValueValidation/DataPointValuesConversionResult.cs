using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

/// <summary>
/// The outcome of converting a batch of external values into strongly-typed data point values.
/// Mirrors <see cref="DataPointValuesValidationResult{TDataPointValue}"/> for the conversion stage.
/// </summary>
/// <param name="ConvertedValues">The successfully converted data point values.</param>
/// <param name="Failures">The conversion failures encountered.</param>
public record DataPointValuesConversionResult<TDataPointValue>(
    IReadOnlyList<TDataPointValue> ConvertedValues,
    IReadOnlyList<DataPointValueValidationFailure> Failures) where TDataPointValue : IDataPointValue
{
    /// <summary>
    /// True if every external value was converted; false if any value failed.
    /// </summary>
    public bool AllConverted => Failures.Count == 0;
}
