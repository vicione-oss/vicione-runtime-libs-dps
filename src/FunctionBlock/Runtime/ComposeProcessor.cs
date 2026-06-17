using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Suite.System.Runtime;

internal class ComposeProcessor : IInputProcessor
{
    private readonly Type _type;
    private readonly Dictionary<Guid, Action<object, object?>> _connectorTargetTypeSetters;
    private readonly IRuntimeLink _runtimeLink;
    private readonly Dictionary<Guid, ChangedValue> _changedValues = [];

    internal ComposeProcessor(Type type, Dictionary<Guid, Action<object, object?>> connectorTargetTypeSetters, IRuntimeLink runtimeLink)
    {
        _type = type;
        _connectorTargetTypeSetters = connectorTargetTypeSetters;
        _runtimeLink = runtimeLink;
    }

    public void ProcessInput(IRuntimeConnector runtimeConnector)
        => Add(runtimeConnector.Connector.Id, runtimeConnector.ChangedTime, runtimeConnector.Validity, runtimeConnector.Value);

    internal void Add(Guid connectorId, DateTime changedTime, Validity validity, object? value)
        => _changedValues.Add(connectorId, new(changedTime, validity, value));

    internal void Process()
    {
        if (_changedValues.Count == 0)
            return;

        var instance = Activator.CreateInstance(_type) ?? throw new InvalidOperationException("Cannot compose null instance.");
        var changedTime = DateTime.MinValue;
        var validity = int.MaxValue;

        foreach (var (connectorId, changedValue) in _changedValues)
        {
            _connectorTargetTypeSetters[connectorId](instance, changedValue.Value);

            if (changedValue.ChangedTime > changedTime)
                changedTime = changedValue.ChangedTime;

            var changedValidity = (int)changedValue.Validity;
            if (changedValidity < validity)
                validity = changedValidity;
        }

        _runtimeLink.SetData(instance, changedTime, (Validity)validity);
        _changedValues.Clear();
    }

    private record ChangedValue(DateTime ChangedTime, Validity Validity, object? Value);
}
