using System;

namespace ViciOne.Suite.DataPort;

[Serializable]
public class DataPortConnectionFailedException : Exception
{
    public DataPortConnectionFailedException() { }
    public DataPortConnectionFailedException(string message) : base(message) { }
    public DataPortConnectionFailedException(string message, Exception inner) : base(message, inner) { }
}
