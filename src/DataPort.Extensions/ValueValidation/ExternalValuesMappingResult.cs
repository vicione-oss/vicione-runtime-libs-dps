using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.ValueValidation;

/// <summary>
/// Result of mapping external values to data point values.
/// </summary>
public readonly record struct ExternalValuesMappingResult<TDataPoint, TDatapointValue>(
    IReadOnlyList<TDatapointValue> MappedValues,
    IReadOnlyList<string> UnmappedChannels) where TDataPoint : IDataPoint
    where TDatapointValue : IDataPointValue<TDataPoint>;
