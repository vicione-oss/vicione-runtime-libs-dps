using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.ManagedEngine.ExternalCommunication;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class DataPortOutgoingCommunicationWithPropertyHandling_
{
    [Fact]
    public async Task Acceptance_Async()
    {
        TestCommunication communication = new()
        {
            Nodes =
            [
                new()
                {
                    Properties =
                    {
                        { "Count", new() { Value = 0, Channels = { "property", }, } },
                        { "PropertyWithSubProperties", new() { Value = new Dictionary<string, Property>()
                            {
                                { "SubProperty", new() { Value = 0, Channels = { "subProperty", }, } },
                            },
                        } },
                    },
                }
            ],
        };
        List<ExternalValue> values =
        [
            new()
            {
                Channel = "property",
                Value = 10,
            },
            new()
            {
                Channel = "subProperty",
                Value = 25,
            },
            new()
            {
                Channel = "value",
                Value = 1,
            },
        ];
        var provider = Substitute.For<DataPortOutgoingCommunicationWithPropertyHandling<TestCommunication>>(communication);
        var processValues = Array.Empty<ExternalValue>();
        var propertyValues = Array.Empty<ExternalValue>();
        await provider.SendAsync(Arg.Any<ulong>(), Arg.Do<IReadOnlyCollection<ExternalValue>>(v => processValues = [.. v]), Arg.Do<IReadOnlyCollection<ExternalValue>>(v => propertyValues = [.. v]), Arg.Any<CancellationToken>());

        await ((IExternalOutgoingCommunication<TestCommunication>)provider).SendAsync(0, values, CancellationToken.None);

        processValues.Should().ContainSingle().Which.Value.Should().Be(1);
        propertyValues.Should().HaveCount(2).And.SatisfyRespectively(
            e =>
            {
                e.Channel.Should().Be("property");
                e.Value.Should().Be(10);
            },
            e =>
            {
                e.Channel.Should().Be("subProperty");
                e.Value.Should().Be(25);
            });
    }

    [SuppressMessage("Maintainability", "CA1515:Erwägen Sie, öffentliche Typen intern zu machen.")]
    public sealed record class TestCommunication : DataPortCommunication;
}
