namespace ViciOne.Suite.DataPort.Extensions.Outgoing;

/// <summary>
/// Factory for creating data point values.
/// </summary>
/// <typeparam name="TDataPoint">Data point configuration type</typeparam>
/// <typeparam name="TDataPointValue">Data point value type</typeparam>
public interface IDataPointValueFactory<in TDataPoint, out TDataPointValue>
{
    /// <summary>
    /// Creates a data point value with validation.
    /// </summary>
    /// <param name="dataPoint">Data point configuration</param>
    /// <param name="value">Value to write</param>
    /// <returns>data point value object</returns>
    TDataPointValue Create(TDataPoint dataPoint, object? value);
}
