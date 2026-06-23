using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Verification;

/// <summary>
/// Verifies that a set of data points are correctly configured before the data port starts operating.
/// </summary>
/// <typeparam name="TDataPoint">The type of the data point to verify.</typeparam>
/// <remarks>
/// Report misconfiguration by returning failures rather than throwing: the base class logs each
/// returned <see cref="MisconfiguredDataPoint{TDataPoint}"/> and aborts <c>ConnectAsync</c> with an
/// <see cref="Exceptions.InvalidConfigurationException"/> when the returned list is non-empty.
/// Supply an instance through the base class's <c>CreateConfigurationVerifier</c> factory; the default
/// returns <see langword="null"/>, which skips verification.
/// </remarks>
public interface IDataPointConfigurationVerifier<TDataPoint> where TDataPoint : IDataPoint
{
    /// <summary>
    /// Verifies the provided data points and returns any that are misconfigured.
    /// </summary>
    /// <param name="dataPoints">The data points to verify.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A list of <see cref="MisconfiguredDataPoint{TDataPoint}"/> entries for each data point
    /// that failed verification. Returns an empty list if all data points are valid.
    /// </returns>
    ValueTask<IReadOnlyList<MisconfiguredDataPoint<TDataPoint>>> Verify(IReadOnlyList<TDataPoint> dataPoints, CancellationToken cancellationToken);
}
