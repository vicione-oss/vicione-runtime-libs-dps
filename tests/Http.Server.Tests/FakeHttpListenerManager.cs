using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;

namespace ViciOne.Suite.DataPort;

internal class FakeHttpListenerManager : IHttpListenerManager
{
    public List<string> PostEndpoints = [];
    public List<string> GetEndpoints = [];
    public Dictionary<string, Func<IHttpContext, Task>> CapturedPostHandlers = [];

    public Task CloseConnection(HttpServerDataPortCommunication communication) => Task.CompletedTask;

    public Task MapGet(HttpServerDataPortCommunication communication, string path, Func<IHttpContext, Task> handler)
    {
        GetEndpoints.Add(path);

        return Task.CompletedTask;
    }

    public Task MapPost(HttpServerDataPortCommunication communication, string path, Func<IHttpContext, Task> handler)
    {
        PostEndpoints.Add(path);
        CapturedPostHandlers[path] = handler;

        return Task.CompletedTask;
    }

    public Task RegisterOrCreateWebApp(HttpServerDataPortCommunication communication) => Task.CompletedTask;
    public Task StartApp(HttpServerDataPortCommunication communication) => Task.CompletedTask;
}
