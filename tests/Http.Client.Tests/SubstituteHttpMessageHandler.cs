using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort;

internal sealed class SubstituteHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> func) : HttpMessageHandler
{
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        => func(request);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(func(request));
}
