using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Suite.System.Runtime;

internal interface IInputProcessor
{
    void ProcessInput(IRuntimeConnector connector);
}
