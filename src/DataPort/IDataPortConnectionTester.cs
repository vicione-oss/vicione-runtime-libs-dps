using System.Threading.Tasks;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.Suite.DataPort;

public interface IDataPortConnectionTester<T> where T : ICommunication
{
    /// <summary>
    /// Opens and closes a connection if no exception is thrown the test is considered passed.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="DataPortConnectionFailedException"></exception>
    static abstract Task TestConnectionAsync(T communication);
}
