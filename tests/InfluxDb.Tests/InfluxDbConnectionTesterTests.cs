using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;


public class InfluxDbConnectionTester_TestConnectionAsync
{
    [Collection(InfluxDbTestEnvironment.Name)]
    public class Interoperability(InfluxDbTestSystem influxDb)
    {
        [Fact]
        [Trait("Category", "Interoperability")]
        public Task Can_connect_to_server() => InfluxDbConnectionTester.TestConnectionAsync(influxDb.Communication);
    }

    [Fact]
    public async Task Throws_if_test_connection_fails()
    {
        InfluxDbDataPortCommunication communication = new()
        {
            Url = "http://localhost:8086",
        };

        var act = FluentActions.Awaiting(() => InfluxDbConnectionTester.TestConnectionAsync(communication));

        await act.Should().ThrowAsync<DataPortConnectionFailedException>().WithMessage("*fail*connect*influxdb*");
    }
}
