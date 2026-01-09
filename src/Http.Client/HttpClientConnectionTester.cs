using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort;

public sealed class HttpClientConnectionTester : IDataPortConnectionTester<HttpClientDataPortCommunication>
{
    public static async Task TestConnectionAsync(HttpClientDataPortCommunication communication)
    {
        using HttpClient client = new();
        await TestConnectionAsync(communication, client);
    }

    internal static async Task TestConnectionAsync(HttpClientDataPortCommunication communication, HttpClient httpClient)
    {
        HttpClientDataPortProperties properties = new(communication);
        try
        {
            var response = await httpClient.GetAsync(new Uri(properties.BaseAddress));

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP request failed: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            throw new DataPortConnectionFailedException("Failed to connect to HTTP.", ex);
        }
    }
}
