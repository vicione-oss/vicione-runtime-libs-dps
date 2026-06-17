using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;

namespace ViciOne.Suite.System.Runtime;

internal class EmptyConnectorValueTransferInterceptor : IConnectorValueTransferInterceptor
{
    public IConnectorValueTransferInterceptionResult<TValue> Intercept<TValue>(Guid connectorId, TValue? value)
        => new DefaultConnectorValueTransferInterceptionResult<TValue>() { Value = value };

    public bool IsInterceptionPending(Guid id)
        => false;
}
