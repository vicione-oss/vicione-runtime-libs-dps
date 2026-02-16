using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation;

/// <summary>
/// Validates data point values for type correctness and value range compliance.
/// </summary>
/// <typeparam name="TDataPoint">The type of data point being validated.</typeparam>
/// <typeparam name="TDataPointValue">The type of data point value being validated.</typeparam>
public class DataPointValueValidator<TDataPoint, TDataPointValue>(ILogger logger)
    where TDataPoint : IDataPoint
    where TDataPointValue : IDataPointValue<TDataPoint>
{
    /// <summary>
    /// Validates all DataPointValue instances for type and range correctness.
    /// </summary>
    /// <param name="dataPointValues">Collection of data point values to validate.</param>
    /// <returns>Validation result containing valid values and all failures encountered.</returns>
    public DataPointValuesValidationResult<TDataPoint, TDataPointValue> ValidateAll(
        IReadOnlyCollection<TDataPointValue> dataPointValues)
    {
        ArgumentNullException.ThrowIfNull(dataPointValues);

        var validValues = new List<TDataPointValue>(dataPointValues.Count);
        var failures = new List<DataPointValueValidationFailure<TDataPoint>>(dataPointValues.Count);

        foreach (var dataPointValue in dataPointValues)
        {
            var typeValidation = dataPointValue.ValidateType();
            if (!typeValidation.IsValid)
            {
                failures.Add(CreateTypeMismatchFailure(dataPointValue, typeValidation));
                continue;
            }

            if (!dataPointValue.IsInValueRange())
            {
                failures.Add(CreateValueOutOfRangeFailure(dataPointValue));
                continue;
            }

            validValues.Add(dataPointValue);
        }

        var isValid = failures.Count == 0;
        return new DataPointValuesValidationResult<TDataPoint, TDataPointValue>(isValid, validValues.AsReadOnly(),
            failures.AsReadOnly());
    }

    private DataPointValueValidationFailure<TDataPoint> CreateValueOutOfRangeFailure(
        IDataPointValue<TDataPoint> dataPointValue)
    {
        var dataPoint = dataPointValue.DataPoint;
        var channel = dataPoint.Channels.TransferredChannels.FirstOrDefault()?.Value ?? "unknown";

        var dataType = dataPoint.DataTypeName.Value;
        logger.ValueOutOfRange(
            channel,
            dataPoint.Identifier.Value,
            dataPointValue.Value,
            dataType);

        return new DataPointValueValidationFailure<TDataPoint>(
            DataPoint: dataPoint,
            ActualValue: dataPointValue.Value,
            Reason: ValidationFailureReason.ValueOutOfRange,
            Details:
            $"Value out of range at {dataPoint.Identifier}: '{dataPointValue.Value}' is invalid for {dataType}");
    }

    private DataPointValueValidationFailure<TDataPoint> CreateTypeMismatchFailure(
        IDataPointValue<TDataPoint> dataPointValue,
        TypeValidationResult typeValidation)
    {
        var dataPoint = dataPointValue.DataPoint;
        var channel = dataPoint.Channels.TransferredChannels.FirstOrDefault()?.Value ?? "unknown";
        var allowedTypes = string.Join(", ", typeValidation.AllowedTypes.Select(type => type.Name));

        logger.ValueHasWrongType(
            channel,
            dataPoint.Identifier.Value,
            allowedTypes,
            typeValidation.ActualType?.Name ?? "null");

        return new DataPointValueValidationFailure<TDataPoint>(
            DataPoint: dataPoint,
            ActualValue: dataPointValue.Value,
            Reason: ValidationFailureReason.TypeMismatch,
            Details:
            $"Type mismatch at {dataPoint.Identifier}: expected {allowedTypes}, got {typeValidation.ActualType?.Name ?? "null"}");
    }
}
