using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via YAML deserializer (reflection).")]
public class PropertyCategoryDefinitionYaml
{
    public string Id { get; set; } = string.Empty;
}

