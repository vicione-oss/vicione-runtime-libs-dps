using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class HttpClientConnectionTester_TestConnectionAsync
{
    [Fact]
    public async Task Can_connect_to_server()
    {
        HttpClientDataPortCommunication communication = new()
        {
            BaseAddress = "http://localhost:80",
        };
        using SubstituteHttpMessageHandler messageHandler = new(Assert);
        using HttpClient client = new(messageHandler);

        await HttpClientConnectionTester.TestConnectionAsync(communication, client);

        static HttpResponseMessage Assert(HttpRequestMessage request)
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri.Should().Be("http://localhost:80");
            return new(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Throws_if_test_connection_fails()
    {
        HttpClientDataPortCommunication communication = new()
        {
            BaseAddress = "http://localhost:80",
        };

        var act = FluentActions.Awaiting(() => HttpClientConnectionTester.TestConnectionAsync(communication));

        await act.Should().ThrowAsync<DataPortConnectionFailedException>().WithMessage("*fail*connect*http*");
    }
}
