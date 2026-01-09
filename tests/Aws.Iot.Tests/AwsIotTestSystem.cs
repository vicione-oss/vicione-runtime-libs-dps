using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amazon.IoT;
using Amazon.IotData;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

[SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.")]
public sealed class AwsIotTestSystem : IAsyncLifetime, IDisposable
{
    private AmazonIotDataClient? _amazonIotDataClient;
    private AmazonIoTClient? _amazonIoTClient;

    internal AwsIotDataPortCommunication Communication { get; } = new();
    internal string ServiceUrl { get; } = "";

    public Task InitializeAsync()
    {
        Communication.AccessKey = "";
        Communication.SecretAccessKey = "";
        Communication.CertificateId = "";
        Communication.CertificateFile = @"";
        Communication.PrivateKeyFile = @"";
        _amazonIotDataClient = new(Communication.AccessKey, Communication.SecretAccessKey, ServiceUrl);
        _amazonIoTClient = new(Communication.AccessKey, Communication.SecretAccessKey, new AwsIotDataPortProperties(Communication).Region);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _amazonIotDataClient?.Dispose();
        _amazonIoTClient?.Dispose();
    }

    public Task DisposeAsync()
    {
        _amazonIotDataClient?.Dispose();
        _amazonIoTClient?.Dispose();
        return Task.CompletedTask;
    }

    internal async Task<JsonObject> GetShadow(string thing, string? shadowName = null)
    {
        var getThingShadowResponse = await _amazonIotDataClient!.GetThingShadowAsync(new()
        {
            ThingName = thing,
            ShadowName = shadowName,
        });
        var payload = await JsonSerializer.DeserializeAsync<JsonObject>(getThingShadowResponse.Payload);

        getThingShadowResponse.HttpStatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();

        return payload!;
    }

    internal async Task UpdateShadow(string thing, JsonObject value, string? shadowName = null)
    {
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new JsonObject
        {
            ["state"] = value,
        });
        var updateThingShadowResponse = await _amazonIotDataClient!.UpdateThingShadowAsync(new()
        {
            ThingName = thing,
            ShadowName = shadowName,
            Payload = new MemoryStream(payloadBytes),
        });

        updateThingShadowResponse.HttpStatusCode.Should().Be(HttpStatusCode.OK);
    }

    internal async Task DetachPrincipals(string thing)
    {
        var listThingPrincipalsResponse = await _amazonIoTClient!.ListThingPrincipalsAsync(thing);

        listThingPrincipalsResponse.HttpStatusCode.Should().Be(HttpStatusCode.OK);

        foreach (var principal in listThingPrincipalsResponse.Principals)
        {
            var detachThingPrincipalResponse = await _amazonIoTClient.DetachThingPrincipalAsync(thing, principal);

            detachThingPrincipalResponse.HttpStatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    internal async Task DeleteThing(string thing)
    {
        var deleteThingResponse = await _amazonIoTClient!.DeleteThingAsync(thing);

        deleteThingResponse.HttpStatusCode.Should().Be(HttpStatusCode.OK);
    }
}
