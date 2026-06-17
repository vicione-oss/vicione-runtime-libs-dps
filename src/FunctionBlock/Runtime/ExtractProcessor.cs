using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Suite.System.Runtime;

internal class ExtractProcessor : IInputProcessor
{
    private readonly Func<object, object?> _getter;
    private readonly IRuntimeLink _runtimeLink;

    internal ExtractProcessor(Func<object, object?> getter, IRuntimeLink runtimeLink)
    {
        _getter = getter;
        _runtimeLink = runtimeLink;
    }

    public void ProcessInput(IRuntimeConnector runtimeConnector)
        => ProcessValue(runtimeConnector.ChangedTime, runtimeConnector.Validity, runtimeConnector.Value);

    internal void ProcessValue(DateTime changedTime, Validity validity, object? value)
    {
        object? result;

        if (value is null)
            result = null;
        else
            result = _getter(value);

        _runtimeLink.SetData(result, changedTime, validity);
    }
}
