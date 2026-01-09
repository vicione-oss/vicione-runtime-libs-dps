using System;
using System.Threading.Tasks;
using InfluxDB.Client;

namespace ViciOne.Suite.DataPort;

public sealed class InfluxDbConnectionTester : IDataPortConnectionTester<InfluxDbDataPortCommunication>
{
    public static async Task TestConnectionAsync(InfluxDbDataPortCommunication communication)
    {
        InfluxDbDataPortProperties properties = new(communication);
        try
        {
            using var client = new InfluxDBClient(properties.Url, properties.Token);

            _ = await client.GetOrganizationsApi().FindOrganizationsAsync(org: properties.Organization);
        }
        catch (Exception ex)
        {
            throw new DataPortConnectionFailedException("Failed to connect to InfluxDB.", ex);
        }
    }
}
