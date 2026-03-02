using ViciOne.Suite.DataPort.Extensions.Client;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Client;

internal record TestClientInformation(string Id = "test") : IClientInformation;
