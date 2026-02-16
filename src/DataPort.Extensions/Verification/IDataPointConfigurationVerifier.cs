using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;

namespace ViciOne.Suite.DataPort.Extensions.Verification;

/// <summary>
/// Verifies that a set of data points are correctly configured before the data port starts operating.
/// </summary>
/// <typeparam name="TDataPoint">The type of the data point to verify.</typeparam>
public interface IDataPointConfigurationVerifier<TDataPoint> where TDataPoint : IDataPoint
{
    /// <summary>
    /// Verifies the provided data points and returns any that are misconfigured.
    /// </summary>
    /// <param name="dataPoints">The data points to verify.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A list of <see cref="MisConfiguredDataPoint{TDataPoint}"/> entries for each data point
    /// that failed verification. Returns an empty list if all data points are valid.
    /// </returns>
    ValueTask<List<MisConfiguredDataPoint<TDataPoint>>> Verify(List<TDataPoint> dataPoints, CancellationToken cancellationToken);
}