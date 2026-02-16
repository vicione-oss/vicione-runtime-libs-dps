using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Verification;

/// <summary>
/// Represents a data point that failed verification due to one or more configuration mismatches.
/// </summary>
/// <typeparam name="TDataPoint">The type of the data point.</typeparam>
/// <param name="DataPoint">The misconfigured data point.</param>
/// <param name="MisMatchingConfigurations">The list of configuration mismatches detected for the data point.</param>
public record MisConfiguredDataPoint<TDataPoint>(
    TDataPoint DataPoint,
    List<MisMatchingConfiguration> MisMatchingConfigurations) where TDataPoint : IDataPoint;

/// <summary>
/// Represents a single configuration mismatch detected during data point verification.
/// </summary>
/// <param name="Value">A human-readable description of the mismatch.</param>
public record MisMatchingConfiguration(string Value);