using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.Communication;
using ViciOne.ManagedEngine.ExternalCommunication;

namespace ViciOne.Suite.DataPort;

public abstract class DataPortOutgoingCommunicationWithPropertyHandling<T>(T communication) : IExternalOutgoingCommunication<T> where T : IDataPortCommunication, ICommunication
{
    private readonly HashSet<string> _propertyChannels = [.. communication.Nodes.GetAllPropertyChannels()];

    async Task IExternalOutgoingCommunication.SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> values, CancellationToken cancellationToken)
    {
        var processValues = ArrayPool<ExternalValue>.Shared.Rent(values.Count);
        var propertyValues = ArrayPool<ExternalValue>.Shared.Rent(values.Count);

        try
        {
            var (processValuesCount, propertyValuesCount) = ClassifyValues(values, processValues, propertyValues);
            await SendAsync(engineCycle, processValues[0..processValuesCount], propertyValues[0..propertyValuesCount], cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<ExternalValue>.Shared.Return(processValues, true);
            ArrayPool<ExternalValue>.Shared.Return(propertyValues, true);
        }

        (int ProcessValuesCount, int PropertiesValuesCount) ClassifyValues(IReadOnlyCollection<ExternalValue> values, ExternalValue[] processValues, ExternalValue[] propertyValues)
        {
            var valuesCount = 0;
            var propertiesCount = 0;

            foreach (var value in values)
            {
                if (_propertyChannels.Contains(value.Channel))
                    propertyValues[propertiesCount++] = value;
                else
                    processValues[valuesCount++] = value;
            }

            return (valuesCount, propertiesCount);
        }
    }

    public abstract Task ConnectAsync(CancellationToken cancellationToken);
    public abstract Task DisconnectAsync(CancellationToken cancellationToken);
    public abstract Task SendAsync(ulong engineCycle, IReadOnlyCollection<ExternalValue> processValues, IReadOnlyCollection<ExternalValue> propertyValues, CancellationToken cancellationToken);
}
