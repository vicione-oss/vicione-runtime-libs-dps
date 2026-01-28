using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via YAML deserializer (reflection).")]
public class DataPortMetadata
{
    public List<NodeYaml> Nodes { get; internal set; } = [];
    public List<PropertyDefinitionYaml> Properties { get; internal set; } = [];
    public List<PropertyCategoryDefinitionYaml> PropertyCategories { get; internal set; } = [];
}

