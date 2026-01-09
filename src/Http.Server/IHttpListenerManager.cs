using System;
using System.Threading.Tasks;
using EmbedIO;

namespace ViciOne.Suite.DataPort;

internal interface IHttpListenerManager
{
    Task CloseConnection(HttpServerDataPortCommunication communication);
    Task MapGet(HttpServerDataPortCommunication communication, string path, Func<IHttpContext, Task> handler);
    Task MapPost(HttpServerDataPortCommunication communication, string path, Func<IHttpContext, Task> handler);
    Task RegisterOrCreateWebApp(HttpServerDataPortCommunication communication);
    Task StartApp(HttpServerDataPortCommunication communication);
}
