using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ViciOne.Suite.DataPort;

public static class YamlDeserializer
{
    public static DataPortMetadata LoadMetadata(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<DataPortMetadata>(yaml);
    }
}
