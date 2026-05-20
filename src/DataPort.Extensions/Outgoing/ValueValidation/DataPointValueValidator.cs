using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

/// <summary>
/// Validates data point values for value range compliance.
/// </summary>
/// <typeparam name="TDataPoint">The type of data point being validated.</typeparam>
/// <typeparam name="TDataPointValue">The type of data point value being validated.</typeparam>
/// <remarks>
/// Type correctness is no longer validated here: a <see cref="ITypedDataPointValue{TValue}"/> is
/// strongly typed, so its value can only ever be of the data point's configured .NET type. Any failure to
/// represent an external value as that type is handled at conversion time, not by this validator.
/// </remarks>
public class DataPointValueValidator<TDataPoint, TDataPointValue>(ILogger logger)
    where TDataPoint : IDataPoint
    where TDataPointValue : IDataPointValue
{
    /// <summary>
    /// Validates all DataPointValue instances for range correctness.
    /// </summary>
    /// <param name="dataPointValues">Collection of data point values to validate.</param>
    /// <returns>Validation result containing valid values and all failures encountered.</returns>
    public DataPointValuesValidationResult<TDataPointValue> ValidateAll(
        IReadOnlyList<TDataPointValue> dataPointValues)
    {
        ArgumentNullException.ThrowIfNull(dataPointValues);

        var validValues = new List<TDataPointValue>(dataPointValues.Count);
        var failures = new List<DataPointValueValidationFailure>();

        foreach (var dataPointValue in dataPointValues)
        {
            if (!dataPointValue.IsInValueRange())
            {
                failures.Add(CreateValueOutOfRangeFailure(dataPointValue));
                continue;
            }

            validValues.Add(dataPointValue);
        }

        return new DataPointValuesValidationResult<TDataPointValue>(validValues.AsReadOnly(),
            failures.AsReadOnly());
    }

    private DataPointValueValidationFailure CreateValueOutOfRangeFailure(
        IDataPointValue dataPointValue)
    {
        var dataPoint = dataPointValue.DataPoint;
        var transferredChannels = dataPoint.Channels.Transferred;
        var channel = transferredChannels.Count > 0 ? transferredChannels[0].Value : "unknown";

        var dataType = dataPoint.DataTypeName.Value;
        logger.ValueOutOfRange(
            channel,
            dataPoint.Identifier.Value,
            dataType);

        return new DataPointValueValidationFailure(
            DataPoint: dataPoint,
            Reason: ValidationFailureReason.ValueOutOfRange,
            Details: $"Value out of range at {dataPoint.Identifier} for {dataType}");
    }
}
